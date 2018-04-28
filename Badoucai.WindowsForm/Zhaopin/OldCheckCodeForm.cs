using System;
using System.Collections.Generic;
using System.Configuration;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Windows.Forms;
using Badoucai.Business.Zhaopin;
using Badoucai.EntityFramework.MySql;
using Badoucai.Library;
using Microsoft.VisualBasic;
using MySql.Data.MySqlClient;

namespace Badoucai.WindowsForm.Zhaopin
{
    public partial class OldCheckCodeForm : Form
    {
        private static string handleUser = string.Empty;

        private const int range = 20;

        private static readonly Stack<Watermark> locationStack = new Stack<Watermark>();

        private static DateTime? endTime;

        private static CookieContainer cookieContainer = new CookieContainer();

        private static string timestamp = string.Empty;

        private static bool isGetCheckCode;

        private static string compnayName = string.Empty;

        private static readonly string proxyHost = ConfigurationManager.AppSettings["Proxy"];

        public OldCheckCodeForm()
        {
            InitializeComponent();
        }

        private void btn_ReferenceCheckCode_Click(object sender, EventArgs e)
        {
            this.RunAsync(() => ReferenceCheckCode());
        }

        private void pic_Body_MouseClick(object sender, MouseEventArgs e)
        {
            if(this.btn_GetCheckCode.Enabled) return;

            ImageWatermark(e.Location.X - range / 2, e.Location.Y - range / 2);
        }

        private void btn_Checking_Click(object sender, EventArgs e)
        {
            var coordinatParam = string.Empty;

            var list = new List<Watermark>();

            if (!locationStack.Any()) return;

            while (true)
            {
                var watermark = locationStack.Pop();

                if(watermark.Index == 0) break;

                list.Add(watermark);
            }

            list.Reverse();

            coordinatParam = list.Aggregate(coordinatParam, (current, item) => current + $";{item.LocationX + range / 2},{item.LocationY + range / 2}");

            if (coordinatParam.Length == 0)
            {
                coordinatParam = "99,99;99,99;99,99";
            }

            coordinatParam = coordinatParam.Substring(1);

            var checkedResult = RequestFactory.QueryRequest("https://rd.zhaopin.com/resumePreview/captcha/verifyjsonp?callback=jsonpCallback", $"p={HttpUtility.UrlEncode(coordinatParam)}&time={timestamp}", RequestEnum.POST, cookieContainer, host: proxyHost);

            if (!checkedResult.IsSuccess)
            {
                MessageBox.Show(checkedResult.ErrorMsg);

                return;
            }

            var match = Regex.Match(checkedResult.Data, "jsonpCallback\\('(.+?)'\\)");

            if (!match.Success)
            {
                this.lbl_Tip.Text = $"提示：{DateTime.Now:HH:mm:ss} 验证失败！";

                endTime = DateTime.Now.AddMinutes(1);

                var dataResult = ReferenceCheckCode();

                if (!dataResult.IsSuccess) endTime = null;

                return;
            }

            checkedResult = RequestFactory.QueryRequest("https://rd.zhaopin.com/resumePreview/resume/_CheackValidatingCode?validatingCode=" + match.Result("$1"), "", RequestEnum.POST , cookieContainer, host: proxyHost);

            if (!checkedResult.IsSuccess || checkedResult.Data.ToLower().Trim() != "true")
            {
                this.lbl_Tip.Text = $"提示：{DateTime.Now:HH:mm:ss} 验证失败！{checkedResult.ErrorMsg}";

                endTime = DateTime.Now.AddMinutes(1);

                var dataResult = ReferenceCheckCode();

                if (!dataResult.IsSuccess) endTime = null;

                return;
            }

            this.lbl_Tip.Text = $"提示：{DateTime.Now:HH:mm:ss} 验证成功！";

            endTime = null;

            this.btn_GetCheckCode.Enabled = true;

            this.btn_ReferenceCheckCode.Enabled = false;

            this.btn_Checking.Enabled = false;

            this.pic_Body.Image = null;

            this.pic_Header.Image = null;

            using (var db = new MangningXssDBEntities())
            {
                var checkCode = db.ZhaopinCheckCode.FirstOrDefault(f => f.HandleUser == handleUser && f.Status == 1);
                
                if(checkCode == null) return;

                checkCode.Status = 2;

                checkCode.CompleteTime = DateTime.Now;

                db.SaveChanges();
            }

            ReferenceRank();
        }

