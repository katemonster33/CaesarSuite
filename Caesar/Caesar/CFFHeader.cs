using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Caesar
{
    public class CFFHeader
    {
        public int? CaesarVersion;
        public int? GpdVersion;
        public int? EcuCount;
        public int? EcuOffset;
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

            ulong bitFlags = reader.ReadUInt16();

            CaesarVersion = reader.ReadBitflagInt32(ref bitFlags);
            GpdVersion = reader.ReadBitflagInt32(ref bitFlags);
            EcuCount = reader.ReadBitflagInt32(ref bitFlags);
            EcuOffset = reader.ReadBitflagInt32(ref bitFlags);
            CtfOffset = reader.ReadBitflagInt32(ref bitFlags);
            StringPoolSize = reader.ReadBitflagInt32(ref bitFlags);
            DscOffset = reader.ReadBitflagInt32(ref bitFlags);
            DscCount = reader.ReadBitflagInt32(ref bitFlags);
            DscEntrySize = reader.ReadBitflagInt32(ref bitFlags);

            CbfVersionString = reader.ReadBitflagStringWithReader(ref bitFlags, BaseAddress);
            GpdVersionString = reader.ReadBitflagStringWithReader(ref bitFlags, BaseAddress);
            XmlString = reader.ReadBitflagStringWithReader(ref bitFlags, BaseAddress);


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
        }

        public void PrintDebug() 
        {
            Console.WriteLine($"{nameof(CaesarVersion)} : {CaesarVersion}");
            Console.WriteLine($"{nameof(GpdVersion)} : {GpdVersion}");
            Console.WriteLine($"{nameof(EcuCount)} : {EcuCount}");
            Console.WriteLine($"{nameof(EcuOffset)} : {EcuOffset} 0x{EcuOffset:X}");
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
    }
}
