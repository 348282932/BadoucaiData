using System;
using System.Collections.Concurrent;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Badoucai.Business.Socket;
using Badoucai.Business.Zhaopin;
using Badoucai.EntityFramework.MySql;
using Badoucai.Library;
using Fiddler;
using Newtonsoft.Json;

namespace Badoucai.WindowsForm.Zhaopin
{
    public partial class CheckCodeSchedulingForm : Form
    {
        private static readonly CheckCodeBusiness business = new CheckCodeBusiness();

        private static readonly ConcurrentDictionary<string,Socket> cleaningDictionary = new ConcurrentDictionary<string, Socket>();

        private static readonly ConcurrentDictionary<int,Socket> clientDictionary = new ConcurrentDictionary<int, Socket>();

        private static readonly string webProxyIp = ConfigurationManager.AppSettings["WebProxyIp"];

        private static readonly int webProxyPort = Convert.ToInt32(ConfigurationManager.AppSettings["WebProxyPort"]);

        public CheckCodeSchedulingForm()
        {
            InitializeComponent();

            Task.Run(()=>business.StartTimer());
        }

        private void btn_Start_Click(object sender, EventArgs e)
        {
            // 重置状态为处理中的验证码

            business.ResetStatus();

            // 开启监听

            this.RunAsync(StartSocketServer);

            FiddlerApplication.BeforeRequest += oSessions =>
            {
                //oSessions["X-OverrideGateway"] = "210.83.225.31:15839";
                //oSessions["X-OverrideGateway"] = "127.0.0.1:8888";
                //39.108.161.230:8080

                if (oSessions.url.Contains("zhaopin")) oSessions["X-OverrideGateway"] = $"{webProxyIp}:{webProxyPort}";

                oSessions.bBufferResponse = true;

                if (oSessions.url.Contains("captcha.js"))
                {
                    if (oSessions.RequestHeaders.Exists("If-Modified-Since")) oSessions.RequestHeaders.Remove("If-Modified-Since");

                    if (oSessions.RequestHeaders.Exists("If-None-Match")) oSessions.RequestHeaders.Remove("If-None-Match");

                    if (oSessions.RequestHeaders.Exists("Accept-Encoding")) oSessions.RequestHeaders.Remove("Accept-Encoding");
                }

                if (oSessions.RequestHeaders.Exists("User-Agent")) oSessions.RequestHeaders["User-Agent"] = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/63.0.3239.84 Safari/537.36";
            };

            FiddlerApplication.BeforeResponse += oSessions =>
            {
                if (oSessions.url.Contains("captcha.js"))
                {
                    oSessions.ResponseHeaders["Cache-Control"] = "no-store";

                    var jsContent = oSessions.GetResponseBodyEncoding().GetString(oSessions.ResponseBody);

                    jsContent = "var hackStr = '';" + jsContent;

                    jsContent = jsContent.Replace("pData.join(\";\")", "hackStr");

                    jsContent = jsContent.Remove(jsContent.Length - 4);

                    jsContent += "this.hack = function (coordinate){ $(\"#captcha-submitCode\").removeClass(\"btn-disabled\");validate = true;hackStr = coordinate; $(\"#captcha-submitCode\").trigger(\"click\"); return true;}";

                    jsContent += "}\r\n";

                    jsContent += "function execHack(coordinate){this.captcha.hack(coordinate); return 1;}";

                    oSessions.ResponseBody = oSessions.GetResponseBodyEncoding().GetBytes(jsContent);
                }
            };

            if (!FiddlerApplication.IsStarted())
            {
                FiddlerApplication.Startup(8887, FiddlerCoreStartupFlags.Default);
            }
        }

