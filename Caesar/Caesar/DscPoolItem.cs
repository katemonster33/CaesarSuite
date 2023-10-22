using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Caesar
{
    public class DscPoolItem : CaesarObject
    {
        public int? DscIndex;
        public string? Qualifier;

        protected override void ReadData(CaesarReader reader, CaesarContainer container)
        {
            Bitflags = reader.ReadUInt16();
            uint? idk1 = reader.ReadBitflagUInt8(ref Bitflags);
            uint? idk2 = reader.ReadBitflagUInt8(ref Bitflags);
            DscIndex = reader.ReadBitflagInt32(ref Bitflags);
            Qualifier = reader.ReadBitflagStringWithReader(ref Bitflags, AbsoluteAddress);
        }
    }
}
