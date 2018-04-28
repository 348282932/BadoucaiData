using System;
using System.Collections.Generic;
using System.Configuration;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Windows.Forms;
using Badoucai.Business.Socket;
using Badoucai.Business.Zhaopin;
using Badoucai.EntityFramework.MySql;
using Badoucai.Library;
using Microsoft.VisualBasic;

namespace Badoucai.WindowsForm.Zhaopin
{
    public partial class CheckCodeForm : Form
    {
        private static string handleUser = string.Empty;

        private static byte[] handleUserBytes = Encoding.UTF8.GetBytes(handleUser);

        private const int range = 20;

        private static readonly Stack<Watermark> locationStack = new Stack<Watermark>();

        private static DateTime? endTime;

        private static CookieContainer cookieContainer = new CookieContainer();

        private readonly CheckCodeBusiness business = new CheckCodeBusiness();

        private Socket socket;

        private static string timestamp = string.Empty;

        private static int checkCodeId;

        private static bool isGetCheckCode;

        private static string account;

        private static short codeType;

        private static bool isChecked;

        private static readonly string webProxyIp = ConfigurationManager.AppSettings["WebProxyIp"];

        private static readonly int webProxyPort = Convert.ToInt32(ConfigurationManager.AppSettings["WebProxyPort"]);

        public CheckCodeForm()
        {
            InitializeComponent();
        }

        private void btn_ReferenceCheckCode_Click(object sender, EventArgs e)
        {
            this.RunAsync(() => ReferenceCheckCode());
        }

        private void pic_Body_MouseClick(object sender, MouseEventArgs e)
        {
            if(this.btn_GetCheckCode.Enabled || this.btn_Connecting.Enabled) return;

            ImageWatermark(e.Location.X - range / 2, e.Location.Y - range / 2);
        }

        private void btn_Checking_Click(object sender, EventArgs e)
        {
            var accountBytes = Encoding.UTF8.GetBytes(account);

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

            if (codeType == 2)
            {
                var coordinatParamBytes = Encoding.UTF8.GetBytes(coordinatParam);

                this.socket.Send(new CheckCoordinatPackage { Id = 0x09, Length = (short)(11 + coordinatParamBytes.Length + accountBytes.Length), Account = accountBytes, CheckCodeId = checkCodeId, CoordinatValue = coordinatParamBytes }.Serialize());

                this.btn_ReferenceCheckCode.Enabled = false;

                this.lbl_Tip.Text = "提示：正在验证...";

                this.RunAsync(() =>
                {
                    while (true)
                    {
                        if(isChecked) break;

                        Thread.Sleep(100);
                    }

                    this.btn_ReferenceCheckCode.Enabled = true;
                });

                return;
            }

            var checkedResult = RequestFactory.QueryRequest("https://rd.zhaopin.com/resumePreview/captcha/verifyjsonp?callback=jsonpCallback", $"p={HttpUtility.UrlEncode(coordinatParam)}&time={timestamp}", RequestEnum.POST, cookieContainer);

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

                try
                {
                    this.socket.Send(new CheckedResultPackage { Id = 0x03, Length = (short)(13 + handleUserBytes.Length + accountBytes.Length), Status = 2, CheckCodeId = checkCodeId, Account = accountBytes, HandleUser = handleUserBytes }.Serialize());
                }
                catch (Exception ex)
                {
                    LogFactory.Warn(ex.Message);

                    this.RunInMainthread(() =>
                    {
                        this.lbl_Tip.Text = $"提示：{DateTime.Now:HH:mm:ss} 连接服务器失败！请稍后再试！";

                        this.btn_Connecting.Enabled = true;

                        this.btn_Checking.Enabled = false;

                        this.btn_GetCheckCode.Enabled = false;

                        this.btn_ReferenceCheckCode.Enabled = false;
                    });

                    return;
                }

                var dataResult = ReferenceCheckCode();

                if (!dataResult.IsSuccess) endTime = null;

                return;
            }

