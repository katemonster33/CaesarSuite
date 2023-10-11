using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Text.Json.Serialization;

namespace Caesar
{
    public class CTFLanguage : CaesarObject
    {
        public string? Qualifier;
        public int? LanguageIndex;
        private int? StringPoolSize;
        private int? MaybeOffsetFromStringPoolBase;
        private int? StringCount;
        [JsonIgnore]
        public List<string>? StringEntries;


        public void LoadStrings(CaesarReader reader, int headerSize) 
        {
            if (StringCount != null)
            {
                long oldPos = reader.BaseStream.Position;
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
                reader.BaseStream.Seek(oldPos, SeekOrigin.Begin);
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
            Console.WriteLine($"Language: {Qualifier} stringCount: {StringCount} stringPoolSize 0x{StringPoolSize:X}, unknowns: {LanguageIndex} {MaybeOffsetFromStringPoolBase}, base: {AbsoluteAddress:X} ");
        }

        protected override void ReadData(CaesarReader reader, CaesarContainer container)
        {
            Bitflags = reader.ReadUInt16();

            Qualifier = reader.ReadBitflagStringWithReader(ref Bitflags, AbsoluteAddress);
            LanguageIndex = reader.ReadBitflagInt16(ref Bitflags);
            StringPoolSize = reader.ReadBitflagInt32(ref Bitflags);
            MaybeOffsetFromStringPoolBase = reader.ReadBitflagInt32(ref Bitflags);
            StringCount = reader.ReadBitflagInt32(ref Bitflags);
        }
    }
}
