using System;
using System.IO;

namespace Badoucai.Business.Socket
{
    public class CheckCodePackage : Package
    {
        public override byte Id { get; set; }

        public override short Length { get; set; }

        public int CheckCodeId { get; set; }

        public short AccountLength { get; set; }

        public byte[] Account { get; set; }

        public short Status { get; set; }

        public short Type { get; set; }

        public short CookieLength { get; set; }

        public byte[] Cookie { get; set; }

        public override byte[] Serialize()
        {
            if(Length - Cookie.Length - Account.Length != 15) throw new Exception("接受包异常！");

            using (var stream = new MemoryStream())
            {
                stream.WriteByte(Id);

                stream.WriteByte((byte)(Length >> 8));
                stream.WriteByte((byte)Length);

                stream.WriteByte((byte)(CheckCodeId >> 24));
                stream.WriteByte((byte)(CheckCodeId >> 16));
                stream.WriteByte((byte)(CheckCodeId >> 8));
                stream.WriteByte((byte)CheckCodeId);

                stream.WriteByte((byte)(Status >> 8));
                stream.WriteByte((byte)Status);

                stream.WriteByte((byte)(Type >> 8));
                stream.WriteByte((byte)Type);

                stream.WriteByte((byte)(Account.Length >> 8));
                stream.WriteByte((byte)Account.Length);

                stream.WriteByte((byte)(Cookie.Length >> 8));
                stream.WriteByte((byte)Cookie.Length);

                stream.Write(Account, 0, Account.Length);

                stream.Write(Cookie, 0, Cookie.Length);

                return stream.ToArray();
            }
        }

        public CheckCodePackage DeSerialize(byte[] bytes)
        {
            var accountLenth = (short)(bytes[11] << 8 | bytes[12]);

            var cookieLength = (short)(bytes[13] << 8 | bytes[14]);

            var accountValueBytes = bytes.Copy(15, accountLenth);

            var cookieValueBytes = bytes.Copy(15 + accountLenth, cookieLength);

            return new CheckCodePackage
            {
                Id = bytes[0],
                Length = (short)(bytes[1] << 8 | bytes[2]),
                CheckCodeId = bytes[3] << 24 | bytes[4] << 16 | bytes[5] << 8 | bytes[6],
                Status = (short)(bytes[7] << 8 | bytes[8]),
                Type = (short)(bytes[9] << 8 | bytes[10]),
                AccountLength = accountLenth,
                CookieLength = cookieLength,
                Account = accountValueBytes,
                Cookie = cookieValueBytes
            };
        }
    }
}