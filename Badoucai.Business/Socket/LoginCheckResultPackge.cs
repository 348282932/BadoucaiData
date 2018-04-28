using System.IO;

namespace Badoucai.Business.Socket
{
    public class LoginCheckResultPackage : Package
    {
        public override byte Id { get; set; }

        public override short Length { get; set; }

        public short CleaningId { get; set; }

        // 1.成功 2.失败
        public short Status { get; set; }

        public int CheckCodeId { get; set; }

        public override byte[] Serialize()
        {
            using (var stream = new MemoryStream())
            {
                stream.WriteByte(Id);

                stream.WriteByte((byte)(Length >> 8));
                stream.WriteByte((byte)Length);

                stream.WriteByte((byte)(CleaningId >> 8));
                stream.WriteByte((byte)CleaningId);

                stream.WriteByte((byte)(Status >> 8));
                stream.WriteByte((byte)Status);

                stream.WriteByte((byte)(CheckCodeId >> 24));
                stream.WriteByte((byte)(CheckCodeId >> 16));
                stream.WriteByte((byte)(CheckCodeId >> 8));
                stream.WriteByte((byte)CheckCodeId);

                return stream.ToArray();
            }
        }

        public LoginCheckResultPackage DeSerialize(byte[] bytes)
        {
            return new LoginCheckResultPackage
            {
                Id = bytes[0],
                Length = (short)(bytes[1] << 8 | bytes[2]),
                CleaningId = (short)(bytes[3] << 8 | bytes[4]),
                Status = (short)(bytes[5] << 8 | bytes[6]),
                CheckCodeId = bytes[7] << 24 | bytes[8] << 16 | bytes[9] << 8 | bytes[10]
            };
        }
    }
}