using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Caesar
{
    public class Scale
    {
        public long BaseAddress;

        // 0x0b [2,   4,4,4,4,    4,4,4,4,   4,4,4],

        public int? EnumLowBound;
        public int? EnumUpBound;
        public int? PrepLowBound;
        public int? PrepUpBound;

        public float? MultiplyFactor;
        public float? AddConstOffset;

        public int? SICount;
        public int? OffsetSI;

        public int? USCount;
        public int? OffsetUS;

        public int? EnumDescription;
        public int? UnkC;

        [Newtonsoft.Json.JsonIgnore]
        private CTFLanguage Language;

        public void Restore(CTFLanguage language) 
        {
            Language = language;
        }

        public Scale() 
        {
            Language = new CTFLanguage();
        }

        public Scale(CaesarReader reader, long baseAddress, CTFLanguage language) 
        {
            BaseAddress = baseAddress;
            Language = language;
            
            reader.BaseStream.Seek(BaseAddress, SeekOrigin.Begin);

            ulong bitflags = reader.ReadUInt16();

            EnumLowBound = reader.ReadBitflagInt32(ref bitflags);
            EnumUpBound = reader.ReadBitflagInt32(ref bitflags);

            PrepLowBound = reader.ReadBitflagInt32(ref bitflags); // could be float
            PrepUpBound = reader.ReadBitflagInt32(ref bitflags); // could be float

            MultiplyFactor = reader.ReadBitflagFloat(ref bitflags);
            AddConstOffset = reader.ReadBitflagFloat(ref bitflags);

            SICount = reader.ReadBitflagInt32(ref bitflags);
            OffsetSI = reader.ReadBitflagInt32(ref bitflags);

            USCount = reader.ReadBitflagInt32(ref bitflags);
            OffsetUS = reader.ReadBitflagInt32(ref bitflags);

            EnumDescription = reader.ReadBitflagInt32(ref bitflags);
            UnkC = reader.ReadBitflagInt32(ref bitflags);

        }

        public void PrintDebug()
        {
            Console.WriteLine($"{nameof(EnumLowBound)} : {EnumLowBound}");
            Console.WriteLine($"{nameof(EnumUpBound)} : {EnumUpBound}");
            Console.WriteLine($"{nameof(PrepLowBound)} : {PrepLowBound}");
            Console.WriteLine($"{nameof(PrepUpBound)} : {PrepUpBound}");

            Console.WriteLine($"{nameof(MultiplyFactor)} : {MultiplyFactor}");
            Console.WriteLine($"{nameof(AddConstOffset)} : {AddConstOffset}");
            Console.WriteLine($"{nameof(SICount)} : {SICount}");
            Console.WriteLine($"{nameof(OffsetSI)} : {OffsetSI}");

            Console.WriteLine($"{nameof(USCount)} : {USCount}");
            Console.WriteLine($"{nameof(OffsetUS)} : {OffsetUS}");
            Console.WriteLine($"{nameof(EnumDescription)} : {EnumDescription}");
            Console.WriteLine($"{nameof(UnkC)} : {UnkC}");
        }
    }
}
