using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Caesar
{
    public class FlashIdent
    {
        public string? Qualifier;
        public int? LongNameCTF;
        public int? DescriptionCTF;
        public int? UniqueObjectID;

        public long BaseAddress;
        // 2,     4, 4, 4, 4,   4, 4 
        public FlashIdent(CaesarReader reader, long baseAddress)
        {
            BaseAddress = baseAddress;
            reader.BaseStream.Seek(baseAddress, SeekOrigin.Begin);

            ulong bitFlags = reader.ReadUInt16();

            Qualifier = reader.ReadBitflagStringWithReader(ref bitFlags, BaseAddress); // @1
            LongNameCTF = reader.ReadBitflagInt32(ref bitFlags); // @2
            DescriptionCTF = reader.ReadBitflagInt32(ref bitFlags); // @3

            // FIXME: does this point to FlashIdentServiceInfo?
            int? numberOfValues = reader.ReadBitflagInt32(ref bitFlags); // @4
            int? offsetToValues = reader.ReadBitflagInt32(ref bitFlags); // @5

            UniqueObjectID = reader.ReadBitflagInt32(ref bitFlags); // @6
        }
    }
}