            checkedResult = RequestFactory.QueryRequest("https://rd.zhaopin.com/resumePreview/resume/_CheackValidatingCode?validatingCode=" + match.Result("$1"), "", RequestEnum.POST , cookieContainer, host: $"{webProxyIp}:{webProxyPort}");

            if (!checkedResult.IsSuccess)
            {
                this.lbl_Tip.Text = $"提示：{DateTime.Now:HH:mm:ss} 验证失败！{checkedResult.ErrorMsg}";

                endTime = DateTime.Now.AddMinutes(1);

                try
                {
                    this.socket.Send(new CheckedResultPackage { Id = 0x03, Length = (short)(13 + handleUserBytes.Length + accountBytes.Length), Status = 2, CheckCodeId = checkCodeId, Account = accountBytes, HandleUser = handleUserBytes }.Serialize());
                }
                catch (Exception ex)
                {
                    LogFactory.Warn(ex.Message);

                    this.RunInMainthread(() =>
                    {
                        this.lbl_Tip.Text = $"提示：{DateTime.Now:HH:mm:ss} 连接服务器失败！请稍后再试！";

                        this.btn_Connecting.Enabled = true;

                        this.btn_Checking.Enabled = false;

                        this.btn_GetCheckCode.Enabled = false;

                        this.btn_ReferenceCheckCode.Enabled = false;
                    });

                    return;
                }

                var dataResult = ReferenceCheckCode();

                if (!dataResult.IsSuccess) endTime = null;

                return;
            }

            if (checkedResult.Data.ToLower().Trim() != "true")
            {
                this.lbl_Tip.Text = $"提示：{DateTime.Now:HH:mm:ss} 验证失败！";

                endTime = DateTime.Now.AddMinutes(1);

                try
                {
                    this.socket.Send(new CheckedResultPackage { Id = 0x03, Length = (short)(13 + handleUserBytes.Length + accountBytes.Length), Status = 2, CheckCodeId = checkCodeId, Account = accountBytes, HandleUser = handleUserBytes }.Serialize());
                }
                catch (Exception ex)
                {
                    LogFactory.Warn(ex.Message);

                    this.RunInMainthread(() =>
                    {
                        this.lbl_Tip.Text = $"提示：{DateTime.Now:HH:mm:ss} 连接服务器失败！请稍后再试！";

                        this.btn_Connecting.Enabled = true;

                        this.btn_Checking.Enabled = false;

                        this.btn_GetCheckCode.Enabled = false;

                        this.btn_ReferenceCheckCode.Enabled = false;
                    });

                    return;
                }

                var dataResult = ReferenceCheckCode();

                if (!dataResult.IsSuccess) endTime = null;

                return;
            }

            try
            {
                this.socket.Send(new CheckedResultPackage { Id = 0x03, Length = (short)(13 + handleUserBytes.Length + accountBytes.Length), Status = 1, CheckCodeId = checkCodeId, Account = accountBytes, HandleUser = handleUserBytes }.Serialize());
            }
            catch (Exception ex)
            {
                LogFactory.Warn(ex.Message);

                this.RunInMainthread(() =>
                {
                    this.lbl_Tip.Text = $"提示：{DateTime.Now:HH:mm:ss} 连接服务器失败！请稍后再试！";

                    this.btn_Connecting.Enabled = true;

                    this.btn_Checking.Enabled = false;

                    this.btn_GetCheckCode.Enabled = false;

                    this.btn_ReferenceCheckCode.Enabled = false;
                });

                return;
            }

            this.lbl_Tip.Text = $"提示：{DateTime.Now:HH:mm:ss} 验证成功！";

            endTime = null;

            this.btn_GetCheckCode.Enabled = true;

            this.btn_ReferenceCheckCode.Enabled = false;

            this.btn_Checking.Enabled = false;

            this.pic_Body.Image = null;

            this.pic_Header.Image = null;
        }

