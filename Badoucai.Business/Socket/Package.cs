using System.IO;

namespace Badoucai.Business.Socket
{
    public class Package
    {
        public virtual byte Id { get; set; }

        public virtual short Length { get; set; }

        public virtual byte[] Serialize()
        {
            using (var stream = new MemoryStream())
            {
                stream.WriteByte(Id);

                stream.WriteByte((byte)(Length << 8));
                stream.WriteByte((byte)Length);

                return stream.ToArray();
            } 
        }
    }
}