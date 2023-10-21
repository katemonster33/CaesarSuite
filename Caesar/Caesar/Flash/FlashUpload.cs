using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Caesar
{
    public class FlashUpload
    {
        // very similar layout to FlashTable
        // 2,   4, 4, 4, 4,    4, 4, 4, 4,    4, 4, 4, 4,    4
        public int? MeaningCTF;
        public string? UploadKey;
        public int? DescriptionCTF;
        public int? SessionIndex;
        public int? Priority;
        public string? UploadService;
        public string? Qualifier;
        public int? UniqueObjectID;
        public int? AccessCode;


        public int[] FlashClassIndexes = Array.Empty<int>();
        public string[] AllowedECUs = Array.Empty<string>();

        public long BaseAddress;

        public FlashUpload(CaesarReader reader, long baseAddress)
        {
            BaseAddress = baseAddress;
            reader.BaseStream.Seek(baseAddress, SeekOrigin.Begin);

            ulong bitFlags = reader.ReadUInt16();

            MeaningCTF = reader.ReadBitflagInt32(ref bitFlags); // @1
            UploadKey = reader.ReadBitflagStringWithReader(ref bitFlags, BaseAddress); // @2
            DescriptionCTF = reader.ReadBitflagInt32(ref bitFlags); // @3
            SessionIndex = reader.ReadBitflagInt32(ref bitFlags); // @4
            Priority = reader.ReadBitflagInt32(ref bitFlags); // @5

            int? numberOfFlashClasses = reader.ReadBitflagInt32(ref bitFlags); // @6
            int? offsetToFlashClasses = reader.ReadBitflagInt32(ref bitFlags); // @7

            UploadService = reader.ReadBitflagStringWithReader(ref bitFlags, BaseAddress); // @8

            int? numberOfAllowedEcus = reader.ReadBitflagInt32(ref bitFlags); // @9
            int? offsetToAllowedEcus = reader.ReadBitflagInt32(ref bitFlags); // @10

            Qualifier = reader.ReadBitflagStringWithReader(ref bitFlags, BaseAddress); // @11
            UniqueObjectID = reader.ReadBitflagInt32(ref bitFlags); // @12
            AccessCode = reader.ReadBitflagInt32(ref bitFlags); // @13

            if (numberOfAllowedEcus != null && offsetToAllowedEcus != null)
            {
                AllowedECUs = new string[(int)numberOfAllowedEcus];
                for (int ecuIndex = 0; ecuIndex < numberOfAllowedEcus; ecuIndex++)
                {
                    // this assumes that uploads behave like flashtables
                    long ecuRow = (int)offsetToAllowedEcus + BaseAddress + (ecuIndex * 12);
                    reader.BaseStream.Seek(ecuRow, SeekOrigin.Begin);

                    long ecuName = (int)offsetToAllowedEcus + BaseAddress + reader.ReadInt32();
                    reader.BaseStream.Seek(ecuName, SeekOrigin.Begin);
                    AllowedECUs[ecuIndex] = reader.ReadString(Encoding.UTF8);
                }
            }

            if (numberOfFlashClasses != null && offsetToFlashClasses != null)
            {
                FlashClassIndexes = new int[(int)numberOfFlashClasses];
                reader.BaseStream.Seek((int)offsetToFlashClasses + BaseAddress, SeekOrigin.Begin);
                for (int flashClassIndex = 0; flashClassIndex < numberOfFlashClasses; flashClassIndex++)
                {
                    FlashClassIndexes[(int)flashClassIndex] = reader.ReadInt32();
                }
            }
        }
    }
}