        private void btn_Connecting_Click(object sender, EventArgs e)
        {
            this.btn_Connecting.Enabled = false;

            isGetCheckCode = false;

            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            var port = Convert.ToInt32(ConfigurationManager.AppSettings["ListenerPort"]);

            var ipAddress = ConfigurationManager.AppSettings["ListenerIpAddress"];

            try
            {
                socket.Connect(ipAddress, port);
            }
            catch (Exception ex)
            {
                LogFactory.Warn(ex.Message);

                this.lbl_Tip.Text = $"提示：{DateTime.Now:HH:mm:ss} 连接服务器失败！请稍后再试！";

                this.btn_Connecting.Enabled = true;

                socket.Dispose();

                return;
            }

            Task.Run(() =>
            {
                while (true)
                {
                    Thread.Sleep(TimeSpan.FromMinutes(4));

                    try
                    {
                        socket.Send(new HeartbeatPackage().Serialize());
                    }
                    catch (Exception ex)
                    {
                        LogFactory.Warn(ex.Message);

                        this.RunInMainthread(() =>
                        {
                            this.lbl_Tip.Text = $"提示：{DateTime.Now:HH:mm:ss} 连接服务器失败！请稍后再试！";

                            this.btn_Connecting.Enabled = true;

                            this.btn_Checking.Enabled = false;

                            this.btn_GetCheckCode.Enabled = false;

                            this.btn_ReferenceCheckCode.Enabled = false;

                            socket.Dispose();
                        });

                        return;
                    }
                }
            });

            Task.Run(() =>
            {
                while (true)
                {
                    Thread.Sleep(TimeSpan.FromSeconds(3));

                    try
                    {
                        this.socket.Send(new Package
                        {
                            Id = 0x01,
                            Length = 3
                        }.Serialize());
                    }
                    catch (Exception ex)
                    {
                        LogFactory.Warn(ex.Message);

                        this.RunInMainthread(() =>
                        {
                            this.lbl_Tip.Text = $"提示：{DateTime.Now:HH:mm:ss} 连接服务器失败！请稍后再试！";

                            this.btn_Connecting.Enabled = true;

                            this.btn_Checking.Enabled = false;

                            this.btn_GetCheckCode.Enabled = false;

                            this.btn_ReferenceCheckCode.Enabled = false;
                        });

                        return;
                    }
                }
            });

            Task.Run(() =>
            {
                try
                {
                    ListenerServer();

                    ReferenceRank();
                }
                catch (Exception ex)
                {
                    while (true)
                    {
                        if(ex.InnerException == null) break;

                        ex = ex.InnerException;
                    }

                    LogFactory.Warn(ex.Message);

                    this.RunInMainthread(() =>
                    {
                        this.lbl_Tip.Text = $"提示：{DateTime.Now:HH:mm:ss} 连接断开！请查看日志！";

                        this.btn_Connecting.Enabled = true;

                        this.btn_Checking.Enabled = false;

                        this.btn_GetCheckCode.Enabled = false;

                        this.btn_ReferenceCheckCode.Enabled = false;
                    });
                }
            });

            this.btn_Checking.Enabled = false;

            this.btn_GetCheckCode.Enabled = true;

            this.btn_ReferenceCheckCode.Enabled = false;

            this.lbl_Tip.Text = $"提示：{DateTime.Now:HH:mm:ss} 连接服务器成功！";

            this.lbl_timer.Text = "倒计时：60s";

        }

        private void CheckCodeForm_Load(object sender, EventArgs e)
        {
            handleUser = Interaction.InputBox("登录", "请输入姓名");

            if(string.IsNullOrEmpty(handleUser)) Application.Exit();

            handleUserBytes = Encoding.UTF8.GetBytes(handleUser);

            lbl_HandleUser.Text = "当前用户：" + handleUser;

            this.btn_Checking.Enabled = false;

            this.btn_GetCheckCode.Enabled = false;

            this.btn_ReferenceCheckCode.Enabled = false;
        }

