using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace Badoucai.Service
{
    internal class Program
    {
        private static void Main()
        {
            Trace.Listeners.Add(new ConsoleTraceListener());

            var directory = $@"{AppDomain.CurrentDomain.BaseDirectory}\Log";

            if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);

            Trace.Listeners.Add(new TextWriterTraceListener($@"{directory}\{DateTime.Now:yyyy-MM-dd}.log")
            {
                TraceOutputOptions = TraceOptions.DateTime
            });

            //new FlagOssResumeThread().Create().Start();// 清洗 MangningOss 简历库,并标记简历.

            //new HandleBDCOssResumeThread().Create().Start();// 处理八斗才及插件下载的简历,同步到XSS库.

            //new UploadZhaopinResumeThread().Create().Start();// 上传简历至业务库.

            //new SearchResumeByConditionThread().Create().Start();// 搜索简历更新简历刷新时间.

            //new DownloadLocalCompanyResumeThread().Create().Start();// 下载并处理本地公司简历

            //new DownloadAllCompanyReusmeThread().Create().Start();// 下载并处理XSS公司简历

            //new SpiderTycCompanyThread().Create().Start(); // 通过天眼查获取公司信息

            //new DodiZhaopinThread().Create().Start(); // 提取多迪简历信息匹配库中没有联系方式的简历（受多迪服务器影响）

            new ProgramTasksThread().Create().Start(); // 其他任务工作线程

            while (true)
            {
                Thread.Sleep(100);
            }
        }
    }
}
