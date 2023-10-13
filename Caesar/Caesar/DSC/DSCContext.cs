using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Caesar.DSC
{
    public class DSCContext : CaesarObject
    {
        public int DataSize = 0;
        public string? Qualifier;
        public DscList<GlobalVariable> GlobalVars = new DscList<GlobalVariable>();
        public GlobalVariableInitTable GlobalVarInits = new GlobalVariableInitTable();
        public DscList<Function> Functions = new DscList<Function>();

        protected override bool ReadHeader(CaesarReader reader)
        {
            base.ReadHeader(reader);
            DataSize = reader.ReadInt32();
            return true;
        }

        protected override void ReadData(CaesarReader reader, CaesarContainer container)
        {
            const int fnTableEntrySize = 50;
            //reader.BaseStream.Seek(0x10, SeekOrigin.Begin);
            int unk1 = reader.ReadInt32();
            int unk2 = reader.ReadInt32();
            int unk3 = reader.ReadInt32();
            int unk4 = reader.ReadInt32();
            Functions = new DscList<Function>();
            Functions.Read(reader, this, container);
            int dscOffsetA = reader.ReadInt32(); // @ 0x16, originally i16
            int caesarHash = reader.ReadInt16(); // @ 0x1A, size is u32?

            int idk_field_1c = reader.ReadInt16(); // ?? @ 1C, padding

            int globalVarAllocSize = reader.ReadInt16(); // @ 1E

            int idk_field_20 = reader.ReadInt16(); // ?? @ 20, padding
            GlobalVars = new DscList<GlobalVariable>();
            GlobalVars.Read(reader, this, container);

            int globalVariablesIdk1 = reader.ReadInt32(); // ?? @ 28
            int globalVariablesIdk2 = reader.ReadInt16(); // ?? @ 2C

            GlobalVarInits = new GlobalVariableInitTable();
            GlobalVarInits.Read(reader, this, container);

            byte[] globalVarByteBuffer = new byte[globalVarAllocSize];
            GlobalVarInits.PopulateInitBuffer(globalVarByteBuffer);
            foreach(var gv in GlobalVars.GetObjects())
            {
                gv.InitializeData(globalVarByteBuffer);
            }
        }
    }
}
