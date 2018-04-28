using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Aliyun.OSS;
using Badoucai.Business.Zhaopin;
using Badoucai.HTTP;
using Badoucai.Library;
using Microsoft.VisualBasic;
//using Badoucai.WindowsForm._51Job;
using Newtonsoft.Json;
using HtmlDocument = HtmlAgilityPack.HtmlDocument;
using Badoucai.EntityFramework.MySql;

namespace Badoucai.WindowsForm.Zhaopin
{
    public partial class DownloadResumeByOldSystemForm : Form
    {
        /// <summary>
        /// 简历 Dom 队列
        /// </summary>
        private static readonly ConcurrentQueue<KeyValuePair<HtmlDocument, DateTime>> resumeQueue = new ConcurrentQueue<KeyValuePair<HtmlDocument, DateTime>>();

        /// <summary>
        /// 上传 OSS 重试队列
        /// </summary>
        private static readonly ConcurrentQueue<Tuple<string, Resume, string>> retryResumeQueue = new ConcurrentQueue<Tuple<string, Resume, string>>();

        /// <summary>
        /// 待上传简历文件队列
        /// </summary>
        private static readonly ConcurrentQueue<string> pathQueue = new ConcurrentQueue<string>();

        private static readonly int interval = Convert.ToInt32(ConfigurationManager.AppSettings["Interval"]);

        private static readonly int retryInterval = Convert.ToInt32(ConfigurationManager.AppSettings["RetryInterval"]);
        

        /// <summary>
        /// 简历计数增量
        /// </summary>
        private static int successCount;

        private static int insertCount;

        private static int updateCount;

        private static string companyName = string.Empty;

        private static CookieContainer _cookieContainer = new CookieContainer();

        public DownloadResumeByOldSystemForm()
        {
            InitializeComponent();
        }

        private void btn_StartDownload_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(this.tbx_FileSavePath.Text))
            {
                var folderBrowserDialog = new FolderBrowserDialog();

                if (folderBrowserDialog.ShowDialog() != DialogResult.OK) return;

                this.tbx_FileSavePath.Text = folderBrowserDialog.SelectedPath;
            }

            this.btn_StartDownload.Enabled = false;

            _cookieContainer = this.tbx_Cookie.Text.Serialize(".zhaopin.com");

            var result = RequestFactory.QueryRequest("https://jobads.zhaopin.com/Company/CompanyList", cookieContainer: _cookieContainer);

            if (result.IsSuccess && Regex.IsMatch(result.Data, "<span title='(.+?)'"))
            {
                companyName = Regex.Match(result.Data, "<span title='(.+?)'").Result("$1");
            }
            else
            {
                var endIndex = this.tbx_FileSavePath.Text.LastIndexOf("\\", StringComparison.Ordinal);

                companyName = this.tbx_FileSavePath.Text.Substring(endIndex + 1);
            }

            var cookieStr = this.tbx_Cookie.Text;

