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
            NumberOfFilters = reader.ReadBitflagInt32(ref bitflags);

            FiltersOffset = reader.ReadBitflagInt32(ref bitflags);
            NumberOfSegments = reader.ReadBitflagInt32(ref bitflags);
            SegmentOffset = reader.ReadBitflagInt32(ref bitflags);
            EncryptionMode = reader.ReadBitflagInt16(ref bitflags);

            KeyLength = reader.ReadBitflagInt32(ref bitflags);
            KeyBuffer = reader.ReadBitflagInt32(ref bitflags);
            NumberOfOwnIdents = reader.ReadBitflagInt32(ref bitflags);
            IdentsOffset = reader.ReadBitflagInt32(ref bitflags);

            NumberOfSecurities = reader.ReadBitflagInt32(ref bitflags);
            SecuritiesOffset = reader.ReadBitflagInt32(ref bitflags);
            DataBlockType = reader.ReadBitflagStringWithReader(ref bitflags, BaseAddress);
            UniqueObjectId = reader.ReadBitflagInt32(ref bitflags);

            FlashDataInfoQualifier = reader.ReadBitflagStringWithReader(ref bitflags, BaseAddress);
            FlashDataInfoLongName = reader.ReadBitflagInt32(ref bitflags);
            FlashDataInfoDescription = reader.ReadBitflagInt32(ref bitflags);
            FlashDataInfoUniqueObjectId = reader.ReadBitflagInt32(ref bitflags);

            // CtfUnk1 = CaesarReader.ReadBitflagInt32(ref ctfBitflags, reader);
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

        public long GetBlockLengthOffset(CaesarReader reader)
        {
            reader.BaseStream.Seek(BaseAddress, SeekOrigin.Begin);

            ulong bitflags = reader.ReadUInt32();
            reader.ReadUInt16();

            reader.ReadBitflagStringWithReader(ref bitflags, BaseAddress); // Qualifier 
            reader.ReadBitflagInt32(ref bitflags); // LongName 
            reader.ReadBitflagInt32(ref bitflags); // Description 
            reader.ReadBitflagInt32(ref bitflags); // FlashData 

            if (reader.CheckAndAdvanceBitflag(ref bitflags))
            {
                return reader.BaseStream.Position;
            }
            else
            {
                return -1;
            }
        }
        public long GetFlashDataOffset(CaesarReader reader)
        {
            reader.BaseStream.Seek(BaseAddress, SeekOrigin.Begin);

            ulong bitflags = reader.ReadUInt32();
            reader.ReadUInt16();

            reader.ReadBitflagStringWithReader(ref bitflags, BaseAddress); // Qualifier 
            reader.ReadBitflagInt32(ref bitflags); // LongName 
            reader.ReadBitflagInt32(ref bitflags); // Description 

            if (reader.CheckAndAdvanceBitflag(ref bitflags))
            {
                return reader.BaseStream.Position;
            }
            else
            {
                return -1;
            }
        }

        public void PrintDebug()
        {
            Console.WriteLine($"{nameof(Qualifier)} : {Qualifier}");
            Console.WriteLine($"{nameof(LongName)} : {LongName}");
            Console.WriteLine($"{nameof(Description)} : {Description}");
            Console.WriteLine($"{nameof(FlashData)} : {FlashData}");
            Console.WriteLine($"{nameof(BlockLength)} : 0x{BlockLength:X}");
            Console.WriteLine($"{nameof(DataFormat)} : {DataFormat}");
            Console.WriteLine($"{nameof(FileName)} : {FileName}");
            Console.WriteLine($"{nameof(NumberOfFilters)} : {NumberOfFilters}");
            Console.WriteLine($"{nameof(FiltersOffset)} : {FiltersOffset}");
            Console.WriteLine($"{nameof(NumberOfSegments)} : {NumberOfSegments}");
            Console.WriteLine($"{nameof(SegmentOffset)} : {SegmentOffset}");
            Console.WriteLine($"{nameof(EncryptionMode)} : {EncryptionMode}");
            Console.WriteLine($"{nameof(KeyLength)} : {KeyLength}");
            Console.WriteLine($"{nameof(KeyBuffer)} : {KeyBuffer}");
            Console.WriteLine($"{nameof(NumberOfOwnIdents)} : {NumberOfOwnIdents}");
            Console.WriteLine($"{nameof(IdentsOffset)} : {IdentsOffset}");
            Console.WriteLine($"{nameof(NumberOfSecurities)} : {NumberOfSecurities}");
            Console.WriteLine($"{nameof(SecuritiesOffset)} : {SecuritiesOffset}");
            Console.WriteLine($"{nameof(DataBlockType)} : {DataBlockType}");
            Console.WriteLine($"{nameof(UniqueObjectId)} : {UniqueObjectId}");
            Console.WriteLine($"{nameof(FlashDataInfoQualifier)} : {FlashDataInfoQualifier}");
            Console.WriteLine($"{nameof(FlashDataInfoLongName)} : {FlashDataInfoLongName}");
            Console.WriteLine($"{nameof(FlashDataInfoDescription)} : {FlashDataInfoDescription}");
            Console.WriteLine($"{nameof(FlashDataInfoUniqueObjectId)} : {FlashDataInfoUniqueObjectId}");

        }
    }
}
