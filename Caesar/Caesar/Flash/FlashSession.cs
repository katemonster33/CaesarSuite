using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Caesar
{
    public class FlashSession
    {
        public string? Qualifier;
        public int? LongNameCTF;
        public int? DescriptionCTF;

        public long BaseAddress;
        // 2,    4, 4, 4, 4,   4, 4, 4, 4,   4, 2
        public FlashSession(CaesarReader reader, long baseAddress)
        {
            BaseAddress = baseAddress;
            reader.BaseStream.Seek(baseAddress, SeekOrigin.Begin);

            ulong bitFlags = reader.ReadUInt16();

            Qualifier = reader.ReadBitflagStringWithReader(ref bitFlags, BaseAddress); // @1
            LongNameCTF = reader.ReadBitflagInt32(ref bitFlags); // @2
            DescriptionCTF = reader.ReadBitflagInt32(ref bitFlags); // @3

            // as far as i can tell, these are all indexes (integers)
            int? identCount = reader.ReadBitflagInt32(ref bitFlags); // @4
            int? identOffset = reader.ReadBitflagInt32(ref bitFlags); // @5

            int? securitiesCount = reader.ReadBitflagInt32(ref bitFlags); // @6
            int? securitiesOffset = reader.ReadBitflagInt32(ref bitFlags); // @7

            int? datablocksCount = reader.ReadBitflagInt32(ref bitFlags); // @8
            int? datablocksOffset = reader.ReadBitflagInt32(ref bitFlags); // @9

            int? FlashMethod = reader.ReadBitflagInt16(ref bitFlags); // @10
        }
    }
}

