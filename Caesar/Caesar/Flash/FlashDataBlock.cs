using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Caesar
{
    public class FlashDataBlock
    {

        // 0x16 [6,  4,4,4,4,  4,4,4,4,  4,4,4,2,  4,4,4,4,  4,4,4,4,  4,4,4,4,4],
        public string? Qualifier;
        public int? LongName;
        public int? Description;
        public int? FlashData;
        public int? BlockLength;
        public int? DataFormat;
        public int? FileName;
        public int? NumberOfFilters;
        public int? FiltersOffset;
        public int? NumberOfSegments;
        public int? SegmentOffset;
        public int? EncryptionMode;
        public int? KeyLength;
        public int? KeyBuffer;
        public int? NumberOfOwnIdents;
        public int? IdentsOffset;
        public int? NumberOfSecurities;
        public int? SecuritiesOffset;
        public string? DataBlockType;
        public int? UniqueObjectId;
        public string? FlashDataInfoQualifier;
        public int? FlashDataInfoLongName;
        public int? FlashDataInfoDescription;
        public int? FlashDataInfoUniqueObjectId;

        public long BaseAddress;
        public List<FlashSegment> FlashSegments = new List<FlashSegment>();
        public List<FlashSecurity> FlashSecurities = new List<FlashSecurity>();

        public FlashDataBlock(CaesarReader reader, long baseAddress)
        {
            BaseAddress = baseAddress;
            reader.BaseStream.Seek(BaseAddress, SeekOrigin.Begin);
            
            ulong bitflags = reader.ReadUInt32();
            reader.ReadUInt16();

            Qualifier = reader.ReadBitflagStringWithReader(ref bitflags, BaseAddress);
            LongName = reader.ReadBitflagInt32(ref bitflags);
            Description = reader.ReadBitflagInt32(ref bitflags);
            FlashData = reader.ReadBitflagInt32(ref bitflags);

            BlockLength = reader.ReadBitflagInt32(ref bitflags);
            DataFormat = reader.ReadBitflagInt32(ref bitflags);
            FileName = reader.ReadBitflagInt32(ref bitflags);
            NumberOfFilters = reader.ReadBitflagInt32(ref bitflags); // unparsed

            FiltersOffset = reader.ReadBitflagInt32(ref bitflags);
            NumberOfSegments = reader.ReadBitflagInt32(ref bitflags);
            SegmentOffset = reader.ReadBitflagInt32(ref bitflags);
            EncryptionMode = reader.ReadBitflagInt16(ref bitflags);

            KeyLength = reader.ReadBitflagInt32(ref bitflags);
            KeyBuffer = reader.ReadBitflagInt32(ref bitflags);
            NumberOfOwnIdents = reader.ReadBitflagInt32(ref bitflags); // unparsed
            IdentsOffset = reader.ReadBitflagInt32(ref bitflags);

            NumberOfSecurities = reader.ReadBitflagInt32(ref bitflags);
            SecuritiesOffset = reader.ReadBitflagInt32(ref bitflags);
            DataBlockType = reader.ReadBitflagStringWithReader(ref bitflags, BaseAddress);
            UniqueObjectId = reader.ReadBitflagInt32(ref bitflags);

            FlashDataInfoQualifier = reader.ReadBitflagStringWithReader(ref bitflags, BaseAddress);
            FlashDataInfoLongName = reader.ReadBitflagInt32(ref bitflags);
            FlashDataInfoDescription = reader.ReadBitflagInt32(ref bitflags);
            FlashDataInfoUniqueObjectId = reader.ReadBitflagInt32(ref bitflags);

            FlashSegments = new List<FlashSegment>();
            if (NumberOfSegments != null && SegmentOffset != null)
            {
                for (int segmentIndex = 0; segmentIndex < NumberOfSegments; segmentIndex++)
                {
                    long segmentEntryAddress = (long)SegmentOffset + BaseAddress + (segmentIndex * 4);
                    reader.BaseStream.Seek(segmentEntryAddress, SeekOrigin.Begin);

                    long segmentBaseAddress = (long)SegmentOffset + BaseAddress + reader.ReadInt32();

                    FlashSegment segment = new FlashSegment(reader, segmentBaseAddress);
                    FlashSegments.Add(segment);
                }
            }

            FlashSecurities = new List<FlashSecurity>();
            if (NumberOfSecurities != null && SecuritiesOffset != null)
            {
                for (int securitiesIndex = 0; securitiesIndex < NumberOfSecurities; securitiesIndex++)
                {
                    long securitiesEntryAddress = (long)SecuritiesOffset + BaseAddress + (securitiesIndex * 4);
                    reader.BaseStream.Seek(securitiesEntryAddress, SeekOrigin.Begin);

                    long securitiesBaseAddress = (long)SecuritiesOffset + BaseAddress + reader.ReadInt32();
                    FlashSecurity security = new FlashSecurity(reader, securitiesBaseAddress);
                    FlashSecurities.Add(security);
                }
            }
        }
    }
}