using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Caesar
{
    public class FlashHeader
    {
        public int CffHeaderSize;
        public long BaseAddress;

        public string? FlashName;
        public string? CFFTrafoArguments;
        public int? NameCTF;
        public int? DescriptionCTF;
        public string? FileAuthor;
        public string? FileCreationTime;
        public string? AuthoringToolVersion;
        public string? FTRAFOVersionString;
        public int? FTRAFOVersionNumber;
        public string? CFFVersionString;
        public int? NumberOfFlashAreas;
        public int? FlashDescriptionTable;
        public int? DataBlockTableCount;
        public int? DataBlockRefTable;
        public int? LanguageHeaderTable;
        public int? LanguageBlockLength;
        public int? ECURefCount;
        public int? ECURefTable;
        public int? SessionsCount;
        public int? SessionsTable;
        public int? CFFIsFromDataBase;

        public List<FlashDataBlock> DataBlocks = new List<FlashDataBlock>();
        public List<FlashArea> DescriptionHeaders = new List<FlashArea>();
        public List<FlashSession> Sessions = new List<FlashSession>();
        // DIIAddCBFFile
        /*
            21 bits active
            f [6, 4,4,4,4, 4,4,4,4, 4,4,4,4, 4,4,4,4, 4,4,4,4, 1],
         */
        public FlashHeader(CaesarReader reader)
        {
            reader.BaseStream.Seek(StubHeader.StubHeaderSize, SeekOrigin.Begin);
            CffHeaderSize = reader.ReadInt32();

            BaseAddress = reader.BaseStream.Position;

            ulong bitFlags = reader.ReadUInt32();

            reader.ReadUInt16(); // unused

            FlashName = reader.ReadBitflagStringWithReader(ref bitFlags, BaseAddress);
            CFFTrafoArguments = reader.ReadBitflagStringWithReader(ref bitFlags, BaseAddress);
            NameCTF = reader.ReadBitflagInt32(ref bitFlags);
            DescriptionCTF = reader.ReadBitflagInt32(ref bitFlags);
            FileAuthor = reader.ReadBitflagStringWithReader(ref bitFlags, BaseAddress); // FladenAuthor (flatbread?)
            FileCreationTime = reader.ReadBitflagStringWithReader(ref bitFlags, BaseAddress);
            AuthoringToolVersion = reader.ReadBitflagStringWithReader(ref bitFlags, BaseAddress);
            FTRAFOVersionString = reader.ReadBitflagStringWithReader(ref bitFlags, BaseAddress);
            FTRAFOVersionNumber = reader.ReadBitflagInt32(ref bitFlags);
            CFFVersionString = reader.ReadBitflagStringWithReader(ref bitFlags, BaseAddress);
			
            // parsed
            NumberOfFlashAreas = reader.ReadBitflagInt32(ref bitFlags);
            FlashDescriptionTable = reader.ReadBitflagInt32(ref bitFlags);
			
            // parsed
            DataBlockTableCount = reader.ReadBitflagInt32(ref bitFlags);
            DataBlockRefTable = reader.ReadBitflagInt32(ref bitFlags);
            LanguageHeaderTable = reader.ReadBitflagInt32(ref bitFlags);
            LanguageBlockLength = reader.ReadBitflagInt32(ref bitFlags);
			
            // unparsed
            ECURefCount = reader.ReadBitflagInt32(ref bitFlags);
            ECURefTable = reader.ReadBitflagInt32(ref bitFlags);
			
			// parsed .. but not used
            SessionsCount = reader.ReadBitflagInt32(ref bitFlags);
            SessionsTable = reader.ReadBitflagInt32(ref bitFlags);
			
            CFFIsFromDataBase = reader.ReadBitflagUInt8(ref bitFlags);

            DescriptionHeaders = new List<FlashArea>();
            if (NumberOfFlashAreas != null && FlashDescriptionTable != null)
            {
                for (int flashDescIndex = 0; flashDescIndex < NumberOfFlashAreas; flashDescIndex++)
                {
                    long flashTableEntryAddress = (long)FlashDescriptionTable + BaseAddress + (flashDescIndex * 4);
                    reader.BaseStream.Seek(flashTableEntryAddress, SeekOrigin.Begin);

                    long flashEntryBaseAddress = (long)FlashDescriptionTable + BaseAddress + reader.ReadInt32();
                    FlashArea fdh = new FlashArea(reader, flashEntryBaseAddress);
                    DescriptionHeaders.Add(fdh);
                }
            }

            DataBlocks = new List<FlashDataBlock>();
            if (DataBlockRefTable != null && DataBlockTableCount != null)
            {
                for (int dataBlockIndex = 0; dataBlockIndex < DataBlockTableCount; dataBlockIndex++)
                {
                    long datablockEntryAddress = (long)DataBlockRefTable + BaseAddress + (dataBlockIndex * 4);
                    reader.BaseStream.Seek(datablockEntryAddress, SeekOrigin.Begin);

                    long datablockBaseAddress = (long)DataBlockRefTable + BaseAddress + reader.ReadInt32();
                    FlashDataBlock fdb = new FlashDataBlock(reader, datablockBaseAddress);
                    DataBlocks.Add(fdb);
                }
            }
			
			Sessions = new List<FlashSession>();
            if (SessionsTable != null && SessionsCount != null)
            {
                for (int sessionIndex = 0; sessionIndex < SessionsCount; sessionIndex++)
                {
                    long sessionEntryAddress = (int)SessionsTable + BaseAddress + (sessionIndex * 4);
                    reader.BaseStream.Seek(sessionEntryAddress, SeekOrigin.Begin);

                    long sessionBaseAddress = (int)SessionsTable + BaseAddress + reader.ReadInt32();
                    FlashSession fs = new FlashSession(reader, sessionBaseAddress);
                    Sessions.Add(fs);
                }
            }
        }
    }
}