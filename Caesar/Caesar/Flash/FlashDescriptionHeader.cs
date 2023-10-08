using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Caesar
{
    // FLASH_DESCRIPTION_HEADER
    // 0x10: 4 :  4, 4, 4, 4,  4, 4, 4, 4,  4, 4, 4, 4
    public class FlashDescriptionHeader
    {
        public string? Qualifier;
        public int? Description;
        public int? FlashAreaName;
        public int? FlashTableStructureCount;
        public int? FlashTableStructureOffset;
        public int? NumberOfUploads;
        public int? UploadTableRefTable;
        public int? NumberOfIdentServices;
        public int? IdentServicesOffset;
        public int? UniqueObjectID;
        public int? unkb;
        public int? unkc;
        public long BaseAddress;

        public FlashDescriptionHeader(CaesarReader reader, long baseAddress) 
        {
            BaseAddress = baseAddress;

            reader.BaseStream.Seek(baseAddress, SeekOrigin.Begin);
            ulong flashBitFlags = reader.ReadUInt32();

            Qualifier = reader.ReadBitflagStringWithReader(ref flashBitFlags, baseAddress);
            Description = reader.ReadBitflagInt32(ref flashBitFlags);
            FlashAreaName = reader.ReadBitflagInt32(ref flashBitFlags);
            FlashTableStructureCount = reader.ReadBitflagInt32(ref flashBitFlags);
            FlashTableStructureOffset = reader.ReadBitflagInt32(ref flashBitFlags);
            NumberOfUploads = reader.ReadBitflagInt32(ref flashBitFlags);
            UploadTableRefTable = reader.ReadBitflagInt32(ref flashBitFlags);
            NumberOfIdentServices = reader.ReadBitflagInt32(ref flashBitFlags);
            IdentServicesOffset = reader.ReadBitflagInt32(ref flashBitFlags);
            UniqueObjectID = reader.ReadBitflagInt32(ref flashBitFlags);
            unkb = reader.ReadBitflagInt32(ref flashBitFlags);
            unkc = reader.ReadBitflagInt32(ref flashBitFlags);
        }

        public void PrintDebug()
        {
            Console.WriteLine($"{nameof(Qualifier)} : {Qualifier}");
            Console.WriteLine($"{nameof(Description)} : {Description}");
            Console.WriteLine($"{nameof(FlashAreaName)} : {FlashAreaName}");
            Console.WriteLine($"{nameof(FlashTableStructureCount)} : {FlashTableStructureCount}");

            Console.WriteLine($"{nameof(FlashTableStructureOffset)} : 0x{FlashTableStructureOffset:X}");
            Console.WriteLine($"{nameof(NumberOfUploads)} : {NumberOfUploads}");
            Console.WriteLine($"{nameof(UploadTableRefTable)} : {UploadTableRefTable}");
            Console.WriteLine($"{nameof(NumberOfIdentServices)} : {NumberOfIdentServices}");

            Console.WriteLine($"{nameof(IdentServicesOffset)} : {IdentServicesOffset}");
            Console.WriteLine($"{nameof(UniqueObjectID)} : {UniqueObjectID}");
            Console.WriteLine($"{nameof(unkb)} : {unkb}");
            Console.WriteLine($"{nameof(unkc)} : {unkc}");
        }
    }
}
