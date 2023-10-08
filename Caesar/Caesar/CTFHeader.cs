using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Caesar
{
    public class CTFHeader
    {
        public int? CtfUnk1;
        public string? Qualifier;
        public int? CtfUnk3;
        public int? CtfUnk4;
        private int? CtfLanguageCount;
        private int? CtfLanguageTableOffset;
        public string? CtfUnkString;

        public List<CTFLanguage> CtfLanguages;

        public long BaseAddress;

        public CTFHeader() 
        {
            BaseAddress = -1;
            CtfLanguages = new List<CTFLanguage>();
        }

        public CTFHeader(CaesarReader reader, long baseAddress, int headerSize) 
        {
            BaseAddress = baseAddress;
            reader.BaseStream.Seek(BaseAddress, SeekOrigin.Begin);
            ulong ctfBitflags = reader.ReadUInt16();

            CtfUnk1 = reader.ReadBitflagInt32(ref ctfBitflags);
            Qualifier = reader.ReadBitflagStringWithReader(ref ctfBitflags, BaseAddress);
            CtfUnk3 = reader.ReadBitflagInt16(ref ctfBitflags);
            CtfUnk4 = reader.ReadBitflagInt32(ref ctfBitflags);
            CtfLanguageCount = reader.ReadBitflagInt32(ref ctfBitflags);
            CtfLanguageTableOffset = reader.ReadBitflagInt32(ref ctfBitflags);
            CtfUnkString = reader.ReadBitflagStringWithReader(ref ctfBitflags, BaseAddress);

            // parse every language record
            CtfLanguages = new List<CTFLanguage>();
            if (CtfLanguageTableOffset != null)
            {
                long ctfLanguageTableOffsetRelativeToDefintions = (long)CtfLanguageTableOffset + BaseAddress;

                for (int languageEntry = 0; languageEntry < CtfLanguageCount; languageEntry++)
                {
                    long languageTableEntryOffset = ctfLanguageTableOffsetRelativeToDefintions + (languageEntry * 4);

                    reader.BaseStream.Seek(languageTableEntryOffset, SeekOrigin.Begin);
                    long realLanguageEntryAddress = reader.ReadInt32() + ctfLanguageTableOffsetRelativeToDefintions;
                    CTFLanguage language = new CTFLanguage(reader, realLanguageEntryAddress, headerSize);
                    CtfLanguages.Add(language);
                }
            }
        }
        public void PrintDebug() 
        {
            Console.WriteLine("----------- CTF header ----------- ");
            Console.WriteLine($"{nameof(CtfUnk1)} : {CtfUnk1}");
            Console.WriteLine($"{nameof(Qualifier)} : {Qualifier}");
            Console.WriteLine($"{nameof(CtfUnk3)} : {CtfUnk3}");
            Console.WriteLine($"{nameof(CtfUnk4)} : {CtfUnk4}");
            Console.WriteLine($"{nameof(CtfLanguageCount)} : {CtfLanguageCount}");
            Console.WriteLine($"{nameof(CtfLanguageTableOffset)} : 0x{CtfLanguageTableOffset:X}");
            Console.WriteLine($"{nameof(CtfUnkString)} : {CtfUnkString}");
        }
    }
}