        private void StartSocketServer()
        {
            var tcpListener = new TcpListener(IPAddress.Any, 12580);

            tcpListener.Start();

            this.RunInMainthread(() =>
            {
                this.btn_Start.Text = "已启动";

                this.btn_Start.Enabled = false;

                this.tir_Refrence.Start();
            });

            while (true)
            {
                if (!tcpListener.Pending())
                {
                    Thread.Sleep(100);

                    continue;
                }

                Task.Run(() =>
                {
                    var account = string.Empty;

                    using (var socket = tcpListener.AcceptSocket())
                    {
                        var lastTime = DateTime.Now.AddMinutes(1);

                        var bytes = new byte[1024];

                        while (true)
                        {
                            if (lastTime < DateTime.Now)
                            {
                                if(string.IsNullOrEmpty(cleaningDictionary.FirstOrDefault(f => f.Value == socket).Key)) break;

                                LogFactory.Debug($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} 关闭连接 程序ID=> {cleaningDictionary.FirstOrDefault(f => f.Value == socket).Key} account:{account} lastTime =>{lastTime:yyyy-MM-dd HH:mm:ss}");

                                Socket tempSockte;

                                if (cleaningDictionary.TryRemove(account, out tempSockte))
                                {
                                    business.CleaningEnd(account);
                                }

                                break;
                            }

                            if (socket.Available == 0)
                            {
                                Thread.Sleep(100);

                                continue;
                            }

                            lastTime = DateTime.Now.AddMinutes(1);

                            socket.Receive(bytes, 0, 3, SocketFlags.None);

                            var actionId = bytes[0];

                            if (cleaningDictionary.Any(f => f.Value == socket))
                            {
                                LogFactory.Debug($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} 收到心跳 程序ID=> {cleaningDictionary.FirstOrDefault(f => f.Value == socket).Key} lastTime =>{lastTime:yyyy-MM-dd HH:mm:ss}, Dic=>{JsonConvert.SerializeObject(cleaningDictionary.Select(s=>s.Key))}");
                            }

                            

                            var length = (short)(bytes[1] << 8 | bytes[2]);

                            using (var stream = new MemoryStream())
                            {
                                stream.Write(bytes, 0, 3);

                                for (var i = 3; i < length; i+=bytes.Length)
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
                                    var waitCount = business.GetWaitCount();

                                    socket.Send(new WaitMessagePackage
                                    {
                                        Id = 0x01,
                                        Length = 7,
                                        WaitCount = waitCount
                                    }.Serialize());

                                    continue;
                                }

                                if (actionId == 0x02)
                                {
                                    var checeCodeModel = business.GetCheckCode();

                                    if (checeCodeModel == null)
                                    {
                                        socket.Send(new Package { Id = 0x06, Length = 3 }.Serialize());

                                        continue;
                                    }

                                    var accountBytes = Encoding.UTF8.GetBytes(checeCodeModel.Account);

                                    var cookieBytes = Encoding.UTF8.GetBytes(checeCodeModel.Cookie);

                                    socket.Send(new CheckCodePackage
                                    {
                                        Id = 0x02,
                                        Length = (short)(cookieBytes.Length + accountBytes.Length  + 15),
                                        CheckCodeId = checeCodeModel.Id,
                                        Cookie = cookieBytes,
                                        Status = checeCodeModel.Status,
                                        Type = checeCodeModel.Type,
                                        Account = accountBytes
                                    }.Serialize());

                                    clientDictionary.AddOrUpdate(checeCodeModel.Id, k => socket, (k, v) => socket);

                                    continue;
                                }

                                if (actionId == 0x03)
                                {
                                    var package = new CheckedResultPackage().DeSerialize(streamBytes);

                                    var accounts = business.CheckResult(package.CheckCodeId, package.Status, Encoding.UTF8.GetString(package.Account), Encoding.UTF8.GetString(package.HandleUser));

                                    if (package.Status == 1)
                                    {
                                        foreach (var item in accounts)
                                        {
                                            Socket socketClient;

                                            if (!cleaningDictionary.TryGetValue(item, out socketClient)) continue;

                                            socketClient.Send(streamBytes);
                                        }
                                    }

                                    continue;
                                }


                                if (actionId == 0x04)
                                {
                                    var package = new CleaningProcedurePackage().DeSerialize(streamBytes);

                                    account = Encoding.UTF8.GetString(package.Account);

                                    business.CleaningStart(new ZhaopinCleaningProcedure
                                    {
                                        Id = package.CleaningId,
                                        Account = Encoding.UTF8.GetString(package.Account),
                                        Password = Encoding.UTF8.GetString(package.Password)
                                    });

                                    cleaningDictionary.AddOrUpdate(Encoding.UTF8.GetString(package.Account), k => socket, (k, v) => socket);

                                    continue;
                                }

                                if (actionId == 0x05)
                                {
                                    var package = new CheckCodePackage().DeSerialize(streamBytes);

                                    business.InserCheckCode(new ZhaopinCheckCode
                                    {
                                        Account = Encoding.UTF8.GetString(package.Account),
                                        Cookie = Encoding.UTF8.GetString(package.Cookie),
                                        CreateTime = DateTime.Now,
                                        Status = 0,
                                        Type = package.Type
                                    });

                                }
                                if (actionId == 0x09)
                                {
                                    var package = new CheckCoordinatPackage().DeSerialize(streamBytes);

                                    Socket socketClient;

                                    if (!cleaningDictionary.TryGetValue(Encoding.UTF8.GetString(package.Account), out socketClient)) continue;

                                    socketClient.Send(streamBytes);
                                }
                                if (actionId == 0x0a)
                                {
                                    var package = new ReferenceCheckCodePackage().DeSerialize(streamBytes);

                                    Socket socketClient;

                                    if (!cleaningDictionary.TryGetValue(Encoding.UTF8.GetString(package.Account), out socketClient)) continue;

                                    socketClient.Send(streamBytes);
                                }

                                //if (actionId == 0x0c)
                                //{
                                //    var package = new LoginCheckResultPackage().DeSerialize(streamBytes);

                                //    business.LoginCheckResult(package.CheckCodeId, package.Status, package.CleaningId);

                                //    Socket socketClient;

                                //    if (!clientDictionary.TryGetValue(package.CheckCodeId, out socketClient)) continue;

                                //    socketClient.Send(streamBytes);

                                //    if (package.Status == 1)
                                //    {
                                //        clientDictionary.TryRemove(package.CheckCodeId, out socketClient);
                                //    }
                                //}
                            }
                        }
                    }
                });
            }
        }

