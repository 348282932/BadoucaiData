using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Windows.Forms;
using Badoucai.Business.Model;
using Badoucai.Business.Zhaopin;
using Badoucai.EntityFramework.MySql;
using Badoucai.EntityFramework.PostgreSql.Crawler_DB;
using Badoucai.Library;
using Newtonsoft.Json;

namespace Badoucai.WindowsForm.Tools
{
    public partial class ProgramCollectionForm : Form
    {
        public ProgramCollectionForm()
        {
            InitializeComponent();
        }

        private void btn_DownloadNewZPResume_Click(object sender, EventArgs e)
        {
            this.RunAsync(UpdateWork);
        }

        private void DownloadNewZPResume()
        {
            const string cookie = "dywez=95841923.1511179053.1.1.dywecsr=(direct)|dyweccn=(direct)|dywecmd=(none)|dywectr=undefined; UM_distinctid=15fd950d4002e-078f03d618be3a-7b113d-1fa400-15fd950d401b16; companyCuurrentCity=765; __zpWAM=1511179690788.221570.1511179691.1511179691.1; __zpWAMs2=1; LastCity=%e6%b7%b1%e5%9c%b3; LastCity%5Fid=765; _jzqckmp=1; monitorlogin=Y; firstchannelurl=https%3A//passport.zhaopin.com/%3Fy7bRbP%3DdrD8ktbNjFbNjFbNAGbM.F5i4vPMMvKO_E1CyE_.RF9; JSweixinNum=3; usermob=46654A2C4576447646764065442C44764176417643655; userphoto=; userwork=11; bindmob=1; rinfo=JL050895042R90500000000_1; dywem=95841923.y; _jzqx=1.1511246953.1511246953.1.jzqsr=passport%2Ezhaopin%2Ecom|jzqct=/account/logout.-; urlfrom=121126445; urlfrom2=121126445; adfcid=none; adfcid2=none; adfbid=0; adfbid2=0; Hm_lvt_38ba284938d5eddca645bb5e02a02006=1511179417; Hm_lpvt_38ba284938d5eddca645bb5e02a02006=1511246974; _jzqa=1.3386187871408075000.1511226539.1511226539.1511246953.2; _jzqc=1; _jzqb=1.2.10.1511246953.1; JsOrglogin=2049139034; at=fc522dbb32ce478eb97b33e0042dff42; Token=fc522dbb32ce478eb97b33e0042dff42; rt=608743d8649f44468d71c79c80ba3d93; uiioit=2264202C55795C6900374679586B4364522C5B795E690C374E792A6B3364592C5D795A69003743795A6B4764552C5A7951695; lastchannelurl=https%3A//passport.zhaopin.com/org/login; __utma=269921210.1889805250.1511179053.1511226539.1511246953.3; __utmb=269921210.10.9.1511246998154; __utmc=269921210; __utmz=269921210.1511179053.1.1.utmcsr=(direct)|utmccn=(direct)|utmcmd=(none); __utmv=269921210.|2=Member=204885552=1; JsNewlogin=683046344; NewPinLoginInfo=; cgmark=2; NewPinUserInfo=; isNewUser=1; RDpUserInfo=; ihrtkn=; utype=2; nTalk_CACHE_DATA={uid:kf_9051_ISME9754_guest713A8EDA-E83E-25,tid:1511247000031838}; NTKF_T2D_CLIENTID=guest713A8EDA-E83E-256A-17CC-D94EA8BBD23C; RDsUserInfo=236A2D6B566A5C6450695A7555725173477659695F6B5E6A5F6825693B654F651B710E6A5B6B466A58645F69527530722B734C76EBE13B304B0A5F6827693B654F654871346A2D6B566A5F64586951755372577341765B69596B516A2668276948655F2B1CFE8B3C2CFDBA13EA06650EC8276C1535E51D27923A9A06593D623A05388D36496527713B6A546B5A6A2C6453692C752872587308761F690A6B046A1E6800690C651B653371156A016B026A046409691A750A72037305760369056B096A4A680A691A651F654871256A3D6B566A5864536928753172587340765B69466B586A55684969446546654971436A5B6B506A2E642C69547554725473437653695D6B596A53685A69406549653771386A546B5302A93B5A6981E55E7229733C7657695B6B5A6A546858694565426542714F6A526B286A2E6455695E75537256734A762B69276B576A54685269206533654E714D6A2A6B2A6A57642B692A755072557349765C69596B5A6A55685B69436549653771376A546B286A29645D6959755D72537343765A695A6B586A52682D694C6542654171436A596B5B6A5B6451695975557255734A762E69286B576A5468526926653B654E71456A526B226A3A6455695B75517257735F765B69586B536A5F683C6921654F654271466A5B6B506A2; rd_applyorderBy=CreateTime; rd_apply_lastsearchcondition=11%3B12%3B13%3B14%3B15%3B16; dywea=95841923.4452105838603920000.1511179053.1511226539.1511246953.3; dywec=95841923; dyweb=95841923.63.9.1511248182640";

            var cookieContainer = cookie.Serialize(".zhaopin.com");

            var queue = new ConcurrentQueue<string>();

            var tasks = new List<Task>();

            Task.Run(() =>
            {
                var pageIndex = 1;

                var typeIndex = 1;

                while (true)
                {
                    #region 参数

                    var paramDic = new Dictionary<string, string>
                    {
                        { "PageList2", "" },
                        { "DColumn_hidden", "" },
                        { "searchKeyword", "" },
                        { "curSubmitRecord", "4461" },
                        { "curMaxPageNum", "224" },
                        { "buttonAsse", "导入测评系统" },
                        { "buttonInfo", "发通知信" },
                        { "SF_1_1_50", $"{typeIndex}" },
                        { "SF_1_1_51", "-1" },
                        { "SF_1_1_45", "" },
                        { "SF_1_1_44", "" },
                        { "SF_1_1_52", "0" },
                        { "SF_1_1_49", "0" },
                        { "IsInvited", "0" },
                        { "position_city", "[%%POSITION_CITY%%]" },
                        { "deptName", "" },
                        { "select_unique_id", "" },
                        { "selectedResumeList", "" },
                        { "PageNo", "" },
                        { "PosState", "" },
                        { "MinRowID", "" },
                        { "MaxRowID", "2722819791" },
                        { "RowsCount", "123" },
                        { "PagesCount", "5" },
                        { "PageType", "0" },
                        { "CurrentPageNum", $"{pageIndex}" },
                        { "Position_IDs", "[%%POSITION_IDS%%]" },
                        { "Position_ID", "[%%POSITION_ID%%]" },
                        { "SortType", "0" },
                        { "isCmpSum", "0" },
                        { "SelectIndex_Opt", "0" },
                        { "Resume_count", "0" },
                        { "CID", "112963735" },
                        { "forwardingEmailList", "" },
                        { "click_search_op_type", "-1" },
                        { "X-Requested-With", "XMLHttpRequest" }
                    };

                    #endregion

                    var requestResult = HttpClientFactory.RequestForString("https://rd2.zhaopin.com/rdapply/resumes/apply/search?SF_1_1_38=6%2C9&orderBy=CreateTime", HttpMethod.Post, paramDic, cookieContainer, isRandomIP: true);

                    if (!requestResult.IsSuccess)
                    {
                        this.AsyncSetLog(this.tbx_Log,requestResult.ErrorMsg);

                        continue;
                    }

                    if (requestResult.Data.Contains("当前状态下暂无简历，您可以点击其他状态查看简历"))
                    {
                        this.AsyncSetLog(this.tbx_Log, $"第 {typeIndex} 类 已扫描完成！");

                        if(typeIndex >= 4) break;

                        typeIndex++;

                        pageIndex = 1;

                        continue;
                    }

                    foreach (Match match in Regex.Matches(requestResult.Data, "href=\"(//rd\\.zhaopin\\.com/resumepreview.+?)\""))
                    {
                        queue.Enqueue("https:" + match.Result("$1"));
                    }

                    this.AsyncSetLog(this.tbx_Log, $"已扫描 {pageIndex} 页");

                    SpinWait.SpinUntil(()=>!queue.Any());

                    pageIndex++;
                }
            });

            var dictionary = new ConcurrentDictionary<string, string>();

            for (var i = 0; i < 1; i++)
            {
                tasks.Add(Task.Run(() =>
                {
                    while (true)
                    {
                        string url;

                        if(!queue.TryDequeue(out url)) continue;

                        Thread.Sleep(TimeSpan.FromSeconds(20));

                        var requestResult = HttpClientFactory.RequestForString(url, HttpMethod.Get, null, cookieContainer, isRandomIP: true);

                        if (!requestResult.IsSuccess)
                        {
                            this.AsyncSetLog(this.tbx_Log, requestResult.ErrorMsg);

                            LogFactory.Warn(requestResult.ErrorMsg);

                            continue;
                        }

                        var matchResult = Regex.Match(requestResult.Data, "left-tips-id\">ID:&nbsp;(.+?)</");

                        if (!matchResult.Success)
                        {
                            this.AsyncSetLog(this.tbx_Log, $"匹配 ResumeNumber 失败! url=>{url}");

                            LogFactory.Warn($"匹配 ResumeNumber 失败! url=>{url}");

                            continue;
                        }

                        var resumeNumber = matchResult.Result("$1");

                        if (!dictionary.TryAdd(resumeNumber, resumeNumber))
                        {
                            this.AsyncSetLog(this.tbx_Log, $"去重! ResumeNumber=>{resumeNumber}");

                            continue;
                        }

                        File.WriteAllText($@"D:\Badoucai\OldZhaopinResume\50862012\{resumeNumber}.txt", requestResult.Data);

                        this.AsyncSetLog(this.tbx_Log, $"下载成功! ResumeNumber=>{resumeNumber}");
                    }
                }));
            }

            Task.WaitAll(tasks.ToArray());
        }

