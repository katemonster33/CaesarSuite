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
    public class FlashArea
    {
        public string? Qualifier;
        public int? Description;
        public int? FlashAreaName;
        public int? UniqueObjectID;

        public long BaseAddress;

        public FlashTable[] FlashTables = Array.Empty<FlashTable>();
        public FlashUpload[] FlashUploads = Array.Empty<FlashUpload>();
        public FlashIdent[] FlashIdents = Array.Empty<FlashIdent>();
        public FlashClass[] FlashClasses = Array.Empty<FlashClass>();

        public FlashArea(CaesarReader reader, long baseAddress) 
        {
            BaseAddress = baseAddress;

            reader.BaseStream.Seek(baseAddress, SeekOrigin.Begin);
            ulong flashBitFlags = reader.ReadUInt32();

            Qualifier = reader.ReadBitflagStringWithReader(ref flashBitFlags, baseAddress);
            Description = reader.ReadBitflagInt32(ref flashBitFlags);
            FlashAreaName = reader.ReadBitflagInt32(ref flashBitFlags);

            int? flashTableCount = reader.ReadBitflagInt32(ref flashBitFlags);
            int? flashTableOffset = reader.ReadBitflagInt32(ref flashBitFlags);

            int? flashUploadCount = reader.ReadBitflagInt32(ref flashBitFlags);
            int? flashUploadOffset = reader.ReadBitflagInt32(ref flashBitFlags);

            int? identCount = reader.ReadBitflagInt32(ref flashBitFlags);
            int? identsOffset = reader.ReadBitflagInt32(ref flashBitFlags);

            UniqueObjectID = reader.ReadBitflagInt32(ref flashBitFlags);

            int? flashClassCount = reader.ReadBitflagInt32(ref flashBitFlags);
            int? flashClassOffset = reader.ReadBitflagInt32(ref flashBitFlags);

            if (flashTableCount != null && flashTableOffset != null)
            {
                // ft
                FlashTables = new FlashTable[(int)flashTableCount];
                for (int flashTableIndex = 0; flashTableIndex < flashTableCount; flashTableIndex++)
                {
                    long flashTableEntryAddress = (int)flashTableOffset + BaseAddress + (flashTableIndex * 4);
                    reader.BaseStream.Seek(flashTableEntryAddress, SeekOrigin.Begin);

                    long flashEntryBaseAddress = (int)flashTableOffset + BaseAddress + reader.ReadInt32();
                    FlashTables[flashTableIndex] = new FlashTable(reader, flashEntryBaseAddress);
                }
            }
            if (flashTableCount != null && flashTableOffset != null)
            {
                // uploads
                FlashUploads = new FlashUpload[(int)flashUploadCount];
                for (int flashUploadIndex = 0; flashUploadIndex < flashUploadCount; flashUploadIndex++)
                {
                    long flashUploadEntryAddress = (int)flashUploadOffset + BaseAddress + (flashUploadIndex * 4);
                    reader.BaseStream.Seek(flashUploadEntryAddress, SeekOrigin.Begin);

                    long uploadBaseAddress = (int)flashUploadOffset + BaseAddress + reader.ReadInt32();
                    FlashUploads[flashUploadIndex] = new FlashUpload(reader, uploadBaseAddress);
                }
            }
            if (identCount != null && identsOffset != null)
            {
                // idents
                FlashIdents = new FlashIdent[(int)identCount];
                for (int identIndex = 0; identIndex < identCount; identIndex++)
                {
                    long identEntryAddress = (int)identsOffset + BaseAddress + (identIndex * 4);
                    reader.BaseStream.Seek(identEntryAddress, SeekOrigin.Begin);

                    long identBaseAddress = (int)identsOffset + BaseAddress + reader.ReadInt32();
                    FlashIdents[(int)identIndex] = new FlashIdent(reader, identBaseAddress);
                }
            }

            if (flashClassCount != null && flashClassOffset != null)
            {
                // classes
                FlashClasses = new FlashClass[(int)flashClassCount];
                for (int flashClassIndex = 0; flashClassIndex < flashClassCount; flashClassIndex++)
                {
                    long flashClassEntryAddress = (int)flashClassOffset + BaseAddress + (flashClassIndex * 4);
                    reader.BaseStream.Seek(flashClassEntryAddress, SeekOrigin.Begin);

                    long flashClassBaseAddress = (int)flashClassOffset + BaseAddress + reader.ReadInt32();
                    FlashClasses[flashClassIndex] = new FlashClass(reader, flashClassBaseAddress); ;
                }
            }
        }
    }
}