        public byte[] Serialization<T>(T model)
        {
            var type = typeof(T);

            using (var stream = new MemoryStream())
            {
                foreach (var name in type.GetEnumNames())
                {
                    var fieldInfo = type.GetField(name);

                    var fieldTyte = fieldInfo.FieldType;

                    var value = fieldInfo.GetValue(model);

                    if (fieldInfo.FieldType.IsValueType)
                    {
                        var byteLength = 0;

                        if (fieldTyte == typeof(DateTime))
                        {
                            byteLength = 8;

                            value = ((DateTime)value).Ticks;
                        }

                        if(byteLength == 0) byteLength = Marshal.SizeOf(fieldTyte);

                        for (var i = byteLength; i > 0; i--)
                        {
                            stream.WriteByte((byte)((long)value >> ((i - 1) * 8)));
                        }
                    }
                    else
                    {
                        var stringBytes = Encoding.UTF8.GetBytes(value.ToString());

                        stream.Write(stringBytes);
                    }
                }

                return stream.ToArray();
            }
        }

        private void tir_Refrence_Tick(object sender, EventArgs e)
        {
            this.lbl_WaitCount.Text = "待处理：" + business.GetWaitCount();
            this.lbl_ProcessingCount.Text = "处理中：" + business.GetProcessingCount();
            this.lbl_OnlineCount.Text = "清洗程序在线数：" + cleaningDictionary.Count;
        }

        private void btn_Stop_Click(object sender, EventArgs e)
        {
            this.btn_Stop.Enabled = false;

            if (this.btn_Stop.Text == "暂停清洗")
            {
                foreach (var item in cleaningDictionary)
                {
                    item.Value.Send(new Package { Id = 0x07, Length = 3 }.Serialize());
                }

                this.btn_Stop.Text = "开始清洗";
            }
            else
            {
                foreach (var item in cleaningDictionary)
                {
                    item.Value.Send(new Package { Id = 0x08, Length = 3 }.Serialize());
                }

                this.btn_Stop.Text = "暂停清洗";
            }

            this.btn_Stop.Enabled = true;
        }

        private void CheckCodeSchedulingForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            FiddlerApplication.Shutdown();
        }
    }
}
