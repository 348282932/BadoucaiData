using System;
using System.IO;

namespace Badoucai.Business.Socket
{
    public class CheckedResultPackage : Package
    {
        // 1.成功 2.失败
        public short Status { get; set; }

        public int CheckCodeId { get; set; }

        public short AccountLength { get; set; }

        public short HandleUserLength { get; set; }

        public byte[] Account { get; set; }

        public byte[] HandleUser { get; set; }

        public override byte[] Serialize()
        {
            using (var stream = new MemoryStream())
            {
                stream.WriteByte(Id);

                stream.WriteByte((byte)(Length >> 8));
                stream.WriteByte((byte)Length);

                stream.WriteByte((byte)(Status >> 8));
                stream.WriteByte((byte)Status);

                stream.WriteByte((byte)(CheckCodeId >> 24));
                stream.WriteByte((byte)(CheckCodeId >> 16));
                stream.WriteByte((byte)(CheckCodeId >> 8));
                stream.WriteByte((byte)CheckCodeId);

                stream.WriteByte((byte)(Account.Length >> 8));
                stream.WriteByte((byte)Account.Length);

                stream.WriteByte((byte)(HandleUser.Length >> 8));
                stream.WriteByte((byte)HandleUser.Length);

                stream.Write(Account);

                stream.Write(HandleUser);

                return stream.ToArray();
            }
        }

        public CheckedResultPackage DeSerialize(byte[] bytes)
        {
            var accountLength = (short)(bytes[9] << 8 | bytes[10]);

            var handleUserLength = (short)(bytes[11] << 8 | bytes[12]);

            var accountValueBytes = bytes.Copy(13, accountLength);

            var handleUserBytes = bytes.Copy(13 + accountLength, handleUserLength);

            return new CheckedResultPackage
            {
                Id = bytes[0],
                Length = (short)(bytes[1] << 8 | bytes[2]),
                Status = (short)(bytes[3] << 8 | bytes[4]),
                CheckCodeId = bytes[5] << 24 | bytes[6] << 16 | bytes[7] << 8 | bytes[8],
                AccountLength = accountLength,
                HandleUserLength = handleUserLength,
                Account = accountValueBytes,
                HandleUser = handleUserBytes
            };
        }
    }
}