        private void CheckCodeForm_Load(object sender, EventArgs e)
        {
            handleUser = Interaction.InputBox("登录", "请输入姓名");

            if(string.IsNullOrEmpty(handleUser)) Application.Exit();

            lbl_HandleUser.Text = "当前用户：" + handleUser;

            this.btn_Checking.Enabled = false;

            this.btn_ReferenceCheckCode.Enabled = false;

            isGetCheckCode = false;

            Task.Run(() => ReferenceRank());

            this.btn_GetCheckCode.Enabled = true;

            this.lbl_timer.Text = "倒计时：60s";

            Task.Run(() =>
            {
                while (true)
                {
                    var waitCount = ReferenceWaitCount();

                    this.Invoke((MethodInvoker)delegate
                    {
                        if (this.lbl_WaitCount.Text == "待处理：0" && waitCount > 0)
                        {
                            this.WindowState = FormWindowState.Normal;

                            this.TopMost = true;

                            this.Activate();
                        }

                        this.lbl_WaitCount.Text = "待处理：" + waitCount;
                    });

                    Thread.Sleep(2000);
                }
            });
        }

        private void btn_GetCheckCode_Click(object sender, EventArgs e)
        {
            if(isGetCheckCode) return;

            isGetCheckCode = true;

            using (var db = new MangningXssDBEntities())
            {
                var checkCode = db.ZhaopinCheckCode.FirstOrDefault(f => f.HandleUser == handleUser && (f.Status == 0 || f.Status == 1));

                if (checkCode == null)
                {
                    db.Database.ExecuteSqlCommand("UPDATE XSS_Zhaopin_CheckCode SET HandleUser = @handleUser WHERE Status = 0 AND HandleUser IS NULL LIMIT 1", new MySqlParameter("@handleUser", handleUser));

                    checkCode = db.ZhaopinCheckCode.FirstOrDefault(f => f.HandleUser == handleUser && (f.Status == 0 || f.Status == 1));
                }

                if (checkCode == null)
                {
                    this.lbl_Tip.Text = $"提示：{DateTime.Now:HH:mm:ss} 无验证码待输入！";

                    isGetCheckCode = false;

                    return;
                }

                checkCode.Status = 1;

                db.SaveChanges();

                this.RunAsync(() => GetCheckCode(checkCode));
            }
        }

        /// <summary>
        /// 图片水印
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        private void ImageWatermark(int x, int y)
        {
            var location = locationStack.FirstOrDefault(f => Math.Abs(f.LocationX - x) <= range && Math.Abs(f.LocationY - y) <= range);

            Watermark watermark;

            if (location != null)
            {
                while (true)
                {
                    watermark = locationStack.Pop();

                    if (watermark.Index == location.Index) break;
                }

                watermark = locationStack.Peek().Clone();

                this.pic_Body.Image = watermark.Image;

                return;
            }

            watermark = locationStack.Peek().Clone();

            watermark.LocationX = x;

            watermark.LocationY = y;

            AddImageWatermark(watermark);

            this.btn_Checking.Enabled = true;
        }

        /// <summary>
        /// 添加标记
        /// </summary>
        /// <param name="watermark"></param>
        private void AddImageWatermark(Watermark watermark)
        {
            var gs = Graphics.FromImage(watermark.Image);

            var font = new Font("宋体", range, FontStyle.Bold);

            Brush br = new SolidBrush(Color.Magenta);

            gs.DrawString((++watermark.Index).ToString(), font, br, watermark.LocationX, watermark.LocationY);

            gs.Dispose();

            this.pic_Body.Image = watermark.Image;

            locationStack.Push(watermark);
        }

