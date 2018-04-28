using System.IO;

namespace Badoucai.Business.Socket
{
    public class HeartbeatPackage : Package
    {
        public sealed override byte Id { get; set; }

        public sealed override short Length { get; set; }

        public HeartbeatPackage()
        {
            Id = 0x00;

            Length = 3;
        }

        public override byte[] Serialize()
        {
            using (var stream = new MemoryStream())
            {
                stream.WriteByte(Id);

                stream.WriteByte((byte)(Length >> 8));
                stream.WriteByte((byte)Length);

                return stream.ToArray();
            }
        }
    }
}