using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Caesar
{
    public class Scale : CaesarObject
    {
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

        public CaesarStringReference? EnumDescription;
        public int? UnkC;

        public Scale()
        {
        }

        public override string ToString()
        {
            return $"{nameof(EnumLowBound)}={EnumLowBound}, " +
                $"{nameof(EnumUpBound)}={EnumUpBound}, " +
                $"{nameof(PrepLowBound)}={PrepLowBound}, " +
                $"{nameof(PrepUpBound)}={PrepUpBound}, " +

                $"{nameof(MultiplyFactor)}={MultiplyFactor}, " +
                $"{nameof(AddConstOffset)}={AddConstOffset}, " +
                $"{nameof(SICount)}={SICount}, " +
                $"{nameof(OffsetSI)}={OffsetSI}, " +

                $"{nameof(USCount)}={USCount}, " +
                $"{nameof(OffsetUS)}={OffsetUS}, " +
                $"{nameof(EnumDescription)}={EnumDescription}, " +
                $"{nameof(UnkC)}={UnkC}, ";
        }

        protected override void ReadData(CaesarReader reader, CaesarContainer container)
        {
            Bitflags = reader.ReadUInt16();

            EnumLowBound = reader.ReadBitflagInt32(ref Bitflags);
            EnumUpBound = reader.ReadBitflagInt32(ref Bitflags);

            PrepLowBound = reader.ReadBitflagInt32(ref Bitflags); // could be float
            PrepUpBound = reader.ReadBitflagInt32(ref Bitflags); // could be float

            MultiplyFactor = reader.ReadBitflagFloat(ref Bitflags);
            AddConstOffset = reader.ReadBitflagFloat(ref Bitflags);

            SICount = reader.ReadBitflagInt32(ref Bitflags);
            OffsetSI = reader.ReadBitflagInt32(ref Bitflags);

            USCount = reader.ReadBitflagInt32(ref Bitflags);
            OffsetUS = reader.ReadBitflagInt32(ref Bitflags);

            EnumDescription = reader.ReadBitflagStringRef(ref Bitflags, container);
            UnkC = reader.ReadBitflagInt32(ref Bitflags);
        }
    }
}
