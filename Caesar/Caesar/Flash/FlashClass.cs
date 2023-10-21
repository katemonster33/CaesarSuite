using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Caesar
{
    public class FlashClass
    {
        public string? Identifier;
        public string? Qualifier;
        public int? LongNameCTF;
        public int? DescriptionCTF;
        public int? UniqueObjectID;

        public long BaseAddress;
        // 2,   4, 4, 4, 4, 4
        public FlashClass(CaesarReader reader, long baseAddress)
        {
            BaseAddress = baseAddress;
            reader.BaseStream.Seek(baseAddress, SeekOrigin.Begin);

            ulong bitFlags = reader.ReadUInt16();

            Identifier = reader.ReadBitflagStringWithReader(ref bitFlags, BaseAddress); // @1
            Qualifier = reader.ReadBitflagStringWithReader(ref bitFlags, BaseAddress); // @2
            LongNameCTF = reader.ReadBitflagInt32(ref bitFlags); // @3
            DescriptionCTF = reader.ReadBitflagInt32(ref bitFlags); // @4
            UniqueObjectID = reader.ReadBitflagInt32(ref bitFlags); // @5
        }
    }
}

