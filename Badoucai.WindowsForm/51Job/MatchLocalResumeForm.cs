using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Badoucai.HTTP;
using Newtonsoft.Json;
using FormaterResume = Badoucai.Formatter.Template.Data.Resume;
using _51Formatter = Badoucai.Formatter.Template._51Job.Josn.v0;
using _51Serializer = Badoucai.Serialization.Template._51Job.Mht.v0;
using System.Configuration;
using System.IO.Compression;
using Aliyun.OSS;
using Badoucai.Library;

namespace Badoucai.WindowsForm._51Job
{
    public partial class MatchLocalResumeForm : Form
    {
        /// <summary>
        /// 简历 Dom 队列
        /// </summary>
        private static readonly ConcurrentQueue<HtmlAgilityPack.HtmlDocument> resumeQueue = new ConcurrentQueue<HtmlAgilityPack.HtmlDocument>();

        /// <summary>
        /// 上传 OSS 重试队列
        /// </summary>
        private static readonly ConcurrentQueue<Tuple<string, FormaterResume, string>> retryResumeQueue = new ConcurrentQueue<Tuple<string, FormaterResume, string>>();

        /// <summary>
        /// 待上传简历文件队列
        /// </summary>
        private static readonly ConcurrentQueue<string> pathQueue = new ConcurrentQueue<string>();

        /// <summary>
        /// 简历计数增量
        /// </summary>
        private static int count;

        private static string companyName = string.Empty;

        public MatchLocalResumeForm()
        {
            InitializeComponent();
        }

        private void btn_Choose_Click(object sender, EventArgs e)
        {
            var folderBrowserDialog = new FolderBrowserDialog();

            if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
            {
                this.tbx_FilesPath.Text = folderBrowserDialog.SelectedPath;
            }
        }

        private void btn_StartDeCompression_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(this.tbx_FilesPath.Text) || !Directory.Exists(this.tbx_FilesPath.Text))
            {
                this.AsyncSetLog(this.tbx_Log,"文件路径为空，或不存在！");

                return;
            }

            companyName = this.tbx_FilesPath.Text.Substring(this.tbx_FilesPath.Text.LastIndexOf("\\", StringComparison.Ordinal) - 1) + "\\";

