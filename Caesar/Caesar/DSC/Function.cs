using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Caesar.DSC
{
    public class Function : CaesarObject
    {
        public int Identifier { get; set; }

        public string Name { get; set; }

        public int GlobalVariableAddress { get; set; }
        public int DataSize { get; set; }

        public int VarAddr1 { get; set; }
        public int VarSize1 { get; set; }
        public int VarAddr2 { get; set; }
        public int VarSize2 { get; set; }
        public int VarAddr3 { get; set; }
        public int VarSize3 { get; set; }
        
        public int EntryPoint { get; set; }

        public int InputParamOffset { get; set; }
        public int InputParamCount { get; set; }

        public int OutputParamOffset { get; set; }
        public int OutputParamCount { get; set; }

        public Function() 
        {
            Name = string.Empty;
        }

        protected override bool ReadHeader(CaesarReader reader)
        {
            long startAddress = reader.BaseStream.Position;
            Identifier = reader.ReadInt16(); // @ 0
            var dscParent = GetParentByType<DSCContext>();
            int parentAddress = dscParent != null ? dscParent.AbsoluteAddress : 0;
            Name = reader.ReadStringByAddress(parentAddress);

            // not exactly sure if int32 is right -- the first fn's ep looks incorrect in both cases. 
            // 16 bit would limit the filesize to ~32KB which seems unlikely

            //EntryPoint = reader.ReadInt32(); // @ 6
            EntryPoint = reader.ReadInt16();
            int entry2 = reader.ReadInt16(); // @ 6
            int entry3= reader.ReadInt16(); // @ 6
            GlobalVariableAddress = reader.ReadInt16(); // @ 12
            int offset2 = reader.ReadInt16(); // @ 14
            DataSize = reader.ReadInt16(); // @ 16
            int newOffset4 = reader.ReadInt16(); // @ 18
            VarAddr1 = reader.ReadInt32(); // @ 20
            VarSize1= reader.ReadInt16(); // @ 24
            VarAddr2= reader.ReadInt32(); // @ 26
            VarSize2= reader.ReadInt16(); // @ 30
            VarAddr3= reader.ReadInt32(); // @ 32
            VarSize3= reader.ReadInt16(); // @ 36
            InputParamOffset = reader.ReadInt32(); // @ 38
            InputParamCount = reader.ReadInt16(); // @ 42

            OutputParamOffset = reader.ReadInt32(); // @ 44
            OutputParamCount = reader.ReadInt16(); // @ 48
            return true;
        }

        protected override void ReadData(CaesarReader reader, CaesarContainer container)
        {
            throw new NotImplementedException();
        }
    }
}
