using System;
using System.IO;

namespace Badoucai.Business.Socket
{
    public class CleaningProcedurePackage : Package
    {
        public override byte Id { get; set; }

        public override short Length { get; set; }

        public short CleaningId { get; set; }

        public byte AccountLength { get; set; }

        public byte[] Account { get; set; }

        public byte PasswordLength { get; set; }

        public byte[] Password { get; set; }

        public override byte[] Serialize()
        {
            using (var stream = new MemoryStream())
            {
                stream.WriteByte(Id);

                stream.WriteByte((byte)(Length << 8));
                stream.WriteByte((byte)Length);

                stream.WriteByte((byte)(CleaningId << 8));
                stream.WriteByte((byte)CleaningId);

                stream.WriteByte(AccountLength);

                stream.Write(Account,0, Account.Length);

                stream.WriteByte(PasswordLength);

                stream.Write(Password, 0, Password.Length);

                return stream.ToArray();
            }
        }

        public CleaningProcedurePackage DeSerialize(byte[] bytes)
        {
            return new CleaningProcedurePackage
            {
                Id = bytes[0],
                Length = (short)(bytes[1] << 8 | bytes[2]),
                CleaningId = (short)(bytes[3] << 8 | bytes[4]),
                AccountLength = bytes[5],
                Account = bytes.Copy(6, bytes[5]),
                PasswordLength = bytes[6 + bytes[5]],
                Password = bytes.Copy(7 + bytes[5], bytes[6 + bytes[5]])
            };
        }
    }
}