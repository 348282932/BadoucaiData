using System.Configuration;
using System.Threading;
using Aliyun.OSS;

namespace Badoucai.Service
{
    public abstract class BaseThread
    {
        public static readonly OssClient mangningOssClient = new OssClient
        (
            ConfigurationManager.AppSettings["Oss.Mangning.Url"],
            ConfigurationManager.AppSettings["Oss.Mangning.KeyId"],
            ConfigurationManager.AppSettings["Oss.Mangning.KeySecret"]
        );

        public static readonly string mangningBucketName = ConfigurationManager.AppSettings["Oss.Mangning.Bucket"];

        public static readonly OssClient badoucaiOssClient = new OssClient
        (
            ConfigurationManager.AppSettings["Oss.Badoucai.Url"],
            ConfigurationManager.AppSettings["Oss.Badoucai.KeyId"],
            ConfigurationManager.AppSettings["Oss.Badoucai.KeySecret"]
        );

        public static readonly string badoucaiBucketName = ConfigurationManager.AppSettings["Oss.Badoucai.Bucket"];

        public abstract Thread Create();
    }
}