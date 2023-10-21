using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Caesar
{
    public class FlashSegment
    {
        public int? FromAddress;
        public int? SegmentLength;
        public int? DataLength; // almost always 0, useful stuff in SegmentLength instead
        public string? SegmentName;

        public int? LongNameCTF;
        public int? DescriptionCTF;
        public string? UniqueObjectID;

        //    SEGMENT_TABLE_STRUCTURE  2,   4, 4, 4, 4,  4, 4, 4
        public long BaseAddress;

        public FlashSegment(CaesarReader reader, long baseAddress)
        {
            BaseAddress = baseAddress;
            reader.BaseStream.Seek(baseAddress, SeekOrigin.Begin);

            ulong bitFlags = reader.ReadUInt16();

            FromAddress = reader.ReadBitflagInt32(ref bitFlags);
            SegmentLength = reader.ReadBitflagInt32(ref bitFlags);
            DataLength = reader.ReadBitflagInt32(ref bitFlags);
			
			// DIGetDataBlockSegmentStrings fills up the last 4 entries / caesar reads
            SegmentName = reader.ReadBitflagStringWithReader(ref bitFlags, BaseAddress);
            LongNameCTF = reader.ReadBitflagInt32(ref bitFlags);
            DescriptionCTF = reader.ReadBitflagInt32(ref bitFlags);
			
            // confusing as to why this is a string
            UniqueObjectID = reader.ReadBitflagStringWithReader(ref bitFlags, BaseAddress);
			
            // segment only has a relative pointer to the actual data bytes
        }

    }
}
