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
    public class GlobalVariableInitTable : DscTable<GlobalVariableInit>
    {
        public int BlockSize;
        public GlobalVariableInitTable() 
        {
            BlockSize = 0;
        }

        public override void Read(CaesarReader reader)
        {
            RelativeAddress = reader.ReadInt32(); // ?? @ 2E
            BlockSize = reader.ReadInt16(); // ?? @ 32

            Values.Clear();
            long oldPos = reader.BaseStream.Position;
            reader.BaseStream.Seek(RelativeAddress, System.IO.SeekOrigin.Begin);
            long endAddress = reader.BaseStream.Position + BlockSize;
            while(reader.BaseStream.Position < endAddress)
            {
                var newGv = new GlobalVariableInit();
                newGv.Read(reader);
                Values.Add(newGv);
            }
            Debug.Assert(reader.BaseStream.Position == endAddress, "Global variable preinit has leftover data in the read cursor");
            reader.BaseStream.Seek(oldPos, System.IO.SeekOrigin.Begin);
            EntryCount = Values.Count;
        }

        public void PopulateInitBuffer(byte[] buffer)
        {
            foreach(var obj in Values)
            {
                Buffer.BlockCopy(obj.Data, 0, buffer, obj.GvAddress, obj.Size);
            }
        }
    }
}
