using System;
using System.IO;

namespace Badoucai.Business.Socket
{
    public class CheckCoordinatPackage : Package
    {
        public int CheckCodeId { get; set; }

        public short AccountLength { get; set; }

        public short CoordinatLength { get; set; }

        public byte[] Account { get; set; }

        public byte[] CoordinatValue { get; set; }

        public override byte[] Serialize()
        {
            if (Length - CoordinatValue.Length - Account.Length != 11) throw new Exception("接受包异常！");

            using (var stream = new MemoryStream())
            {
                stream.WriteByte(Id);

                stream.WriteByte((byte)(Length >> 8));
                stream.WriteByte((byte)Length);

                stream.WriteByte((byte)(CheckCodeId >> 24));
                stream.WriteByte((byte)(CheckCodeId >> 16));
                stream.WriteByte((byte)(CheckCodeId >> 8));
                stream.WriteByte((byte)CheckCodeId);

                stream.WriteByte((byte)(Account.Length >> 8));
                stream.WriteByte((byte)Account.Length);

                stream.WriteByte((byte)(CoordinatValue.Length >> 8));
                stream.WriteByte((byte)CoordinatValue.Length);

                stream.Write(Account, 0, Account.Length);
                stream.Write(CoordinatValue, 0, CoordinatValue.Length);

                return stream.ToArray();
            }
        }

        public CheckCoordinatPackage DeSerialize(byte[] bytes)
        {
            var accountLenth = (short)(bytes[7] << 8 | bytes[8]);

            var coordinatLength = (short)(bytes[9] << 8 | bytes[10]);

            var accountValueBytes = bytes.Copy(11, accountLenth);

            var coordinatValueBytes = bytes.Copy(11 + accountLenth, coordinatLength);

            return new CheckCoordinatPackage
            {
                Id = bytes[0],
                Length = (short)(bytes[1] << 8 | bytes[2]),
                CheckCodeId = bytes[3] << 24 | bytes[4] << 16 | bytes[5] << 8 | bytes[6],
                AccountLength = accountLenth,
                CoordinatLength = coordinatLength,
                CoordinatValue = coordinatValueBytes,
                Account = accountValueBytes
            };
        }
    }
}