            this.RunAsync(() =>
            {
                DownOldSystemResumeDetail(this.tbx_FileSavePath.Text, cookieStr);
            });
        }

        private void btn_Decompression_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(this.tbx_FileSavePath.Text) || !Directory.Exists(this.tbx_FileSavePath.Text))
            {
                this.AsyncSetLog(this.tbx_Log, "文件路径为空，或不存在！");

                return;
            }

            var startIndex = this.tbx_FileSavePath.Text.IndexOf("\\", 3, StringComparison.Ordinal);

            var endIndex = this.tbx_FileSavePath.Text.LastIndexOf("\\", StringComparison.Ordinal);

            companyName = this.tbx_FileSavePath.Text.Substring(startIndex + 1, endIndex - startIndex);

            //this.RunAsync(this.DecompressionResumeZip);
        }

        private void btn_Upload_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(companyName))
            {
                companyName = Interaction.InputBox("请输入公司名称！", "公司名称");

                if (string.IsNullOrWhiteSpace(companyName))
                {
                    MessageBox.Show("输入的公司名称为空！");

                    return;
                }

                companyName = companyName + "\\";
            }

            //this.RunAsync(this.UploadResume);
        }

        /// <summary>
        /// 下载投递简历列表
        /// </summary>
        /// <param name="cookieContainer"></param>
        /// <param name="beginPage"></param>
        /// <param name="endPage"></param>
        /// <param name="path"></param>
        [Obsolete("下载的压缩文件没有简历Id")]
        public void DownOldSystemResumeZip(CookieContainer cookieContainer, int beginPage, int endPage, string path)
        {
            _cookieContainer = cookieContainer;

            var count = 0;

            for (var i = beginPage; i <= endPage; i++)
            {
                var requestResult = RequestFactory.QueryRequest("https://rd2.zhaopin.com/rdapply/resumes/apply/search?SF_1_1_38=4,9&orderBy=CreateTime", $"PageList2=&DColumn_hidden=&searchKeyword=&curSubmitRecord=1586&curMaxPageNum=80&PageList2={i}&buttonAsse=%E5%AF%BC%E5%85%A5%E6%B5%8B%E8%AF%84%E7%B3%BB%E7%BB%9F&buttonInfo=%E5%8F%91%E9%80%9A%E7%9F%A5%E4%BF%A1&SF_1_1_50=1&SF_1_1_51=-1&SF_1_1_45=&SF_1_1_44=&SF_1_1_52=0&SF_1_1_49=0&IsInvited=0&position_city=%5B%25%25POSITION_CITY%25%25%5D&deptName=&select_unique_id=&selectedResumeList=&PageNo=&PosState=&MinRowID=&MaxRowID=2722819791&RowsCount=123&PagesCount=5&PageType=0&CurrentPageNum={i}&Position_IDs=%5B%25%25POSITION_IDS%25%25%5D&Position_ID=%5B%25%25POSITION_ID%25%25%5D&SortType=0&isCmpSum=0&SelectIndex_Opt=0&Resume_count=0&CID=56211453&forwardingEmailList=&click_search_op_type=-1&X-Requested-With=XMLHttpRequest", RequestEnum.POST, _cookieContainer);

                if (!requestResult.IsSuccess)
                {
                    this.AsyncSetLog(this.tbx_Log,"请求异常！异常原因：" + requestResult.ErrorMsg);

                    return;
                }

                var matches = Regex.Matches(requestResult.Data, "data-resumebh=\"(\\S+)\".+?data-resguid=\"(\\d+)");

                if (matches.Count == 0)
                {
                    this.AsyncSetLog(this.tbx_Log, "匹配结果简历列表失败！");

                    return;
                }

                var filePath = $"{path}\\{count}-{count + matches.Count}.zip";

                var rl = matches.Cast<Match>().Aggregate(string.Empty, (current, match) => current + $",{match.Result("$2")}-{match.Result("$1")}-1-1").Substring(1);

                var param = $"rname=&uname=&down=1&ntype=2&rl={rl}&isone=0&ft=0&jn=";

                requestResult = RequestFactory.QueryRequest("https://rd2.zhaopin.com/s/resume_preview/OutOrSendResume.asp", param, RequestEnum.POST, _cookieContainer);

                if (!requestResult.IsSuccess)
                {
                    this.AsyncSetLog(this.tbx_Log, "请求异常！异常原因：" + requestResult.ErrorMsg);

                    return;
                }

                Thread.Sleep(5000);

                var downloadResult = RequestFactory.HttpDownloadFile(requestResult.Data, filePath, cookieContainer: _cookieContainer);

                if (!downloadResult.IsSuccess)
                {
                    this.AsyncSetLog(this.tbx_Log, "下载失败！异常原因：" + downloadResult.ErrorMsg);

                    return;
                }

                this.AsyncSetLog(this.tbx_Log, "下载成功！Path => " + filePath);

                count += matches.Count;
            }
        }

        /// <summary>
        /// 下载投递简历列表
        /// </summary>
        /// <param name="path"></param>
        /// <param name="cookieStr"></param>
        public void DownOldSystemResumeDetail(string path, string cookieStr)
        {
            var count = 0;

            while (true)
            {
                var requestResult = RequestFactory.QueryRequest("https://rd2.zhaopin.com/rdapply/resumes/apply/search?SF_1_1_38=7,9&orderBy=CreateTime", "PageList2=&DColumn_hidden=&searchKeyword=&curSubmitRecord=1586&curMaxPageNum=80&PageList2=1&buttonAsse=%E5%AF%BC%E5%85%A5%E6%B5%8B%E8%AF%84%E7%B3%BB%E7%BB%9F&buttonInfo=%E5%8F%91%E9%80%9A%E7%9F%A5%E4%BF%A1&SF_1_1_50=1&SF_1_1_51=-1&SF_1_1_45=&SF_1_1_44=&SF_1_1_52=0&SF_1_1_49=0&IsInvited=0&position_city=%5B%25%25POSITION_CITY%25%25%5D&deptName=&select_unique_id=&selectedResumeList=&PageNo=&PosState=&MinRowID=&MaxRowID=2722819791&RowsCount=123&PagesCount=5&PageType=0&CurrentPageNum=1&Position_IDs=%5B%25%25POSITION_IDS%25%25%5D&Position_ID=%5B%25%25POSITION_ID%25%25%5D&SortType=0&isCmpSum=0&SelectIndex_Opt=0&Resume_count=0&CID=56211453&forwardingEmailList=&click_search_op_type=-1&X-Requested-With=XMLHttpRequest", RequestEnum.POST, _cookieContainer);

                if (!requestResult.IsSuccess)
                {
                    this.AsyncSetLog(this.tbx_Log, "请求异常！异常原因：" + requestResult.ErrorMsg);

                    return;
                }

                var matches = Regex.Matches(requestResult.Data, "(?s)javascript:ViewOneResume.+?'(\\d+)'\\);\" href=\"(.+?)\".+?>(.+?)</a.+?<td title=\"(.+?)\"");

                if (requestResult.Data.Contains("因请求量过大导致系统无法处理您的请求，您需要通过验证才能继续后续的操作！"))
                {
                    this.AsyncSetLog(this.tbx_Log, "搜索简历列表失败！出现验证码！");

                    using (var db = new MangningXssDBEntities())
                    {
                        if (!db.ZhaopinCheckCode.Any(a => a.Account == companyName && (a.Status == 0 || a.Status == 1)))
                        {
                            db.ZhaopinCheckCode.Add(new ZhaopinCheckCode
                            {
                                Account = companyName,
                                Cookie = cookieStr,
                                CreateTime = DateTime.Now,
                                Status = 0,
                                Type = 1
                            });

                            db.SaveChanges();
                        }
                    }

                    Thread.Sleep(retryInterval);

                    continue;
                }

                if (matches.Count == 0)
                {
                    Thread.Sleep(10 * 1000);

                    continue;
                }

                var ids = string.Empty;

                foreach (Match match in matches)
                {
                    try
                    {
                        var filePath = $"{path}\\{match.Result("$3").Replace("\\", "").Replace("/", "")}_{DateTime.Parse(match.Result("$4")):yyyy-MM-dd HH：mm：ss}.txt";

                        while (true)
                        {
                            requestResult = RequestFactory.QueryRequest($"https:{match.Result("$2")}", cookieContainer: _cookieContainer);

                            if (!requestResult.IsSuccess)
                            {
                                this.AsyncSetLog(this.tbx_Log, "请求异常！异常原因：" + requestResult.ErrorMsg);

                                return;
                            }

                            if (requestResult.Data.Contains("因请求量过大导致系统无法处理您的请求，您需要通过验证才能继续后续的操作！"))
                            {
                                this.AsyncSetLog(this.tbx_Log, "查看简历详情失败！出现验证码！");

                                using (var db = new MangningXssDBEntities())
                                {
                                    if (!db.ZhaopinCheckCode.Any(a => a.Account == companyName && (a.Status == 0 || a.Status == 1)))
                                    {
                                        db.ZhaopinCheckCode.Add(new ZhaopinCheckCode
                                        {
                                            Account = companyName,
                                            Cookie = cookieStr,
                                            CreateTime = DateTime.Now,
                                            Status = 0,
                                            Type = 1
                                        });

                                        db.SaveChanges();
                                    }
                                }

                                Thread.Sleep(retryInterval);

                                continue;
                            }

                            Thread.Sleep(interval);

                            ids += match.Result("$1") + "%3B";

                            break;
                        }

                        File.WriteAllText(filePath, requestResult.Data);

                        this.AsyncSetLog(this.tbx_Log, $"下载成功！第 {++count} 份 Path => {filePath}");

                        var htmlDocument = new HtmlDocument();

                        htmlDocument.LoadHtml(requestResult.Data);

                        resumeQueue.Enqueue(new KeyValuePair<HtmlDocument, DateTime>(htmlDocument, DateTime.Parse(match.Result("$4"))));
                    }
                    catch (Exception ex)
                    {
                        this.AsyncSetLog(this.tbx_Log, "程序异常！异常消息：" + ex.Message);
                    }
                }

                requestResult = RequestFactory.QueryRequest("https://rd2.zhaopin.com/RdApply/Resumes/Apply/SetResumeState", $"ids={ids}&oldResumeState=1&resumeState=4", RequestEnum.POST, _cookieContainer);

                var jsonObj = JsonConvert.DeserializeObject<dynamic>(requestResult.Data);

                if ((int)jsonObj.Code != 200)
                {
                    this.AsyncSetLog(this.tbx_Log, $"标记简历为不合适失败！Message = {(string)jsonObj.Message}");

                    continue;
                }

                this.AsyncSetLog(this.tbx_Log, "标记简历为不合适成功！");
            }

            this.AsyncSetLog(this.tbx_Log, "简历处理完成！");

            this.RunInMainthread(() =>
            {
                this.btn_StartDownload.Enabled = true;
            });
        }

        /// <summary>
        /// 解压简历
        /// </summary>
        private void DecompressionResumeZip()
        {
            var tasks = new List<Task>();

            var domFilesPath = ConfigurationManager.AppSettings["Zhaopin_DomFiles_Path"] + companyName;

            var formatterFailFilesPath = ConfigurationManager.AppSettings["Zhaopin_FormatterFailFiles_Path"] + companyName;

            var formatterSuccessFilesPath = ConfigurationManager.AppSettings["Zhaopin_FormatterSuccessFiles_Path"] + companyName;

            if (!Directory.Exists(domFilesPath)) Directory.CreateDirectory(domFilesPath);

            if (!Directory.Exists(formatterFailFilesPath)) Directory.CreateDirectory(formatterFailFilesPath);

            if (!Directory.Exists(formatterSuccessFilesPath)) Directory.CreateDirectory(formatterSuccessFilesPath);

            for (var i = 0; i < 4; i++)
            {
                tasks.Add(Task.Run(() => FormatResume(domFilesPath, formatterFailFilesPath, formatterSuccessFilesPath)));
            }

            Task.WaitAll(tasks.ToArray());
        }

        /// <summary>
        /// 格式化简历
        /// </summary>
        /// <param name="domFilesPath"></param>
        /// <param name="formatterFailFilesPath"></param>
        /// <param name="formatterSuccessFilesPath"></param>
        private void FormatResume(string domFilesPath, string formatterFailFilesPath, string formatterSuccessFilesPath)
        {
            while (true)
            {
                KeyValuePair<HtmlDocument, DateTime> keyValuePair;

                if (!resumeQueue.TryDequeue(out keyValuePair)) continue;

                var doc = keyValuePair.Key;

                var id = doc.DocumentNode.SelectSingleNode("//input[@id='resume_id']")?.Attributes["value"]?.Value;

                var name = doc.DocumentNode.SelectSingleNode("//input[@id='tt_username']")?.Attributes["value"]?.Value;

                var pathFile = $"{formatterSuccessFilesPath}{id}.json";

                var pathError = $"{formatterFailFilesPath}{id}.html";

                doc.Save($"{domFilesPath}{id}.html");

                try
                {
                    var resumeObj = Format.Convert_V0(ZhaopinHelper.ConvertTo_Dtl_V0(doc));

                    resumeObj.UpdateTime = keyValuePair.Value;

                    resumeObj.Reference.UpdateTime = keyValuePair.Value;

                    lock (resumeQueue)
                    {
                        File.WriteAllText(pathFile, JsonConvert.SerializeObject(resumeObj));
                    }

                    this.AsyncSetLog(this.tbx_Log, $"成功解析：{id}_{name}.json => 排队数：{resumeQueue.Count}");
                }
                catch (Exception ex)
                {
                    this.AsyncSetLog(this.tbx_Log, $"{ex.Message} => {id}_{name}.html => 排队数：{resumeQueue.Count}");

                    LogFactory.Warn($"{ex.Message} => {id}_{name}.html => 排队数：{resumeQueue.Count}");

                    lock (resumeQueue)
                    {
                        if (File.Exists(pathError)) File.Delete(pathError);

                        doc.Save(pathError);
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

            var path = ConfigurationManager.AppSettings["Zhaopin_FormatterSuccessFiles_Path"] + companyName;

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
            var resume = JsonConvert.DeserializeObject<Resume>(File.ReadAllText(path));

            dynamic response;

            string tag;

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

                    HttpAPI.FinishUploadResume(JsonConvert.SerializeObject(account));

                    pathQueue.Enqueue(path);

                    return;
                }
            }
            catch (Exception ex)
            {
                pathQueue.Enqueue(path);

                this.AsyncSetLog(this.tbx_Log, $"{DateTime.Now} 准备上传简历到数据库异常，异常消息：{ex.Message}");

                HttpAPI.FinishUploadResume(JsonConvert.SerializeObject(account));

                return;
            }

            #endregion

            #region 上传简历到数据库
            
            try
            {
                var times = 0;

                while (true)
                {
                    response = HttpAPI.UploadResume(JsonConvert.SerializeObject(resume));

                    if (response.Code.ToString() != "0" && times++ == 0)
                    {
                        HttpAPI.FinishUploadResume(JsonConvert.SerializeObject(account));

                        continue;
                    }

                    break;
                }

                tag = response.Reference.Tag.ToString();

                if (response.Code.ToString() != "0")
                {

                    var failpath = ConfigurationManager.AppSettings["Zhaopin_UploadFailFiles_Path"] + companyName + Path.GetFileName(path);

                    if (File.Exists(failpath)) File.Delete(failpath);

                    File.Move(path, failpath);

                    this.AsyncSetLog(this.tbx_Log, $"上传简历失败！响应信息：{JsonConvert.SerializeObject(response)}");

                    return;
                }
                else
                {
                    if (tag == "C") Interlocked.Increment(ref insertCount);

                    if (tag == "U") Interlocked.Increment(ref updateCount);

                    this.AsyncSetLog(this.tbx_Log, $"上传简历成功！已上传：{Interlocked.Increment(ref successCount)} C=>{insertCount} U=>{updateCount} 还剩余：{pathQueue.Count} 份待上传！");
                }
            }
            catch (Exception ex)
            {
                pathQueue.Enqueue(path);

                this.AsyncSetLog(this.tbx_Log, $"{DateTime.Now} 上传简历到数据库异常，异常消息：{ex.Message}");

                return;
            }
            finally
            {
                HttpAPI.FinishUploadResume(JsonConvert.SerializeObject(account));
            }

            #endregion

            #region 上传简历到 OSS 

            var id = response.Reference.ResumeId.ToString();

            if (tag == "C" || tag == "U")
            {
                using (var stream = new MemoryStream(GZip.Compress(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(resume)))))
                {
                    try
                    {
                        newOss.PutObject(newBucket, $"{ConfigurationManager.AppSettings["Oss.New.ResumePath"]}{id}", stream);
                    }
                    catch (Exception ex)
                    {
                        this.AsyncSetLog(this.tbx_Log, "上传 OSS 失败！异常信息：" + ex.Message);

                        retryResumeQueue.Enqueue(new Tuple<string, Resume, string>(id, resume, path));

                        return;
                    }
                }
            }

            var uploadSuccessPath = ConfigurationManager.AppSettings["Zhaopin_UploadSuccessFiles_Path"] + companyName;

            if (!Directory.Exists(uploadSuccessPath)) Directory.CreateDirectory(uploadSuccessPath);

            uploadSuccessPath += Path.GetFileName(path);

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
                Tuple<string, Resume, string> resume;

                if (!retryResumeQueue.TryDequeue(out resume)) continue;

                using (var stream = new MemoryStream(GZip.Compress(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(resume.Item2)))))
                {
                    try
                    {
                        newOss.PutObject(newBucket, $"{ConfigurationManager.AppSettings["Oss.New.ResumePath"]}{resume.Item1}", stream);

                        var uploadSuccessPath = ConfigurationManager.AppSettings["Zhaopin_UploadSuccessFiles_Path"] + companyName + Path.GetFileName(resume.Item3);

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