        private void btn_GetCheckCode_Click(object sender, EventArgs e)
        {
            if(isGetCheckCode) return;

            isGetCheckCode = true;

            try
            {
                this.socket.Send(new Package { Id = 0x02, Length = 3 }.Serialize());
            }
            catch (Exception ex)
            {
                LogFactory.Warn(ex.Message);

                this.RunInMainthread(() =>
                {
                    this.lbl_Tip.Text = $"提示：{DateTime.Now:HH:mm:ss} 连接服务器失败！请稍后再试！";

                    this.btn_Connecting.Enabled = true;

                    this.btn_Checking.Enabled = false;

                    this.btn_GetCheckCode.Enabled = false;

                    this.btn_ReferenceCheckCode.Enabled = false;
                });
            }
        }

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

        private void GetCheckCode(ZhaopinCheckCode checkCodeModel)
        {
            if (checkCodeModel == null)
            {
                this.RunInMainthread(() =>
                {
                    this.lbl_Tip.Text = $"提示：{DateTime.Now:HH:mm:ss} 未获取到验证码！";
                });

                return;
            }

            cookieContainer = checkCodeModel.Cookie.Serialize(".zhaopin.com");

            checkCodeId = checkCodeModel.Id;

            account = checkCodeModel.Account;

            codeType = checkCodeModel.Type;

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

                return new DataResult("未获取到验证码");
            }

            var codeStream = streamResult.Data;

            timestamp = (DateTime.Now.ToUniversalTime().Ticks - 621355968000000000) / 10000000 + new Random().Next(0, 1000).ToString("000");

            // 将 ImageAttributes 应用于绘制

            var picBody = Image.FromHbitmap(ImageHelper.GetValidCode_Zhaopin(Image.FromStream(codeStream)).GetHbitmap());

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

            var accountBytes = Encoding.UTF8.GetBytes(account);

            try
            {
                this.socket.Send(new CheckedResultPackage { Id = 0x03, Length = (short)(13 + handleUserBytes.Length + accountBytes.Length), Status = 2, CheckCodeId = checkCodeId, Account = accountBytes, HandleUser = handleUserBytes }.Serialize());
            }
            catch (Exception ex)
            {
                LogFactory.Warn(ex.Message);

                this.RunInMainthread(() =>
                {
                    this.lbl_Tip.Text = $"提示：{DateTime.Now:HH:mm:ss} 连接服务器失败！请稍后再试！";

                    this.btn_Connecting.Enabled = true;

                    this.btn_Checking.Enabled = false;

                    this.btn_GetCheckCode.Enabled = false;

                    this.btn_ReferenceCheckCode.Enabled = false;
                });
            }

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

        private void ReferenceServerStatus(int waitCount)
        {
            if (waitCount < 0)
            {
                this.RunInMainthread(() =>
                {
                    this.lbl_Tip.Text = $"提示：{DateTime.Now:HH:mm:ss} 连接服务器失败！请重新连接服务器！";

                    this.btn_Checking.Enabled = false;

                    this.btn_GetCheckCode.Enabled = false;

                    this.btn_ReferenceCheckCode.Enabled = false;

                    this.btn_Connecting.Enabled = true;

                    this.pic_Body.Image = null;

                    this.pic_Header.Image = null;

                    this.lbl_WaitCount.Text = "待处理：";
                });

                return;
            }

            this.RunInMainthread(() =>
            {
                if (this.lbl_WaitCount.Text == "待处理：0" && waitCount > 0)
                {
                    this.WindowState = FormWindowState.Normal;

                    this.TopMost = true;

                    this.Activate();
                }

                this.lbl_WaitCount.Text = "待处理：" + waitCount;
            });
            
        }