            this.RunAsync(() =>
            {
                DecompressionResumeZip(this.tbx_FilesPath.Text + "\\");
            });
        }

        /// <summary>
        /// 解压Zip简历文件包
        /// </summary>
        private void DecompressionResumeZip(string path)
        {
            var tasks = new List<Task>();

            for (var i = 0; i < 8; i++)
            {
                tasks.Add(Task.Run(() => FormatResume()));
            }

            foreach (var doc in CompressionFactory.GetMhtSources(path))
            {
                resumeQueue.Enqueue(doc);
            }

            Task.WaitAll(tasks.ToArray());
        }

        /// <summary>
        /// 格式化简历
        /// </summary>
        private void FormatResume()
        {
            var domFilesPath = ConfigurationManager.AppSettings["51Job_DomFiles_Path"] + companyName;

            var formatterFailFilesPath = ConfigurationManager.AppSettings["51Job_FormatterFailFiles_Path"] + companyName;

            var formatterSuccessFilesPath = ConfigurationManager.AppSettings["51Job_FormatterSuccessFiles_Path"] + companyName;

            if (!Directory.Exists(domFilesPath)) Directory.CreateDirectory(domFilesPath);

            if (!Directory.Exists(formatterFailFilesPath)) Directory.CreateDirectory(formatterFailFilesPath);

            if (!Directory.Exists(formatterSuccessFilesPath)) Directory.CreateDirectory(formatterSuccessFilesPath);

            while (true)
            {
                HtmlAgilityPack.HtmlDocument doc;

                if (!resumeQueue.TryDequeue(out doc)) continue;

                var id = _51Serializer.Serializer.DeserializeId(doc);

                var name = doc?.DocumentNode.SelectSingleNode("//td[@class = 'name']/text()[1]").InnerText.Trim("&nbsp;".ToCharArray());

                var pathFile = $"{formatterSuccessFilesPath}{id}.json";

                var pathError = $"{formatterFailFilesPath}{id}.html";

                doc?.Save($"{domFilesPath}{id}.html");

                try
                {
                    var resumeObj = _51Formatter.Formatter.Format(doc);

                    lock (resumeQueue)
                    {
                        File.WriteAllText(pathFile, JsonConvert.SerializeObject(resumeObj));
                    }

                    if (File.Exists(pathError)) File.Delete(pathError);

                    this.AsyncSetLog(this.tbx_Log,$"成功解析：{id}_{name}.json => 排队数：{resumeQueue.Count}");
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;

                    this.AsyncSetLog(this.tbx_Log,$"{ex.Message} => {id}_{name}.html => 排队数：{resumeQueue.Count}");

                    Console.ResetColor();

                    lock (resumeQueue)
                    {
                        doc?.Save(pathError);
                    }
                }
            }
        }

        /// <summary>
        /// 上传简历
        /// </summary>
        private void UploadResume()
        {
            var newOss = new OssClient(
                ConfigurationManager.AppSettings["Oss.New.Url"],
                ConfigurationManager.AppSettings["Oss.New.KeyId"],
                ConfigurationManager.AppSettings["Oss.New.KeySecret"]);

            var newBucket = ConfigurationManager.AppSettings["Oss.New.Bucket"];

            Task.Run(() =>
            {
                RetryUploadOss(newOss, newBucket);
            });

            var path = ConfigurationManager.AppSettings["51Job_FormatterSuccessFiles_Path"] + companyName;

            var tasks = new List<Task>();

            for (var i = 0; i < 16; i++)
            {
                tasks.Add(Task.Run(() =>
                {
                    while (true)
                    {
                        string filePath;

                        if (!pathQueue.TryDequeue(out filePath)) continue;

                        SingleFileUpload(newOss, newBucket, filePath);
                    }
                }));
            }

            foreach (var file in Directory.EnumerateFiles(path, "*.json"))
            {
                pathQueue.Enqueue(file);
            }

            Task.WaitAll(tasks.ToArray());
        }

        /// <summary>
        /// 单个文件上传
        /// </summary>
        /// <param name="newBucket"></param>
        /// <param name="path"></param>
        /// <param name="newOss"></param>
        private void SingleFileUpload(IOss newOss, string newBucket, string path)
        {
            var resume = JsonConvert.DeserializeObject<FormaterResume>(File.ReadAllText(path));

            dynamic response;

            var account = new ResumeRefrence
            {
                Id = resume.Reference.Id,
                Source = resume.Reference.Source
            };

            #region 准备上传简历到数据库

            try
            {
                response = HttpAPI.PrepareUploadResume(JsonConvert.SerializeObject(account));

                if (response.Code.ToString() != "0")
                {
                    this.AsyncSetLog(this.tbx_Log, $"准备上传简历API，响应信息：{JsonConvert.SerializeObject(response)}");

                    pathQueue.Enqueue(path);

                    return;
                }
            }
            catch (Exception ex)
            {
                pathQueue.Enqueue(path);

                this.AsyncSetLog(this.tbx_Log,$"{DateTime.Now} 准备上传简历到数据库异常，异常消息：{ex.Message}");

                return;
            }

            #endregion

            #region 上传简历到数据库

            try
            {
                response = HttpAPI.UploadResume(JsonConvert.SerializeObject(resume));

                if (response.Code.ToString() != "0")
                {
                    var failpath = ConfigurationManager.AppSettings["51Job_UploadFailFiles_Path"] + companyName + Path.GetFileName(path);

                    if (File.Exists(failpath)) File.Delete(failpath);

                    File.Move(path, failpath);

                    this.AsyncSetLog(this.tbx_Log,$"上传简历失败！响应信息：{JsonConvert.SerializeObject(response)}");

                    return;
                }
                else
                {
                    this.AsyncSetLog(this.tbx_Log,$"上传简历成功！已上传：{Interlocked.Increment(ref count)}，还剩余：{pathQueue.Count} 份待上传！");
                }
            }
            catch (Exception ex)
            {
                pathQueue.Enqueue(path);

                this.AsyncSetLog(this.tbx_Log,$"{DateTime.Now} 上传简历到数据库异常，异常消息：{ex.Message}");

                return;
            }
            finally
            {
                HttpAPI.FinishUploadResume(JsonConvert.SerializeObject(account));
            }

            #endregion

            #region 上传简历到 OSS 

            var tag = response.Reference.Tag.ToString();

            var id = response.Reference.ResumeId.ToString();

            var cellphone = response.Reference.Cellphone.ToString();

            var email = response.Reference.Email.ToString();

            if (tag == "C" || tag == "U")
            {
                if (cellphone != "0")
                {
                    resume.Cellphone = cellphone;

                    resume.Email = email;
                }

                using (var stream = new MemoryStream(GZip.Compress(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(resume)))))
                {
                    try
                    {
                        newOss.PutObject(newBucket, $"{ConfigurationManager.AppSettings["Oss.New.ResumePath"]}{id}", stream);
                    }
                    catch (Exception ex)
                    {
                        this.AsyncSetLog(this.tbx_Log,"上传 OSS 失败！异常信息：" + ex.Message);

                        retryResumeQueue.Enqueue(new Tuple<string, FormaterResume, string>(id, resume, path));

                        return;
                    }
                }
            }

            var uploadSuccessPath = ConfigurationManager.AppSettings["51Job_UploadSuccessFiles_Path"] + companyName + Path.GetFileName(path);

            if (File.Exists(uploadSuccessPath)) File.Delete(uploadSuccessPath);

            File.Move(path, uploadSuccessPath);

            #endregion
        }

        /// <summary>
        /// 重试上传 OSS 失败的简历
        /// </summary>
        /// <param name="newOss"></param>
        /// <param name="newBucket"></param>
        private static void RetryUploadOss(IOss newOss, string newBucket)
        {
            while (true)
            {
                Tuple<string, FormaterResume, string> resume;

                if (!retryResumeQueue.TryDequeue(out resume)) continue;

                using (var stream = new MemoryStream(GZip.Compress(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(resume.Item2)))))
                {
                    try
                    {
                        newOss.PutObject(newBucket, $"{ConfigurationManager.AppSettings["Oss.New.ResumePath"]}{resume.Item1}", stream);

                        var uploadSuccessPath = ConfigurationManager.AppSettings["51Job_UploadSuccessFiles_Path"] + companyName + Path.GetFileName(resume.Item3);

                        if (File.Exists(uploadSuccessPath)) File.Delete(uploadSuccessPath);

                        File.Move(resume.Item3, uploadSuccessPath);
                    }
                    catch (Exception)
                    {
                        retryResumeQueue.Enqueue(resume);
                    }
                }
            }
        }

        /// <summary>
        /// 重新解析格式化失败简历
        /// </summary>
        private void RetryFormatterResumeDom()
        {
            var formatterFailFilesPath = ConfigurationManager.AppSettings["51Job_FormatterFailFiles_Path"] + companyName; 

            var tasks = new List<Task>();

            for (var i = 0; i < 8; i++)
            {
                tasks.Add(Task.Run(() => FormatResume()));
            }

            foreach (var file in Directory.EnumerateFiles(formatterFailFilesPath, "*.html"))
            {
                var dom = new HtmlAgilityPack.HtmlDocument();

                dom.LoadHtml(File.ReadAllText(file, Encoding.Default));

                while (resumeQueue.Count > 100)
                {
                    Thread.Sleep(TimeSpan.FromSeconds(1));
                }

                resumeQueue.Enqueue(dom);
            }

            Task.WaitAll(tasks.ToArray());
        }

        private void btn_StartMatch_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(this.tbx_Cookie.Text))
            {
                this.AsyncSetLog(this.tbx_Log, "Cookie 为空！");

                return;
            }

            //TODO:匹配代码待整合
        }
    }

    public class ResumeRefrence
    {
        /// <summary>
        /// 
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string Source { get; set; }
    }
}
