using Caesar.DSC;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Caesar
{
    public class CFFHeader : CaesarObject
    {
        public int? CaesarVersion;
        public int? GpdVersion;
        [JsonIgnore]
        public CTFHeader CaesarCTFHeader = new CTFHeader();
        public int? StringPoolSize;
        private int? DscOffset;
        private int? DscCount;
        private int? DscEntrySize;
        public CaesarTable<DSCContainer>? DscTable;
        public string? CbfVersionString;
        public string? GpdVersionString;
        public string? XmlString;

        [JsonIgnore]
        public int CffHeaderSize;
        [JsonIgnore]
        public long BaseAddress;


        [JsonIgnore]
        public CaesarTable<ECU> CaesarECUs;

        public long DscBlockOffset;
        private int DscBlockSize;

        [JsonIgnore]
        public byte[] DSCPool = new byte[] { };

        // DIIAddCBFFile

        public CFFHeader() 
        { }

        public CFFHeader(CaesarReader reader, CaesarContainer container) 
        {
            reader.BaseStream.Seek(StubHeader.StubHeaderSize, SeekOrigin.Begin);
            CffHeaderSize = reader.ReadInt32();

            BaseAddress = reader.BaseStream.Position;
            AbsoluteAddress = (int)BaseAddress;
            Bitflags = reader.ReadUInt16();

            CaesarVersion = reader.ReadBitflagInt32(ref Bitflags);
            GpdVersion = reader.ReadBitflagInt32(ref Bitflags);
            CaesarECUs = reader.ReadBitflagSubTableAlt<ECU>(this, container, false) ?? new CaesarTable<ECU>();
            CaesarCTFHeader.Read(reader, this, container);
            StringPoolSize = reader.ReadBitflagInt32(ref Bitflags);
            DscOffset = reader.ReadBitflagInt32(ref Bitflags);
            DscCount = reader.ReadBitflagInt32(ref Bitflags);
            DscEntrySize = reader.ReadBitflagInt32(ref Bitflags);
            if(DscOffset != null && DscCount != null && DscEntrySize != null && StringPoolSize != null)
            {
                var oldPos = AbsoluteAddress;

                AbsoluteAddress = (int)StringPoolSize + CffHeaderSize + 0x414;
                DscTable = new CaesarTable<DSCContainer>()
                {
                    RelativeAddress = (int)DscOffset,
                    EntryCount = (int)DscCount,
                    EntrySize = (int)DscEntrySize,
                    BlockSize = (int)DscEntrySize * DscCount
                };
                DscTable.Read(reader, this, container);
                AbsoluteAddress  = oldPos;
            }

            CbfVersionString = reader.ReadBitflagStringWithReader(ref Bitflags, BaseAddress);
            GpdVersionString = reader.ReadBitflagStringWithReader(ref Bitflags, BaseAddress);
            XmlString = reader.ReadBitflagStringWithReader(ref Bitflags, BaseAddress);

            if (StringPoolSize != null)
            {
                long dataBufferOffsetAfterStrings = (long)StringPoolSize + CffHeaderSize + 0x414;
                if (DscCount != null && DscOffset != null && DscEntrySize != null && DscCount > 0)
                {
                    DscBlockOffset = (long)DscOffset + dataBufferOffsetAfterStrings;
                    DscBlockSize = (int)(DscEntrySize * DscCount);
                    reader.BaseStream.Seek(DscBlockOffset, SeekOrigin.Begin);
                    DSCPool = reader.ReadBytes(DscBlockSize);
                    for (int i  = 0; i < DscCount; i++)
                    {
                        int dscFileOffset = BitConverter.ToInt32(DSCPool, i * 8);
                        int dscFileSize = BitConverter.ToInt32(DSCPool, i * 8 + 4);
                        long oldPos = reader.BaseStream.Position;
                        reader.BaseStream.Seek(dscFileOffset + DscBlockOffset, SeekOrigin.Begin);
                        File.WriteAllBytes("ACM_DSC_" + i + ".CCF", reader.ReadBytes(dscFileSize));
                        reader.BaseStream.Seek(oldPos, SeekOrigin.Begin);
                    }
                }
            }
            CaesarCTFHeader.LoadStrings(reader, CffHeaderSize);

            if (CaesarCTFHeader.CtfLanguages.Count != 0)
            {
                container.Language = CaesarCTFHeader.CtfLanguages.GetObjects()[0];
            }
            else
            {
                throw new NotImplementedException("no idea how to handle missing stringtable");
            }
            CaesarECUs.Read(reader, this, container);

        }

        public void PrintDebug()
        {
            Console.WriteLine($"{nameof(CaesarVersion)} : {CaesarVersion}");
            Console.WriteLine($"{nameof(GpdVersion)} : {GpdVersion}");
            Console.WriteLine($"{nameof(CaesarECUs)} : {CaesarECUs}");
            Console.WriteLine($"{nameof(CaesarCTFHeader)} : {CaesarCTFHeader}");
            Console.WriteLine($"{nameof(StringPoolSize)} : {StringPoolSize} 0x{StringPoolSize:X}");
            
            Console.WriteLine($"{nameof(DscEntrySize)} : {DscEntrySize}");
            Console.WriteLine($"{nameof(CbfVersionString)} : {CbfVersionString}");
            Console.WriteLine($"{nameof(GpdVersionString)} : {GpdVersionString}");

            Console.WriteLine($"{nameof(DscOffset)} : {DscOffset} 0x{DscOffset:X}");
            Console.WriteLine($"{nameof(DscBlockOffset)} : {DscBlockOffset} 0x{DscBlockOffset:X}");
            Console.WriteLine($"{nameof(DscCount)} : {DscCount}");
            Console.WriteLine($"{nameof(DscBlockSize)} : {DscCount}");
        }

        protected override void ReadData(CaesarReader reader, CaesarContainer container)
        {
            throw new NotImplementedException();
        }
    }
}
