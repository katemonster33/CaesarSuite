using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Caesar.DSC
{
    public class GlobalVariableInitTable : CaesarTable<GlobalVariableInit>
    {
        public GlobalVariableInitTable() 
        {
            BlockSize = 0;
        }

        protected override bool ReadHeader(CaesarReader reader)
        {
            RelativeAddress = reader.ReadInt32() + AbsoluteAddress; // ?? @ 2E
            BlockSize = reader.ReadInt16(); // ?? @ 32

            return true;
        }

        protected override void ReadData(CaesarReader reader, CaesarContainer container)
        {
            Objects.Clear();
            long endAddress = reader.BaseStream.Position + (BlockSize ?? 0);
            while(reader.BaseStream.Position < endAddress)
            {
                var newGv = new GlobalVariableInit();
                newGv.Read(reader, this, container);
                Objects.Add(newGv);
            }
            Debug.Assert(reader.BaseStream.Position == endAddress, "Global variable preinit has leftover data in the read cursor");
        }

        public void PopulateInitBuffer(byte[] buffer)
        {
            foreach(var obj in Objects)
            {
                Buffer.BlockCopy(obj.Data, 0, buffer, obj.GvAddress, obj.Size);
            }
        }
    }
}
