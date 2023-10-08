using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Caesar
{
    public class ComParameter
    {
        public short? ComParamIndex;
        // this takes precedence over SubinterfaceIndex for KW2C3PE
        public short? ParentInterfaceIndex;
        public short? SubinterfaceIndex;
        public short? Unk5;
        public int? Unk_CTF;
        public short? Phrase;
        private int? DumpSize;
        public byte[]? Dump;
       
        public int ComParamValue;
        public string ParamName = "";

        private long BaseAddress;
        CTFLanguage Language;

        public void Restore(CTFLanguage language) 
        {
            Language = language;
        }

        public ComParameter() 
        { 
            Language =new CTFLanguage();
        }

        // looks exactly like the definition in DIOpenDiagService (#T)
        public ComParameter(CaesarReader reader, long baseAddress, List<ECUInterface> parentEcuInterfaceList, CTFLanguage language) 
        {
            BaseAddress = baseAddress;
            Language = language;
            reader.BaseStream.Seek(baseAddress, SeekOrigin.Begin);
            ulong bitflags = reader.ReadUInt16();

            ComParamIndex = reader.ReadBitflagInt16(ref bitflags);
            ParentInterfaceIndex = reader.ReadBitflagInt16(ref bitflags);
            SubinterfaceIndex = reader.ReadBitflagInt16(ref bitflags);
            Unk5 = reader.ReadBitflagInt16(ref bitflags);
            Unk_CTF = reader.ReadBitflagInt32(ref bitflags); // no -1? ctf strings should have -1
            Phrase = reader.ReadBitflagInt16(ref bitflags);
            DumpSize = reader.ReadBitflagInt32(ref bitflags);
            Dump = reader.ReadBitflagDumpWithReader(ref bitflags, DumpSize, baseAddress);
            ComParamValue = 0;
            if (Dump != null && DumpSize == 4)
            {
                ComParamValue = BitConverter.ToInt32(Dump, 0);
            }

            if (ParentInterfaceIndex != null && ComParamIndex != null)
            {
                ECUInterface parentEcuInterface = parentEcuInterfaceList[(int)ParentInterfaceIndex];

                if (ComParamIndex >= parentEcuInterface.ComParameterNames.Count)
                {
                    // throw new Exception("Invalid communication parameter : parent interface has no matching key");
                    ParamName = "CP_UNKNOWN_MISSING_KEY";
                    Console.WriteLine($"Warning: Tried to load a communication parameter without a parent (value: {ComParamValue}), parent: {parentEcuInterface.Qualifier}.");
                }
                else 
                {
                    ParamName = parentEcuInterface.ComParameterNames[(int)ComParamIndex];
                }
            }
        }

        public void PrintDebug() 
        {
            Console.WriteLine($"ComParam: id {ComParamIndex} ({ParamName}), v {ComParamValue} 0x{ComParamValue:X8} SI_Index:{SubinterfaceIndex} | parentIndex:{ParentInterfaceIndex} 5:{Unk5} DumpSize:{DumpSize} D: {BitUtility.BytesToHex(Dump)}");
            Console.WriteLine($"Pos 0x{BaseAddress:X}");
        }
    }
}