        private void ReferenceRank()
        {
            var data = business.GetTodayRank();

            var arr = data.ToArray();

            arr = arr.OrderByDescending(o => o.Value).ToArray();

            this.RunInMainthread(() =>
            {
                this.lbx_TodayRank.Items.Clear();

                this.lbx_TodayRank.Items.Add("排名\t姓名\t数量");

                for (var i = 0; i < arr.Length; i++)
                {
                    this.lbx_TodayRank.Items.Add($"{i + 1}\t{arr[i].Key}\t{arr[i].Value}");
                }
            });

            data = business.GetTotalRank();

            this.RunInMainthread(() =>
            {
                this.lbx_TotalRank.Items.Clear();

                arr = data.ToArray();

                arr = arr.OrderByDescending(o => o.Value).ToArray();

                this.lbx_TotalRank.Items.Add("排名\t姓名\t数量");

                for (var i = 0; i < arr.Length; i++)
                {
                    this.lbx_TotalRank.Items.Add($"{i + 1}\t{arr[i].Key}\t{arr[i].Value}");
                }
            });
        }

        private void ListenerServer()
        {
            var bytes = new byte[1024];
            
            while (true)
            {
                if (socket.Available == 0)
                {
                    Thread.Sleep(100);

                    continue;
                }

                socket.Receive(bytes, 0, 3, SocketFlags.None);

                var actionId = bytes[0];

                var length = (short)(bytes[1] << 8 | bytes[2]);

                using (var stream = new MemoryStream())
                {
                    stream.Write(bytes, 0, 3);

                    for (var i = 3; i < length; i += bytes.Length)
                    {
                        var readLength = length - i > bytes.Length ? bytes.Length : length - i;

                        var len = 0;

                        while (len != readLength)
                        {
                            len += socket.Receive(bytes, len, readLength - len, SocketFlags.None);
                        }

                        stream.Write(bytes, 0, readLength);
                    }

                    var streamBytes = stream.ToArray();

                    if (actionId == 0x01)
                    {
                        var waitCount = new WaitMessagePackage().DeSerialize(streamBytes).WaitCount;

                        ReferenceServerStatus(waitCount);

                        ReferenceRank();

                        continue;
                    }

                    if (actionId == 0x02)
                    {
                        var model = new CheckCodePackage().DeSerialize(streamBytes);

                        GetCheckCode(new ZhaopinCheckCode
                        {
                            Id = model.CheckCodeId,
                            Account = Encoding.UTF8.GetString(model.Account),
                            Status = model.Status,
                            Cookie = Encoding.UTF8.GetString(model.Cookie),
                            Type = model.Type
                        });

                        continue;
                    }

                    if (actionId == 0x06)
                    {
                        this.RunInMainthread(() =>
                        {
                            this.lbl_Tip.Text = $"提示：{DateTime.Now:HH:mm:ss} 没有验证码待输入！";

                            this.lbl_WaitCount.Text = "待处理：0";

                            isGetCheckCode = false;
                        });

                        continue;
                    }

                    if (actionId == 0x0c)
                    {
                        var model = new LoginCheckResultPackage().DeSerialize(streamBytes);

                        isChecked = true;

                        if (model.Status == 1)
                        {
                            this.RunInMainthread(() =>
                            {
                                this.lbl_Tip.Text = $"提示：{DateTime.Now:HH:mm:ss} 验证成功！";

                                endTime = null;

                                this.btn_GetCheckCode.Enabled = true;

                                this.btn_ReferenceCheckCode.Enabled = false;

                                this.btn_Checking.Enabled = false;

                                this.pic_Body.Image = null;

                                this.pic_Header.Image = null;
                            });
                        }
                        else
                        {
                            endTime = DateTime.Now.AddMinutes(1);

                            var dataResult = ReferenceCheckCode();

                            if (!dataResult.IsSuccess) endTime = null;
                        }
                    }
                }
            }
        }
    }

    [Serializable]
    public class Watermark
    {
        public int Index { get; set; }

        public Image Image { get; set; }

        public int LocationX { get; set; }

        public int LocationY { get; set; }
    }
}
