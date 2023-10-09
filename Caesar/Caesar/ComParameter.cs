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
        ECU parentEcu;
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

        string? comParamName;
        public string ComParamName
        {
            get
            {
                if(comParamName == null)
                {
                    comParamName = GetComParamName();
                }
                return comParamName;
            }
            set => comParamName = value;
        }

        string GetComParamName()
        {
            if(ParentInterfaceIndex != null && ParentInterfaceIndex < parentEcu.ECUInterfaces.Count)
            {
                var ecuInt = parentEcu.ECUInterfaces[(int)ParentInterfaceIndex];
                if(ComParamIndex != null && ComParamIndex < ecuInt.ComParameterNames.Count)
                {
                    return ecuInt.ComParameterNames[(int)ComParamIndex];
                }
            }
            return "CP_UNKNOWN_PARAM";
        }

        private long BaseAddress;
        CTFLanguage Language;

        public void Restore(CTFLanguage language) 
        {
            Language = language;
        }

        public ComParameter() 
        { 
            Language =new CTFLanguage();
            parentEcu = new ECU();
        }

        // looks exactly like the definition in DIOpenDiagService (#T)
        public ComParameter(CaesarReader reader, long baseAddress, ECU parentEcu, CTFLanguage language) 
        {
            this.parentEcu = parentEcu;
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
        }

        public void PrintDebug() 
        {
            Console.WriteLine($"ComParam: id {ComParamIndex} name {ComParamName}, v {ComParamValue} 0x{ComParamValue:X8} SI_Index:{SubinterfaceIndex} | parentIndex:{ParentInterfaceIndex} 5:{Unk5} DumpSize:{DumpSize} D: {BitUtility.BytesToHex(Dump)}");
            Console.WriteLine($"Pos 0x{BaseAddress:X}");
        }
    }
}
