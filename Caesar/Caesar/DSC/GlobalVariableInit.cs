using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Caesar.DSC
{
    public class GlobalVariableInit : DscObject
    {
        public ushort GvAddress = 0;
        public byte Size = 0;
        public byte[] Data = new byte[0];

        // We do not set RelativeAddress here so that CaesarObject.Read does not try to seek to the new address
        public void Read(CaesarReader reader)
        {
            GvAddress = reader.ReadUInt16();
            Size = reader.ReadByte();
            Data = reader.ReadBytes(Size);
        }
    }
}