        /// <summary>
        /// 过滤恢武成都简历
        /// </summary>
        private void FilterChengDuResumes()
        {
            var sb = new StringBuilder();

            sb.AppendLine("姓名\t手机\t邮箱\t年龄\t学历\t更新日期\t性别\t平台\t地点");

            var cellphones = File.ReadAllLines(@"D:\给过的手机号.txt");

            var educationDic = new Dictionary<int, string>
            {
                { 0, "无学历" },
                { 1, "初中" },
                { 2, "中技" },
                { 3, "高中" },
                { 4, "中专" },
                { 5, "大专" }
            };

            using (var db = new BadoucaiDBEntities())
            {
                var resumes = db.SpiderResumeDownload.Where(w => w.Weight == 4 && w.Cellphone > 0).ToList();

                var index = 0;

                var count = 0;

                foreach (var resume in resumes)
                {
                    ++index;

                    if (cellphones.Contains(resume.Cellphone.ToString())) continue;

                    var resumeJson = JsonConvert.DeserializeObject<dynamic>(File.ReadAllText("Y:" + resume.SavePath.Replace("/", "\\")));

                    var age = (int)resumeJson.userDetials.birthYear;

                    var gender = (int)resumeJson.userDetials.gender;

                    var currentEducationLevel = resumeJson.detialJSonStr.MaxEducationLevel != null ? (int)resumeJson.detialJSonStr.MaxEducationLevel : (int)resumeJson.detialJSonStr.CurrentEducationLevel;

                    if (age < 18 || age > 26 || gender != 1 || currentEducationLevel > 5) continue;

                    sb.AppendLine($"{(string)resumeJson.userDetials.userName}\t{resume.Cellphone}\t{resume.Email}\t{age}\t{educationDic[currentEducationLevel]}\t{resume.UpdateTime}\t男\t智联\t成都");

                    this.AsyncSetLog(this.tbx_Log, $"{index}/{resumes.Count} 成功：{++count}");
                }

                File.WriteAllText(@"D:\成都（恢武）.txt", sb.ToString());

                this.AsyncSetLog(this.tbx_Log, "完成");
            }
        }

