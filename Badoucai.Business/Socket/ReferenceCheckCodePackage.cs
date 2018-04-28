using System;
using System.IO;

namespace Badoucai.Business.Socket
{
    public class ReferenceCheckCodePackage : Package
    {
        public short AccountLength { get; set; }
        public byte[] Account { get; set; }

        public override byte[] Serialize()
        {
            using (var stream = new MemoryStream())
            {
                stream.Write(Id);

                stream.WriteByte((byte)(Length >> 8));
                stream.WriteByte((byte)Length);

                stream.WriteByte((byte)(Account.Length >> 8));
                stream.WriteByte((byte)Account.Length);

                stream.Write(Account, 0, Account.Length);

                return stream.ToArray();
            }
        }

        public ReferenceCheckCodePackage DeSerialize(byte[] bytes)
        {
            var accountLength = (short)(bytes[3] << 8 | bytes[4]);

            var accountValueBytes = bytes.Copy(5, accountLength);

            return new ReferenceCheckCodePackage
            {
                Id = bytes[0],
                Length = (short)(bytes[1] << 8 | bytes[2]),
                AccountLength = accountLength,
                Account = accountValueBytes
            };
        }
    }
}