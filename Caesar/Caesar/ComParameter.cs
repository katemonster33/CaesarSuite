﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Caesar
{
    public class ComParameter : CaesarObject
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
            ECU? parentEcu = GetParentByType<ECU>();
            if(parentEcu != null && ParentInterfaceIndex != null && ParentInterfaceIndex < parentEcu.ECUInterfaces.Count)
            {
                var ecuInt = parentEcu.ECUInterfaces[(int)ParentInterfaceIndex];
                if(ComParamIndex != null && ComParamIndex < ecuInt.ComParameterNames.Count)
                {
                    return ecuInt.ComParameterNames[(int)ComParamIndex];
                }
            }
            return "CP_UNKNOWN_PARAM";
        }

        public void Restore(CTFLanguage language) 
        {

        }

        public void PrintDebug() 
        {
            Console.WriteLine($"ComParam: id {ComParamIndex} name {ComParamName}, v {ComParamValue} 0x{ComParamValue:X8} SI_Index:{SubinterfaceIndex} | parentIndex:{ParentInterfaceIndex} 5:{Unk5} DumpSize:{DumpSize} D: {BitUtility.BytesToHex(Dump)}");
            Console.WriteLine($"Pos 0x{AbsoluteAddress:X}");
        }

        protected override void ReadData(CaesarReader reader, CaesarContainer container)
        {
            Bitflags = reader.ReadUInt16();
            ComParamIndex = reader.ReadBitflagInt16(ref Bitflags);
            ParentInterfaceIndex = reader.ReadBitflagInt16(ref Bitflags);
            SubinterfaceIndex = reader.ReadBitflagInt16(ref Bitflags);
            Unk5 = reader.ReadBitflagInt16(ref Bitflags);
            Unk_CTF = reader.ReadBitflagInt32(ref Bitflags); // no -1? ctf strings should have -1
            Phrase = reader.ReadBitflagInt16(ref Bitflags);
            DumpSize = reader.ReadBitflagInt32(ref Bitflags);
            Dump = reader.ReadBitflagDumpWithReader(ref Bitflags, DumpSize, AbsoluteAddress);
            ComParamValue = 0;
            if (Dump != null && DumpSize == 4)
            {
                ComParamValue = BitConverter.ToInt32(Dump, 0);
            }
        }
    }
}