        private void UpdateWork()
        {
            var resumeJson = File.ReadAllText(Directory.CreateDirectory(AppDomain.CurrentDomain.BaseDirectory).Parent?.Parent?.FullName + "/App_Data/Zhaopin/ResumeTemplates/Resume_v1.json");

            var baseResumeModel = JsonConvert.DeserializeObject<BaseResumeModel>(resumeJson);

            var index = 0;

            using (var db = new MangningXssDBEntities())
            {
                var userResumeList = db.ZhaopinResume
                    .Join(db.ZhaopinUser.Where(w => !string.IsNullOrEmpty(w.Cookie) && w.Source.Contains("MANUAL") && string.IsNullOrEmpty(w.Status)), a => a.UserId, b => b.Id, (a, b) => new { a.Id, b.Email, a.UserId, ResumeNumber = a.DeliveryNumber, b.Cookie })
                    .ToList();

                foreach (var item in userResumeList)
                {
                    InsertXssJs(item.ResumeNumber, item.Id.ToString(), baseResumeModel, "", item.Cookie.Serialize("zhaopin.com"), item.Email);

                    this.AsyncSetLog(this.tbx_Log, $"{item.Email}修改简历成功！{++index}/{userResumeList.Count}");
                }
            }
        }

        public void InsertXssJs(string extendedId, string resumeId, BaseResumeModel resume, string xssJs, CookieContainer cookieContainer, string email)
        {
            var param = $"Language_ID=1&ext_id={extendedId}&Resume_ID={resumeId}&Version_Number=1&RowID=0&SaveType=0&cmpany_name={HttpUtility.UrlEncode(resume.CompanyName)}&industry={resume.CompanyIndustry}&customSubJobtype={HttpUtility.UrlEncode(resume.JobTitle)}&SchJobType={resume.JobType}&subJobType={resume.SubJobType}&jobTypeMain={resume.JobType}&subJobTypeMain={resume.SubJobType}&workstart_date_y={resume.WorkDateStartYear.Replace("年", "")}&workstart_date_m={resume.WorkDateStartMonth.Replace("月", "")}&workend_date_y={resume.WorkDateEndYear.Replace("年", "")}&workend_date_m={resume.WorkDateEndMonth.Replace("月", "")}&salary_scope={resume.Salary}&job_description={HttpUtility.UrlEncode(resume.WorkDescription + xssJs)}&company_type=&company_size=";

            var dataResult = RequestFactory.QueryRequest("https://i.zhaopin.com/Resume/WorkExperienceEdit/Save", param, RequestEnum.POST, cookieContainer);

            if (!dataResult.IsSuccess || dataResult.Data.Contains("接口调用异常"))
            {
                this.AsyncSetLog(this.tbx_Log, $"{email} 修改简历失败！" + dataResult.ErrorMsg);
            }
        }

