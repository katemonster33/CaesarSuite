using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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

        public byte[]? Dump1;
        public byte[]? Dump2;
        public byte[]? Dump3;
        public List<ushort> UnknownAddresses;
        public int GlobalVariableAddress { get; set; }
        public int DataSize { get; set; }
        
        public uint InstructionTableAddress { get; set; }
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
            UnknownAddresses = new List<ushort>();
        }

        public void Read(CaesarReader reader)
        {
            Identifier = reader.ReadInt16(); // @ 0
            long nameOffset = reader.ReadInt32();
            if(nameOffset < reader.BaseStream.Length)
            {
                Name = reader.ReadStringByAddress(0, (int)nameOffset); // @ 4
            }

            // not exactly sure if int32 is right -- the first fn's ep looks incorrect in both cases. 
            // 16 bit would limit the filesize to ~32KB which seems unlikely
            // first function address is junk but rest are good? not sure what is up with that
            InstructionTableAddress = reader.ReadUInt32(); // @ 8
            InstructionTableSize = reader.ReadInt16(); // @ 10
            if (InstructionTableAddress <= 0xFFFF)
            {
                InstructionDump = reader.ReadDump(InstructionTableAddress, InstructionTableSize);
            }
            TryReadFunctionDump(reader, out Dump1); // @ 12

            int newOffset4 = reader.ReadInt16(); // @ 18
            TryReadFunctionDump(reader, out Dump2); // @ 20
            TryReadFunctionDump(reader, out Dump3); // @ 26

            long strAddress = reader.ReadUInt32(); // @ 12
            int strCount = reader.ReadInt16(); // @ 16
            if (strAddress != 0 && (strAddress + (strCount * 2)) <= reader.BaseStream.Length && strCount <= 0xFF)
            {
                byte[] bytes = reader.ReadDump(strAddress, strCount * 2);// @ 32
                for (int i = 0; i < bytes.Length; i+= 2) 
                {
                    ushort addr = BitConverter.ToUInt16(bytes, i);
                    UnknownAddresses.Add(addr);
                }
            }

            InputParamOffset = reader.ReadInt32(); // @ 38
            InputParamCount = reader.ReadInt16(); // @ 42

            OutputParamOffset = reader.ReadInt32(); // @ 44
            OutputParamCount = reader.ReadInt16(); // @ 48
        }

        private bool TryReadFunctionDump(CaesarReader reader, [NotNullWhen(true)] out byte[]? dumpBytes)
        {
            long dumpAddress = reader.ReadUInt32(); // @ 12
            int dumpSize = reader.ReadInt16(); // @ 16
            dumpBytes = null;
            if (dumpAddress != 0 && (dumpAddress + dumpSize) <= reader.BaseStream.Length && dumpSize <= 0xFF)
            {
                dumpBytes = reader.ReadDump(dumpAddress, dumpSize);
                return true;
            }
            return false;
        }
    }
}
