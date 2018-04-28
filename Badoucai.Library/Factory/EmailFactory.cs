using System;
using System.Collections.Generic;
using System.Linq;
using OpenPop.Mime;
using OpenPop.Pop3;
using System.Net.Mail;
using System.Net.Mime;

namespace Badoucai.Library
{
    /// <summary>
    /// 
    /// </summary>
    public class EmailFactory
    {
        /// <summary>
        /// 邮件信息
        /// </summary>
        private readonly MailMessage message;

        /// <summary>
        /// 邮件发送者
        /// </summary>
        private static string Sender => "longzhijie@badoucai.net";

        /// <summary>
        /// 收件服务器
        /// </summary>
        private static string PopService => "pop3.mxhichina.com";

        /// <summary>
        /// 发件服务器
        /// </summary>
        private static string SmtpService => "smtp.badoucai.net";

        /// <summary>
        /// 邮件发送者密码
        /// </summary>
        private static string SenderPwd => "uLaF9CEk";

        /// <summary>
        /// 邮件标题
        /// </summary>
        public string Subject { get; set; }

        /// <summary>
        /// 邮件正文
        /// </summary>
        public string Body { get; set; }

        /// <summary>
        /// 是否HTML邮件
        /// </summary>
        public bool IsBodyHtml = true;

        /// <summary>
        /// 构造初始化函数
        /// </summary>
        /// <param name="ToMail">收件人</param>
        /// <param name="ToCC">抄送</param>
        /// <param name="ToBcc">密送</param>
        public EmailFactory(string ToMail, string ToCC = "", string ToBcc = "")
        {
            message = new MailMessage();
            var to = ToMail.Split(',');
            to.ToList().ForEach(t =>
            {
                message.To.Add(new MailAddress(t));
            });

            if (!string.IsNullOrWhiteSpace(ToCC))
            {
                var cc = ToCC.Split(',');
                cc.ToList().ForEach(c =>
                {
                    message.CC.Add(new MailAddress(c));
                });
            }
            if (!string.IsNullOrWhiteSpace(ToBcc))
            {
                var bcc = ToBcc.Split(',');
                bcc.ToList().ForEach(c =>
                {
                    message.Bcc.Add(new MailAddress(c));
                });
            }
        }

        /// <summary>
        /// 发送邮件
        /// </summary>
        public void Send()
        {
            //发信人

            message.From = new MailAddress(Sender);

            //标题

            message.Subject = Subject;

            message.IsBodyHtml = IsBodyHtml;

            message.BodyEncoding = System.Text.Encoding.UTF8;

            message.Body = Body;

            message.Priority = MailPriority.Normal;

            var client = new SmtpClient(SmtpService, 25) { Credentials = new System.Net.NetworkCredential(Sender, SenderPwd)};

            client.Send(message);
        }

