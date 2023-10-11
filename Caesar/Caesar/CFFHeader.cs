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
        public int? CtfOffset; // nCtfHeaderRpos
        public int? StringPoolSize;
        private int? DscOffset;
        private int? DscCount;
        private int? DscEntrySize;
        public string? CbfVersionString;
        public string? GpdVersionString;
        public string? XmlString;

        [Newtonsoft.Json.JsonIgnore]
        public int CffHeaderSize;
        [Newtonsoft.Json.JsonIgnore]
        public long BaseAddress;

        public CTFHeader CaesarCTFHeader = new CTFHeader();

        public CaesarTable<ECU> CaesarECUs;

        public long DscBlockOffset;
        private int DscBlockSize;

        [Newtonsoft.Json.JsonIgnore]
        public byte[] DSCPool = new byte[] { };

        // DIIAddCBFFile

        public CFFHeader() 
        { }

        public CFFHeader(CaesarReader reader) 
        {
            reader.BaseStream.Seek(StubHeader.StubHeaderSize, SeekOrigin.Begin);
            CffHeaderSize = reader.ReadInt32();

            BaseAddress = reader.BaseStream.Position;
            AbsoluteAddress = (int)BaseAddress;
            Bitflags = reader.ReadUInt16();

            CaesarVersion = reader.ReadBitflagInt32(ref Bitflags);
            GpdVersion = reader.ReadBitflagInt32(ref Bitflags);
            CaesarECUs = reader.ReadBitflagSubTableAlt<ECU>(this, new CTFLanguage(), null, false) ?? new CaesarTable<ECU>();
            CaesarCTFHeader.Read(reader, this, new CTFLanguage(), null);
            StringPoolSize = reader.ReadBitflagInt32(ref Bitflags);
            DscOffset = reader.ReadBitflagInt32(ref Bitflags);
            DscCount = reader.ReadBitflagInt32(ref Bitflags);
            DscEntrySize = reader.ReadBitflagInt32(ref Bitflags);

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
                }
            }
            CaesarECUs.Read(reader, this, CaesarCTFHeader.CtfLanguages.Count > 0 ? CaesarCTFHeader.CtfLanguages.GetObjects()[0] : new CTFLanguage(), null);

            CaesarCTFHeader.LoadStrings(reader, CffHeaderSize);
        }

        public void PrintDebug()
        {
            Console.WriteLine($"{nameof(CaesarVersion)} : {CaesarVersion}");
            Console.WriteLine($"{nameof(GpdVersion)} : {GpdVersion}");
            Console.WriteLine($"{nameof(CaesarECUs)} : {CaesarECUs}");
            Console.WriteLine($"{nameof(CtfOffset)} : 0x{CtfOffset:X}");
            Console.WriteLine($"{nameof(StringPoolSize)} : {StringPoolSize} 0x{StringPoolSize:X}");
            
            Console.WriteLine($"{nameof(DscEntrySize)} : {DscEntrySize}");
            Console.WriteLine($"{nameof(CbfVersionString)} : {CbfVersionString}");
            Console.WriteLine($"{nameof(GpdVersionString)} : {GpdVersionString}");

            Console.WriteLine($"{nameof(DscOffset)} : {DscOffset} 0x{DscOffset:X}");
            Console.WriteLine($"{nameof(DscBlockOffset)} : {DscBlockOffset} 0x{DscBlockOffset:X}");
            Console.WriteLine($"{nameof(DscCount)} : {DscCount}");
            Console.WriteLine($"{nameof(DscBlockSize)} : {DscCount}");
        }

        protected override void ReadData(CaesarReader reader, CTFLanguage language, ECU? currentEcu)
        {
            throw new NotImplementedException();
        }
    }
}