        /// <summary>
        /// 获取验证码
        /// </summary>
        /// <param name="checkCodeModel"></param>
        private void GetCheckCode(ZhaopinCheckCode checkCodeModel)
        {
            cookieContainer = checkCodeModel.Cookie.Serialize(".zhaopin.com");

            compnayName = checkCodeModel.Account;

            var dataResult = ReferenceCheckCode();

            if (!dataResult.IsSuccess) return;

            this.RunInMainthread(() =>
            {
                this.btn_GetCheckCode.Enabled = false;

                this.btn_ReferenceCheckCode.Enabled = true;

                this.RunAsync(StartTimer);

                this.lbl_Tip.Text = $"提示：{DateTime.Now:HH:mm:ss} 获取验证码成功！";
            });
        }

        /// <summary>
        /// 启动验证计时器
        /// </summary>
        private void StartTimer()
        {
            while (true)
            {
                Thread.Sleep(1000);

                if (endTime == null)
                {
                    this.RunInMainthread(() =>
                    {
                        this.lbl_timer.Text = "倒计时：60s";
                    });

                    return;
                }

                var seconds = endTime.Value.Subtract(DateTime.Now).Seconds;

                this.RunInMainthread(() =>
                {
                    this.lbl_timer.Text = $"倒计时：{seconds}s";
                });

                if (seconds <= 0) break;
            }

            using (var db = new MangningXssDBEntities())
            {
                var checkCode = db.ZhaopinCheckCode.FirstOrDefault(f => f.HandleUser == handleUser && f.Status != 2);

                if (checkCode != null)
                {
                    checkCode.HandleUser = null;

                    checkCode.Status = 0;

                    db.SaveChanges();
                }
            }

            this.RunInMainthread(() =>
            {
                this.btn_Checking.Enabled = false;

                this.btn_GetCheckCode.Enabled = true;

                this.btn_ReferenceCheckCode.Enabled = false;

                this.pic_Body.Image = null;

                this.pic_Header.Image = null;

                this.lbl_Tip.Text = $"提示：{DateTime.Now:HH:mm:ss} 输入超时，请重新获取验证码！";
            });
        }

        /// <summary>
        /// 刷新验证码
        /// </summary>
        /// <returns></returns>
        private DataResult ReferenceCheckCode()
        {
            // 准备颜色变换矩阵的元素
            float[][] colorMatrixElements = {
                new float[] {-1, 0, 0, 0, 0},
                new float[] {0, -1, 0, 0, 0},
                new float[] {0, 0, -1, 0, 0},
                new float[] {0, 0, 0, 1, 0},
                new float[] {1, 1, 1, 0, 1}
            };

            // 为 ImageAttributes 设置颜色变换矩阵

            var colorMatrix = new ColorMatrix(colorMatrixElements);

            var imageAttributes = new ImageAttributes();

            imageAttributes.SetColorMatrix(colorMatrix, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);

            locationStack.Clear();

            var streamResult = HttpClientFactory.RequestForStream("https://rd.zhaopin.com/resumePreview/captcha/getcap?t1515065150298", HttpMethod.Get, cookieContainer: cookieContainer);

            if (!streamResult.IsSuccess)
            {
                this.RunInMainthread(() =>
                {
                    this.lbl_Tip.Text = $"提示：{DateTime.Now:HH:mm:ss} 未获取到验证码！{streamResult.ErrorMsg}";
                });

                isGetCheckCode = false;

                return new DataResult("未获取到验证码!");
            }

            var codeStream = streamResult.Data;

            timestamp = (DateTime.Now.ToUniversalTime().Ticks - 621355968000000000) / 10000000 + new Random().Next(0, 1000).ToString("000");

            // 将 ImageAttributes 应用于绘制

            Bitmap picBody;

            try
            {
                picBody = Image.FromHbitmap(ImageHelper.GetValidCode_Zhaopin(Image.FromStream(codeStream)).GetHbitmap());
            }
            catch (Exception)
            {
                using (var db = new MangningXssDBEntities())
                {
                    var checkCode = db.ZhaopinCheckCode.FirstOrDefault(f => f.HandleUser == handleUser && f.Status != 2);

                    if (checkCode != null)
                    {
                        db.ZhaopinCheckCode.Remove(checkCode);

                        db.SaveChanges();
                    }

                    this.RunInMainthread(() =>
                    {
                        this.lbl_Tip.Text = $"提示：{DateTime.Now:HH:mm:ss} Cookie 过期！CompanyName = {compnayName}";
                    });

                    isGetCheckCode = false;

                    return new DataResult("未获取到验证码!");
                }
            }

            var imageBody = new Bitmap(picBody.Width, picBody.Height);

            var graphics = Graphics.FromImage(imageBody);

            graphics.DrawImage(picBody, new Rectangle(0, 0, picBody.Width, picBody.Height), 0, 0, picBody.Width, picBody.Height, GraphicsUnit.Pixel, imageAttributes);

            var picHeader = Image.FromHbitmap(ImageHelper.GetValidCodeSource_Zhaopin(Image.FromStream(codeStream)).GetHbitmap());

            var imageHeader = new Bitmap(picHeader.Width, picHeader.Height);

            graphics = Graphics.FromImage(imageHeader);

            graphics.DrawImage(picHeader, new Rectangle(0, 0, picHeader.Width, picHeader.Height), 0, 0, picHeader.Width, picHeader.Height, GraphicsUnit.Pixel, imageAttributes);

            graphics.Dispose();

            this.pic_Header.Image = imageHeader;

            this.pic_Body.Image = imageBody;

            endTime = DateTime.Now.AddMinutes(1);

            locationStack.Push(new Watermark
            {
                Index = 0,
                Image = this.pic_Body.Image,
                LocationX = -999,
                LocationY = -999
            });

            isGetCheckCode = false;

            return new DataResult();
        }

