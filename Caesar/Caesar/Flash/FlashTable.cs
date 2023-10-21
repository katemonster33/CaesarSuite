using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Caesar
{
    public class FlashTable
    {
        public int? MeaningCTF;
        public string? FlashKey;
        public int? FlashDescriptionCTF;
        public int? SessionIndex;
        public int? Priority;
        public int[] FlashClassIndexes = Array.Empty<int>();
        public string? FlashService;
        public string? Qualifier;
        public int? UniqueObjectId;
        public int? AccessCode;
        public string[] AllowedECUs = Array.Empty<string>();

        public long BaseAddress;
        // confirmed as: 2,   4, 4, 4, 4,  4, 4, 4, 4,  4, 4, 4, 4,  4

        public FlashTable(CaesarReader reader, long baseAddress)
        {
            BaseAddress = baseAddress;
            reader.BaseStream.Seek(baseAddress, SeekOrigin.Begin);

            ulong bitFlags = reader.ReadUInt16();

            MeaningCTF = reader.ReadBitflagInt32(ref bitFlags); // 6
            FlashKey = reader.ReadBitflagStringWithReader(ref bitFlags, BaseAddress); // 2E
            FlashDescriptionCTF = reader.ReadBitflagInt32(ref bitFlags); // 7
            SessionIndex = reader.ReadBitflagInt32(ref bitFlags); // 0
            Priority = reader.ReadBitflagInt32(ref bitFlags); // 1E

            int numberOfFlashClasses = reader.ReadBitflagInt32(ref bitFlags) ?? 0; // 01
            int? offsetToFlashClasses = reader.ReadBitflagInt32(ref bitFlags); // 3E

            FlashService = reader.ReadBitflagStringWithReader(ref bitFlags, BaseAddress); // 42

            int? numberOfAllowedEcus = reader.ReadBitflagInt32(ref bitFlags); // 01
            int? offsetToAllowedEcus = reader.ReadBitflagInt32(ref bitFlags); // 50

            Qualifier = reader.ReadBitflagStringWithReader(ref bitFlags, BaseAddress); // 64

            UniqueObjectId = reader.ReadBitflagInt32(ref bitFlags);
            AccessCode = reader.ReadBitflagInt32(ref bitFlags); // ref to table addr? not seen yet

            if (numberOfAllowedEcus != null && offsetToAllowedEcus != null)
            {
                AllowedECUs = new string[(int)numberOfAllowedEcus];
                for (int ecuIndex = 0; ecuIndex < numberOfAllowedEcus; ecuIndex++)
                {
                    // stride size confirmed as 12 in DIGetFlashTableAllowedECUByIndex
                    long ecuRow = (int)offsetToAllowedEcus + BaseAddress + (ecuIndex * 12);
                    reader.BaseStream.Seek(ecuRow, SeekOrigin.Begin);

                    long ecuName = (int)offsetToAllowedEcus + BaseAddress + reader.ReadInt32();
                    reader.BaseStream.Seek(ecuName, SeekOrigin.Begin);
                    AllowedECUs[ecuIndex] = reader.ReadString(Encoding.UTF8);
                }
            }

            if (offsetToFlashClasses != null)
            {
                FlashClassIndexes = new int[numberOfFlashClasses];
                reader.BaseStream.Seek((long)offsetToFlashClasses + BaseAddress, SeekOrigin.Begin);
                for (int flashClassIndex = 0; flashClassIndex < numberOfFlashClasses; flashClassIndex++)
                {
                    FlashClassIndexes[flashClassIndex] = reader.ReadInt32();
                }
            }
        }
    }
}

