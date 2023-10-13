using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Caesar.DSC
{
    public class GlobalVariableInit : CaesarObject
    {
        public int GvAddress = 0;
        public int Size = 0;
        public byte[] Data = new byte[0];

        // We do not set RelativeAddress here so that CaesarObject.Read does not try to seek to the new address
        protected override bool ReadHeader(CaesarReader reader)
        {
            GvAddress = reader.ReadInt16();
            Size = reader.ReadByte();
            Data = reader.ReadBytes(Size);
            return true;
        }

        //DSC-related objects have only headers
        protected override void ReadData(CaesarReader reader, CaesarContainer container)
        {
            throw new NotImplementedException();
        }
    }
}
