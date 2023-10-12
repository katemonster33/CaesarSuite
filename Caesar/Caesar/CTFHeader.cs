using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Caesar
{
    public class CTFHeader : CaesarObject
    {
        public int? CtfUnk1;
        public string? Qualifier;
        public short? CtfUnk3;
        public int? CtfUnk4;
        public CaesarTable<CTFLanguage> CtfLanguages = new CaesarTable<CTFLanguage>();
        public string? CtfUnkString;
        public string? CtfModuleName;
        public string? CtfXmlDefinition;

        public void PrintDebug() 
        {
            Console.WriteLine("----------- CTF header ----------- ");
            Console.WriteLine($"{nameof(CtfUnk1)} : {CtfUnk1}");
            Console.WriteLine($"{nameof(Qualifier)} : {Qualifier}");
            Console.WriteLine($"{nameof(CtfUnk3)} : {CtfUnk3}");
            Console.WriteLine($"{nameof(CtfUnk4)} : {CtfUnk4}");
            Console.WriteLine($"{nameof(CtfLanguages)} : {CtfLanguages}");
            Console.WriteLine($"{nameof(CtfUnkString)} : {CtfUnkString}");
        }

        protected override void ReadData(CaesarReader reader, CaesarContainer container)
        {
            Bitflags = reader.ReadUInt16();

            CtfUnk1 = reader.ReadBitflagInt32(ref Bitflags);
            Qualifier = reader.ReadBitflagStringWithReader(ref Bitflags, AbsoluteAddress);
            CtfUnk3 = reader.ReadBitflagInt16(ref Bitflags);
            CtfUnk4 = reader.ReadBitflagInt32(ref Bitflags);
            CtfLanguages = reader.ReadBitflagSubTableAlt<CTFLanguage>(this, container) ?? new CaesarTable<CTFLanguage>();
            CtfUnkString = reader.ReadBitflagStringWithReader(ref Bitflags, AbsoluteAddress);
            CtfXmlDefinition = reader.ReadBitflagStringWithReader(ref Bitflags, AbsoluteAddress);
            short? stmp = reader.ReadBitflagInt16(ref Bitflags);
            if(stmp != null)
            {
                byte[] bytes = reader.ReadBytes((int)stmp);
                CtfModuleName = Encoding.UTF8.GetString(bytes);
            }
        }

        public void LoadStrings(CaesarReader reader, int headerSize)
        {
            foreach (var lang in CtfLanguages.GetObjects())
            {
                lang.LoadStrings(reader, headerSize);
            }
        }
    }
}