        private void btn_Statistics_Click(object sender, EventArgs e)
        {
            var business = new DataStatisticsBusiness();

            business.StatisticsByArea();

            this.AsyncSetLog(this.tbx_Log, "统计完成！");
        }

        private void btn_zhaopin_Resume_Click(object sender, EventArgs e)
        {
            var business = new MatchResumeLocationBusiness();

            business.MatchResume();
        }

        private void btn_Export_Click(object sender, EventArgs e)
        {
            const string liantong = @"D:\360安全浏览器下载\联通NEW.csv";

            const string dianxin = @"D:\360安全浏览器下载\电信NEW.csv";

            var ltlist = new List<string>();

            for (var i = 0; i < 3; i++)
            {
                ltlist.AddRange(File.ReadAllLines($@"D:\360安全浏览器下载\联通{i}.txt"));
            }

            var dxlist = new List<string>();

            for (var i = 0; i < 3; i++)
            {
                dxlist.AddRange(File.ReadAllLines($@"D:\360安全浏览器下载\电信{i}.txt"));
            }

            var ltArr = File.ReadAllLines(liantong).ToList();

            var dxArr = File.ReadAllLines(dianxin).ToList();

            var rlt = ltArr.AsParallel().Where(r => ltlist.Contains(r)).ToList();
            var rdx = dxArr.AsParallel().Where(r => dxlist.Contains(r)).ToList();

            foreach (var s in rlt)
            {
                ltArr.Remove(s);
            }

            foreach (var s in rdx)
            {
                dxArr.Remove(s);
            }

            var ltSb = new StringBuilder();

            for (var i = 0; i < ltArr.Count; i++)
            {
                ltSb.AppendLine(ltArr[i]);

                if ((i + 1) % 10000 == 0 && i!= 0)
                {
                    File.WriteAllText($@"D:\360安全浏览器下载\联通NEW{i/10000}.txt", ltSb.ToString());

                    ltSb = new StringBuilder();
                }
            }

            var dxSb = new StringBuilder();

            for (var i = 0; i < dxArr.Count; i++)
            {
                dxSb.AppendLine(dxArr[i]);

                if ((i +1) % 10000 == 0 && i != 0)
                {
                    File.WriteAllText($@"D:\360安全浏览器下载\电信NEW{i / 10000}.txt", dxSb.ToString());

                    dxSb = new StringBuilder();
                }
            }
        }

        private void btn_SearchOldResume_Click(object sender, EventArgs e)
        {
            var business = new SearchOldResumeBusiness();

            this.RunAsync(()=>{

                business.Search();
            });
        }

        private void btn_ImprotOldResume_Click(object sender, EventArgs e)
        {
            var business = new OldResumeImprotBusiness();

            this.RunAsync(() =>
            {
                business.Improt();
            });

            this.RunAsync(() => 
            {
                while (true)
                {
                    this.AsyncSetLog(this.tbx_Log,$"{business.count}"); 

                    Thread.Sleep(3000);
                }
            });
        }
    }
}