        /// <summary>
        /// 刷新排行
        /// </summary>
        private void ReferenceRank()
        {
            try
            {
                var todayData = GetTodayRank();

                var arr = todayData.ToArray();

                arr = arr.OrderByDescending(o => o.Value).ToArray();

                this.Invoke((MethodInvoker)delegate
                {
                    this.lbx_TodayRank.Items.Clear();

                    this.lbx_TodayRank.Items.Add("排名\t姓名\t数量");

                    for (var i = 0; i < arr.Length; i++)
                    {
                        this.lbx_TodayRank.Items.Add($"{i + 1}\t{arr[i].Key}\t{arr[i].Value}");
                    }
                });

                var totalData = GetTotalRank();

                this.RunInMainthread(() =>
                {
                    this.lbx_TotalRank.Items.Clear();

                    arr = totalData.ToArray();

                    arr = arr.OrderByDescending(o => o.Value).ToArray();

                    this.lbx_TotalRank.Items.Add("排名\t姓名\t数量");

                    for (var i = 0; i < arr.Length; i++)
                    {
                        this.lbx_TotalRank.Items.Add($"{i + 1}\t{arr[i].Key}\t{arr[i].Value}");
                    }
                });
            }
            catch (Exception ex)
            {
                while (true)
                {
                    if (ex.InnerException == null) break;

                    ex = ex.InnerException;
                }

                LogFactory.Warn(ex.Message);
            }
        }

        /// <summary>
        /// 刷新验证码队列等待数
        /// </summary>
        /// <returns></returns>
        public int ReferenceWaitCount()
        {
            try
            {
                using (var db = new MangningXssDBEntities())
                {
                    return db.ZhaopinCheckCode.Count(c => c.Status == 0 || c.HandleUser == handleUser && c.Status == 1);
                }
            }
            catch (Exception)
            {
                return 0;
            }
        }

        private static Dictionary<string, int> GetTodayRank()
        {
            using (var db = new MangningXssDBEntities())
            {
                return db.ZhaopinCheckCode
                    .Where(w => w.CompleteTime > DateTime.Today && w.Status == 2)
                    .GroupBy(s => new { s.HandleUser })
                    .Select(s => new { s.Key.HandleUser, Count = s.Count() })
                    .ToDictionary(a => a.HandleUser, b => b.Count);

            }
        }

        private static Dictionary<string, int> GetTotalRank()
        {
            using (var db = new MangningXssDBEntities())
            {
                return db.ZhaopinCheckCode
                    .Where(w => w.Status == 2)
                    .GroupBy(s => new { s.HandleUser })
                    .Select(s => new { s.Key.HandleUser, Count = s.Count() })
                    .ToDictionary(a => a.HandleUser, b => b.Count);
            }
        }
    }
}
