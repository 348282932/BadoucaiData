using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Reflection;
using System.Text;
using HtmlAgilityPack;

namespace Badoucai.Library
{
    public class CompressionFactory
    {
        /// <summary>
        /// 获取mht文件源文档
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static IEnumerable<HtmlDocument> GetMhtSources(string path)
        {
            foreach (var fileName in Directory.EnumerateFiles(path, "*.zip"))
            {
                using (var archive = ZipFile.OpenRead(fileName))
                {
                    foreach (var entry in archive.Entries)
                    {
                        if (entry.FullName.StartsWith("index")) continue;

                        if (entry.FullName.EndsWith(".mht", StringComparison.OrdinalIgnoreCase))
                        {
                            using (var memoryStream = new MemoryStream())
                            {
                                var bytes = new byte[1024];

                                var stream = entry.Open();

                                while (stream.Read(bytes, 0, bytes.Length) > 0)
                                {
                                    memoryStream.Write(bytes, 0, bytes.Length);
                                }

                                yield return MultipleHyperTextSerializer(memoryStream.ToArray());
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 获取mht文件源文档
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static IEnumerable<HtmlDocument> GetHtmlSources(string path)
        {
            foreach (var fileName in Directory.EnumerateFiles(path, "*.zip"))
            {
                using (var archive = ZipFile.OpenRead(fileName))
                {
                    foreach (var entry in archive.Entries)
                    {
                        if (entry.FullName.StartsWith("index")) continue;

                        if (entry.FullName.EndsWith(".html", StringComparison.OrdinalIgnoreCase))
                        {
                            var htmlDocument = new HtmlDocument();

                            htmlDocument.Load(entry.Open());

                            yield return htmlDocument;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        private static HtmlDocument MultipleHyperTextSerializer(byte[] bytes)
        {
            using (var stream = new MemoryStream())
            {
                for (var i = 1; i < bytes.Length; ++i)
                {
                    if (bytes[i] == 0x0a && bytes[i - 1] != 0x0d)
                    {
                        stream.Write(0x0d);
                    }

                    stream.Write(bytes[i]);
                }

                bytes = stream.ToArray();
            }

            var document = new HtmlDocument();

            try
            {
                using (var stream = new MemoryStream(GetObject(bytes, "")))
                {
                    document.Load(stream, Encoding.UTF8);
                }
            }
            catch (FormatException)
            {
            }

            return document;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        private static byte[] GetObject(byte[] bytes, string id)
        {
            using (var stream = new MemoryStream(bytes))
            {
                var contents = new StreamContent(stream)
                {
                    Headers =
                    {
                        ContentType = MediaTypeHeaderValue.Parse(GetBoundary(bytes).Insert(0, "multipart/related; "))
                    }
                }.ReadAsMultipartAsync(new MultipartRelatedStreamProvider()).Result.Contents;

                foreach (var type in contents.Select(t => t.Headers.ContentType).Where(t => t.CharSet != null))
                {
                    type.CharSet = type.CharSet.Replace("\"", "");
                }

                HttpContent content;

                if (string.IsNullOrEmpty(id))
                {
                    content = contents.FirstOrDefault(t => !t.Headers.Contains("Content-ID"));
                }
                else
                {
                    content = contents.FirstOrDefault(t =>
                    {
                        IEnumerable<string> values;

                        if (t.Headers.TryGetValues("Content-ID", out values))
                        {
                            return values.Contains(id);
                        }

                        return false;
                    });
                }

                if (content == null)
                {
                    return null;
                }

                bytes = content.ReadAsByteArrayAsync().Result;

                switch (content.Headers.GetValues("Content-Transfer-Encoding").FirstOrDefault())
                {
                    case "base64":
                    {
                        bytes = Convert.FromBase64String(Encoding.ASCII.GetString(bytes));

                        break;
                    }
                    case "quoted-printable":
                    {
                        var instance = typeof(TransferEncoding).Assembly.CreateInstance(
                            "System.Net.Mime.QuotedPrintableStream",
                            false,
                            BindingFlags.Instance | BindingFlags.NonPublic,
                            null,
                            new object[] { stream, true },
                            null,
                            null);

                        if (instance == null)
                        {
                            throw new FormatException("Quoted printable instance was not created.");
                        }

                        var methodInfo = instance.GetType().GetMethod("DecodeBytes");

                        if (methodInfo != null) bytes.Resize((int)methodInfo.Invoke(instance, new object[] { bytes, 0, bytes.Length }));

                        break;
                    }
                    default:
                    {
                        throw new FormatException("Transfer encoding was unexpected.");
                    }
                }

                var charset = content.Headers.ContentType.CharSet;

                if (string.IsNullOrEmpty(charset))
                {
                    return bytes;
                }

                var encoding = Encoding.GetEncoding(charset);

                if (encoding.CodePage == Encoding.UTF8.CodePage)
                {
                    return bytes;
                }

                return Encoding.Convert(encoding, Encoding.UTF8, bytes);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        private static string GetBoundary(byte[] bytes)
        {
            var index = bytes.IndexOf(Encoding.ASCII.GetBytes("boundary="));

            if (index < 0)
            {
                throw new FormatException("Boundary was not found.");
            }

            var boundary = new StringBuilder();

            do
            {
                boundary.Append((char)bytes[index++]);
            }
            while (bytes[index] != 0x0a && bytes[index] != 0x0d);

            return boundary.ToString();
        }
    }
}