        /// <summary>
        /// Get the unread messages from service which from specify address.
        /// </summary>
        /// <param name="hostname"></param>
        /// <param name="port"></param>
        /// <param name="useSsl"></param>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <param name="address"></param>
        /// <param name="seenUids"></param>
        /// <returns></returns>
        public static Dictionary<string, Message> FetchUnseenMessages(string hostname, int port, bool useSsl, string username, string password, string address, List<string> seenUids)
        {
            var messages = new Dictionary<string, Message>();

            try
            {
                using (var client = new Pop3Client())
                {
                    client.Connect(hostname, port, useSsl);

                    client.Authenticate(username, password);

                    var count = client.GetMessageCount();

                    for (var i = 1; i <= count; i++)
                    {
                        var header = client.GetMessageHeaders(i);

                        var uid = client.GetMessageUid(i);

                        if (seenUids.Contains(uid))
                        {
                            continue;
                        }

                        if (header.From.Address == address)
                        {
                            messages.Add(uid, client.GetMessage(i));
                        }
                    }
                }

                return messages;
            }
            catch (Exception)
            {
                return messages;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="hostname"></param>
        /// <param name="port"></param>
        /// <param name="useSsl"></param>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public static List<Message> FetchAllMessages(string hostname, int port, bool useSsl, string username, string password)
        {
            // The client disconnects from the server when being disposed

            using (var client = new Pop3Client())
            {
                // Connect to the server

                client.Connect(hostname, port, useSsl);

                // Authenticate ourselves towards the server

                client.Authenticate(username, password);

                //client.Authenticate(username, password, AuthenticationMethod.UsernameAndPassword);

                // Get the number of messages in the inbox

                var messageCount = client.GetMessageCount();

                // We want to download all messages

                var allMessages = new List<Message>(messageCount);

                // Messages are numbered in the interval: [1, messageCount]

                // Ergo: message numbers are 1-based.

                // Most servers give the latest message the highest number

                for (var i = messageCount; i > 0; i--)
                {
                    allMessages.Add(client.GetMessage(i));
                }

                // Now return the fetched messages

                return allMessages;
            }
        }

        /// <summary>
        /// Example showing:
        ///  - how to use UID's (unique ID's) of messages from the POP3 server
        ///  - how to download messages not seen before
        ///    (notice that the POP3 protocol cannot see if a message has been read on the server
        ///     before. Therefore the client need to maintain this state for itself)
        /// </summary>
        /// <param name="hostname">Hostname of the server. For example: pop3.live.com</param>
        /// <param name="port">Host port to connect to. Normally: 110 for plain POP3, 995 for SSL POP3</param>
        /// <param name="useSsl">Whether or not to use SSL to connect to server</param>
        /// <param name="username">Username of the user on the server</param>
        /// <param name="password">Password of the user on the server</param>
        /// <param name="seenUids">
        /// List of UID's of all messages seen before.
        /// New message UID's will be added to the list.
        /// Consider using a HashSet if you are using >= 3.5 .NET
        /// </param>
        /// <returns>A List of new Messages on the server</returns>
        public static List<EmailMessage> FetchUnseenMessages(string hostname, int port, bool useSsl, string username, string password, List<string> seenUids)
        {
            // The client disconnects from the server when being disposed

            using (var client = new Pop3Client())
            {
                // Connect to the server

                client.Connect(hostname, port, useSsl);

                // Authenticate ourselves towards the server

                client.Authenticate(username, password);

                // Fetch all the current uids seen

                var uids = client.GetMessageUids();

                // Create a list we can return with all new messages

                var newMessages = new List<EmailMessage>();

                // All the new messages not seen by the POP3 client

                for (var i = 0; i < uids.Count; i++)
                {
                    var currentUidOnServer = uids[i];

                    if (!seenUids.Contains(currentUidOnServer))
                    {
                        // We have not seen this message before.

                        // Download it and add this new uid to seen uids

                        // the uids list is in messageNumber order - meaning that the first

                        // uid in the list has messageNumber of 1, and the second has 

                        // messageNumber 2. Therefore we can fetch the message using

                        // i + 1 since messageNumber should be in range [1, messageCount]

                        var unseenMessage = client.GetMessage(i + 1);

                        // Add the message to the new messages

                        newMessages.Add(new EmailMessage { message = unseenMessage, messageId = currentUidOnServer });

                        // Add the uid to the seen uids, as it has now been seen

                        seenUids.Add(currentUidOnServer);
                    }
                }

                // Return our new found messages

                return newMessages;
            }
        }

        /// <summary>
        /// 删除消息
        /// </summary>
        /// <param name="hostname"></param>
        /// <param name="port"></param>
        /// <param name="useSsl"></param>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <param name="messageIdList"></param>
        /// <returns></returns>
        public static bool DeleteMessageByMessageId(string hostname, int port, bool useSsl, string username, string password, List<string> messageIdList)
        {
            using (var client = new Pop3Client())
            {
                // Connect to the server

                client.Connect(hostname, port, useSsl);

                // Authenticate ourselves towards the server

                client.Authenticate(username, password);

                // Get the number of messages on the POP3 server
                var messageCount = client.GetMessageCount();

                // Run trough each of these messages and download the headers
                for (var messageItem = messageCount; messageItem > 0; messageItem--)
                {
                    // If the Message ID of the current message is the same as the parameter given, delete that message
                    if (messageIdList.Any(a=>a==client.GetMessageHeaders(messageItem).MessageId))
                    {
                        // Delete
                        client.DeleteMessage(messageItem);
                        return true;
                    }
                }
            }

            // We did not find any message with the given messageId, report this back
            return false;
        }

        #region 添加附件 
        /// <summary> 
        /// 添加附件（自动识别文件类型） 
        /// </summary> 
        /// <param name="fileName">单个文件的路径</param> 
        public void Attachments(string fileName)
        {
            message.Attachments.Add(new Attachment(fileName));
        }

        /// <summary> 
        /// 添加附件（默认为富文本RTF格式） 
        /// </summary> 
        /// <param name="fileName">单个文件的路径</param> 
        public void AttachmentsForRTF(string fileName)
        {
            message.Attachments.Add(new Attachment(fileName, MediaTypeNames.Application.Rtf));
        }

        #endregion
    }

    public class EmailMessage
    {
        public string messageId { get; set; }
        public Message message { get; set; }
    }
}