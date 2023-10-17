using System;
using System.Collections.Generic;
using System.Formats.Asn1;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Caesar.DSC
{
    public class DSCFile : DscObject
    {
        public string FileName = string.Empty;
        public string FileDescription = string.Empty;
        public DscTable<GlobalVariable> GlobalVars = new DscTable<GlobalVariable>();
        public GlobalVariableInitTable GlobalVarInits = new GlobalVariableInitTable();
        public DscTable<Function> Functions = new DscTable<Function>();
        public DscTable<Unknown1> UnknownTable1 = new DscTable<Unknown1>();

        public void Read(CaesarReader reader)
        {
            const int fnTableEntrySize = 50;
            //reader.BaseStream.Seek(0x10, SeekOrigin.Begin);
            int unk1 = reader.ReadInt32();
            int unk2 = reader.ReadInt32();
            int unk3 = reader.ReadInt32();
            int unk4 = reader.ReadInt32();
            Functions = new DscTable<Function>();
            Functions.Read(reader);
            UnknownTable1 = new DscTable<Unknown1>();
            UnknownTable1.Read(reader);

            int idk_field_1c = reader.ReadInt16(); // ?? @ 1C, padding

            ushort globalVarAllocSize = reader.ReadUInt16(); // @ 1E

            int idk_field_20 = reader.ReadInt16(); // ?? @ 20, padding
            GlobalVars = new DscTable<GlobalVariable>();
            GlobalVars.Read(reader);

            int globalVariablesIdk1 = reader.ReadInt32(); // ?? @ 28
            int globalVariablesIdk2 = reader.ReadInt16(); // ?? @ 2C

            GlobalVarInits = new GlobalVariableInitTable();
            GlobalVarInits.Read(reader);

            byte[] globalVarByteBuffer = new byte[globalVarAllocSize];
            GlobalVarInits.PopulateInitBuffer(globalVarByteBuffer);
            foreach (var gv in GlobalVars.Values)
            {
                gv.InitializeData(globalVarByteBuffer);
            }
            int stringTableOffsetMaybe = reader.ReadInt32();
            int newSize = reader.ReadInt16();
            int newInt5 = reader.ReadInt32();
            int newInt6 = reader.ReadInt32();
            int newInt7 = reader.ReadInt32();
            int newInt8 = reader.ReadInt32();
            int newOffset2 = reader.ReadInt32();
            int newOffset3 = reader.ReadInt16();

            // These two seem to always exist at these addresses
            reader.BaseStream.Seek(0x52, SeekOrigin.Begin);
            FileName = reader.ReadString(Encoding.UTF8);
            reader.BaseStream.Seek(0xE6, SeekOrigin.Begin);
            FileDescription = reader.ReadString(Encoding.UTF8);
            byte[] bytes5 = ReadBytesFromOffset(reader, newOffset2);
            byte[] bytes6 = ReadBytesFromOffset(reader, newOffset3);
            long oldPos = reader.BaseStream.Position;
            reader.BaseStream.Seek(newOffset2, SeekOrigin.Begin);
            long oldPos2 = reader.BaseStream.Position;
            int newInOffset = reader.ReadInt32();
            int newInSize = reader.ReadInt32();
            reader.BaseStream.Seek(newInOffset, SeekOrigin.Begin);
            List<Tuple<int, int>> addresses = new List<Tuple<int, int>>();
            //byte[] unk_buf = reader.ReadBytes(256);
            //Console.WriteLine(BitConverter.ToString(unk_buf));
            for (int i = 0; i < newInSize; i++)
            {
                int unknownInnerIndex1 = reader.ReadInt32();
                int unknownInnerIndex2 = reader.ReadInt32();
                addresses.Add(new Tuple<int, int>(unknownInnerIndex1, unknownInnerIndex2));
            }
            reader.BaseStream.Seek(oldPos2, SeekOrigin.Begin);

            reader.BaseStream.Seek(oldPos, SeekOrigin.Begin);
        }
        byte[] ReadBytesFromOffset(CaesarReader reader, long address)
        {
            long oldPos = reader.BaseStream.Position;
            reader.BaseStream.Seek(address, SeekOrigin.Begin);
            byte[] bytes = reader.ReadBytes(256);
            reader.BaseStream.Seek(oldPos, SeekOrigin.Begin);
            return bytes;
        }
    }
}
