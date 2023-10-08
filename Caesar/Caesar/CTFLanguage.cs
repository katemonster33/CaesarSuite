using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Caesar
{
    public class CTFLanguage
    {
        public string? Qualifier;
        public int? LanguageIndex;
        private int? StringPoolSize;
        private int? MaybeOffsetFromStringPoolBase;
        private int? StringCount;
        public List<string>? StringEntries;

        public long BaseAddress;
        public CTFLanguage() { }
        public CTFLanguage(CaesarReader reader, long baseAddress, int headerSize) 
        {
            BaseAddress = baseAddress;
            reader.BaseStream.Seek(BaseAddress, SeekOrigin.Begin);
            ulong languageEntryBitflags = reader.ReadUInt16();

            Qualifier = reader.ReadBitflagStringWithReader(ref languageEntryBitflags, BaseAddress);
            LanguageIndex = reader.ReadBitflagInt16(ref languageEntryBitflags);
            StringPoolSize = reader.ReadBitflagInt32(ref languageEntryBitflags);
            MaybeOffsetFromStringPoolBase = reader.ReadBitflagInt32(ref languageEntryBitflags);
            StringCount = reader.ReadBitflagInt32(ref languageEntryBitflags);

            LoadStrings(reader, headerSize);
        }

        public void LoadStrings(CaesarReader reader, int headerSize) 
        {
            if (StringCount != null)
            {
                StringEntries = new List<string>();
                int caesarStringTableOffset = headerSize + 0x410 + 4; // header.CffHeaderSize; strange that this has to be manually computed
                for (int i = 0; i < StringCount; i++)
                {
                    reader.BaseStream.Seek(caesarStringTableOffset + (i * 4), SeekOrigin.Begin);
                    int stringOffset = reader.ReadInt32();
                    reader.BaseStream.Seek(caesarStringTableOffset + stringOffset, SeekOrigin.Begin);
                    string result = reader.ReadString(Encoding.UTF8);
                    StringEntries.Add(result);
                }
            }
            else StringEntries = null;
        }

        public string? GetString(int? stringId) 
        {
            if (stringId != null)
            {
                return GetString(StringEntries, (int)stringId);
            }
            else
            { 
                return null; 
            }
        }

        public static string GetString(List<string>? language, int stringId) 
        {
            if (stringId < 0) 
            {
                return "";
            }
            if (language == null || stringId > language.Count) 
            {
                return "";
            }
            return language[stringId];
        }

        public void PrintDebug() 
        {
            Console.WriteLine($"Language: {Qualifier} stringCount: {StringCount} stringPoolSize 0x{StringPoolSize:X}, unknowns: {LanguageIndex} {MaybeOffsetFromStringPoolBase}, base: {BaseAddress:X} ");
        }
    }
}
