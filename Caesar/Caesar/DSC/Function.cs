using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Caesar.DSC
{
    public class Function : DscObject
    {
        public int Identifier { get; set; }

        public string Name { get; set; }

        public byte[]? Dump1 { get; set; }
        public byte[]? Dump2 { get; set; }
        public byte[]? Dump3 { get; set; }
        public byte[]? Dump4 { get; set; }
        public int GlobalVariableAddress { get; set; }
        public int DataSize { get; set; }
        
        public int InstructionTableAddress { get; set; }
        public int InstructionTableSize { get; set; }
        public byte[] InstructionDump { get; set; } 

        public int InputParamOffset { get; set; }
        public int InputParamCount { get; set; }

        public int OutputParamOffset { get; set; }
        public int OutputParamCount { get; set; }

        public Function() 
        {
            Name = string.Empty;
            InstructionDump = new byte[0];
        }

        public void Read(CaesarReader reader)
        {
            Identifier = reader.ReadInt16(); // @ 0
            Name = reader.ReadStringByAddress(0); // @ 4

            // not exactly sure if int32 is right -- the first fn's ep looks incorrect in both cases. 
            // 16 bit would limit the filesize to ~32KB which seems unlikely
            // first function address is junk but rest are good? not sure what is up with that
            InstructionTableAddress = reader.ReadInt32(); // @ 8
            InstructionTableSize = reader.ReadInt16(); // @ 10
            if(InstructionTableAddress <= 0xFFFF)
            {
                InstructionDump = reader.ReadDump(InstructionTableAddress, InstructionTableSize);
            }
            int dumpAddress = reader.ReadInt32(); // @ 12
            int dumpSize = reader.ReadInt16(); // @ 16
            if (dumpAddress != 0 && dumpSize <= 0xFF)
            {
                Dump1 = reader.ReadDump(dumpAddress, dumpSize);
                reader.ReadDump(dumpAddress, dumpSize);
            }
            int newOffset4 = reader.ReadInt16(); // @ 18
            dumpAddress = reader.ReadInt32(); // @ 20
            dumpSize = reader.ReadInt16(); // @ 24
            if (dumpAddress != 0 && dumpSize <= 0xFF)
            {
                Dump2 = reader.ReadDump(dumpAddress, dumpSize);
                reader.ReadDump(dumpAddress, dumpSize);
            }
            dumpAddress = reader.ReadInt32(); // @ 26
            dumpSize = reader.ReadInt16(); // @ 30
            if (dumpAddress != 0 && dumpSize <= 0xFF)
            {
                Dump3 = reader.ReadDump(dumpAddress, dumpSize);
                reader.ReadDump(dumpAddress, dumpSize);
            }
            dumpAddress = reader.ReadInt32(); // @ 32
            dumpSize = reader.ReadInt16(); // @ 36
            if (dumpAddress != 0 && dumpSize <= 0xFF)
            {
                Dump4 = reader.ReadDump(dumpAddress, dumpSize);
                reader.ReadDump(dumpAddress, dumpSize);
            }
            InputParamOffset = reader.ReadInt32(); // @ 38
            InputParamCount = reader.ReadInt16(); // @ 42

            OutputParamOffset = reader.ReadInt32(); // @ 44
            OutputParamCount = reader.ReadInt16(); // @ 48
        }
    }
}
