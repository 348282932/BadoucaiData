using System.IO;

namespace Badoucai.Business.Socket
{
    public class WaitMessagePackage : Package
    {
        public override byte Id { get; set; }

        public override short Length { get; set; }

        public int WaitCount { get; set; }

        public override byte[] Serialize()
        {
            using (var stream = new MemoryStream())
            {
                stream.Write(Id);

                stream.WriteByte((byte)(Length >> 8));
                stream.WriteByte((byte)Length);

                stream.WriteByte((byte)(WaitCount >> 24));
                stream.WriteByte((byte)(WaitCount >> 16));
                stream.WriteByte((byte)(WaitCount >> 8));
                stream.WriteByte((byte)WaitCount);

                return stream.ToArray();
            }
        }

        public WaitMessagePackage DeSerialize(byte[] bytes)
        {
            return new WaitMessagePackage
            {
                Id = bytes[0],
                Length = (short)(bytes[1] << 8 | bytes[2]),
                WaitCount = bytes[3] << 24 | bytes[4] << 16 | bytes[5] << 8 | bytes[6]
            };
        }
    }
}