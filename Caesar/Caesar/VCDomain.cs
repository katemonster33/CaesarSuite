using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Caesar
{
    public class VCDomain : CaesarObject
    {

        public string? Qualifier;
        public CaesarStringReference? Name;
        public CaesarStringReference? Description;
        public string? ReadServiceName;
        public string? WriteServiceName;

        public CaesarTable<VCFragment>? VCFragments;

        public int? DumpSize;
        private int? DefaultStringCount; // exposed as DefaultData.Count
        private int? StringTableOffset; // exposed as DefaultData
        public int? Unk1;

        [Newtonsoft.Json.JsonIgnore]
        public ECU ParentECU;

        public List<Tuple<string, byte[]>> DefaultData = new List<Tuple<string, byte[]>>();

        public long BaseAddress;
        public int Index;

        public void Restore(CTFLanguage language, ECU parentEcu) 
        {
            ParentECU = parentEcu;
            if (VCFragments != null)
            {
                foreach (VCFragment fragment in VCFragments.GetObjects())
                {
                    fragment.ParentDomain = this;
                }
            }
        }

        public VCDomain() 
        {
            ParentECU = new ECU();
        }

        // VCDomain(reader, language, vcdBlockAddress, vcdIndex, this);
        public VCDomain(CaesarReader reader, CTFLanguage language, long baseAddress, int variantCodingDomainEntry, ECU parentEcu)
        {
            ParentECU = parentEcu;
            BaseAddress = baseAddress;
            Index = variantCodingDomainEntry;
            AbsoluteAddress = (int)BaseAddress;

            /*
            byte[] variantCodingPool = parentEcu.ReadVarcodingPool(reader);
            using (BinaryReader poolReader = new BinaryReader(new MemoryStream(variantCodingPool)))
            {
                poolReader.BaseStream.Seek(variantCodingDomainEntry * parentEcu.VcDomain_EntrySize, SeekOrigin.Begin);
                int entryOffset = poolReader.ReadInt32();
                int entrySize = poolReader.ReadInt32();
                uint entryCrc = poolReader.ReadUInt32();
                long vcdBlockAddress = entryOffset + parentEcu.VcDomain_BlockOffset;
            }
            // Console.WriteLine($"VCD Entry @ 0x{entryOffset:X} with size 0x{entrySize:X} and CRC {entryCrc:X8}, abs addr {vcdBlockAddress:X8}");

            long baseAddress = vcdBlockAddress;
            */
            reader.BaseStream.Seek(baseAddress, SeekOrigin.Begin);
            Bitflags = reader.ReadUInt16();

            Qualifier = reader.ReadBitflagStringWithReader(ref Bitflags, baseAddress);
            Name = reader.ReadBitflagStringRef(ref Bitflags, language);
            Description = reader.ReadBitflagStringRef(ref Bitflags, language);
            ReadServiceName = reader.ReadBitflagStringWithReader(ref Bitflags, baseAddress);
            WriteServiceName = reader.ReadBitflagStringWithReader(ref Bitflags, baseAddress);

            VCFragments = reader.ReadBitflagSubTableAlt<VCFragment>(this, language, parentEcu);
            DumpSize = reader.ReadBitflagInt32(ref Bitflags);
            DefaultStringCount = reader.ReadBitflagInt32(ref Bitflags);
            StringTableOffset = reader.ReadBitflagInt32(ref Bitflags);
            Unk1 = reader.ReadBitflagInt16(ref Bitflags);

            // PrintDebug();

            DefaultData = new List<Tuple<string, byte[]>>();
            if (DefaultStringCount != null && StringTableOffset != null)
            {
                long stringTableBaseAddress = (long)StringTableOffset + baseAddress;
                // this could almost be a class of its own but there isn't a distinct name to it
                for (int stringTableIndex = 0; stringTableIndex < DefaultStringCount; stringTableIndex++)
                {
                    reader.BaseStream.Seek(stringTableBaseAddress + (4 * stringTableIndex), SeekOrigin.Begin);
                    int offset = reader.ReadInt32();
                    long stringBaseAddress = stringTableBaseAddress + offset;
                    reader.BaseStream.Seek(stringBaseAddress, SeekOrigin.Begin);
                    ulong strBitflags = reader.ReadUInt16();
                    int? nameUsuallyAbsent_T = reader.ReadBitflagInt32(ref strBitflags);
                    int? offsetToBlob = reader.ReadBitflagInt32(ref strBitflags);
                    int? blobSize = reader.ReadBitflagInt32(ref strBitflags);
                    int? valueType_T = reader.ReadBitflagInt32(ref strBitflags);
                    string? noIdeaStr1 = reader.ReadBitflagStringWithReader(ref strBitflags, stringBaseAddress);
                    int? noIdea2_T = reader.ReadBitflagInt32(ref strBitflags);
                    int? noIdea3 = reader.ReadBitflagInt16(ref strBitflags);
                    string? noIdeaStr2 = reader.ReadBitflagStringWithReader(ref strBitflags, stringBaseAddress);
                    byte[] blob = new byte[] { };
                    if (blobSize != null && offsetToBlob != null)
                    {
                        long blobFileAddress = stringBaseAddress + (long)offsetToBlob;
                        reader.BaseStream.Seek(blobFileAddress, SeekOrigin.Begin);
                        blob = reader.ReadBytes((int)blobSize);
                        // memcpy
                    }

                    string? valueType = language.GetString(valueType_T); // this value is almost always "default"; can probably let the hardcoded string pass
                    if(valueType != null)
                    {
                        DefaultData.Add(new Tuple<string, byte[]>(valueType, blob));
                    }
                    //Console.WriteLine($"Blob: {BitUtility.BytesToHex(blob)} @ {valueType}");
                    //Console.WriteLine($"String base address: 0x{stringBaseAddress:X}");
                }
            }


        }

        private void ValidateFragmentCoverage()
        {
            // apparently gaps are okay, there isn't a 100% way to find out if parsing errors have snuck through
            if (DumpSize != null && VCFragments != null)
            {
                int bitCursor = 0;
                int expectedLengthInBits = (int)DumpSize * 8;
                List<VCFragment> fragments = new List<VCFragment>(VCFragments.GetObjects());
                List<int> bitGapPositions = new List<int>();

                while (fragments.Count > 0)
                {
                    VCFragment? result = fragments.Find(x => x.ByteBitPos == bitCursor);
                    if (result == null)
                    {
                        bitGapPositions.Add(bitCursor);
                        bitCursor++;
                        if (bitCursor > expectedLengthInBits)
                        {
                            throw new Exception("wtf");
                        }
                    }
                    else
                    {
                        bitCursor += result.BitLength;
                        fragments.Remove(result);
                    }
                }
            }
        }

        public void PrintDebug() 
        {

            Console.WriteLine($"VCD Name: {Qualifier}");
            Console.WriteLine($"{nameof(Name)} : {Name}");
            Console.WriteLine($"{nameof(Description)} : {Description}");
            Console.WriteLine($"{nameof(ReadServiceName)} : {ReadServiceName}");
            Console.WriteLine($"{nameof(WriteServiceName)} : {WriteServiceName}");

            Console.WriteLine($"{nameof(VCFragments)} : {VCFragments}");
            Console.WriteLine($"{nameof(DumpSize)} : {DumpSize}");
            Console.WriteLine($"{nameof(DefaultStringCount)} : {DefaultStringCount}");
            Console.WriteLine($"{nameof(StringTableOffset)} : {StringTableOffset}");
            Console.WriteLine($"{nameof(Unk1)} : {Unk1}");

        }

        protected override void ReadData(CaesarReader reader, CTFLanguage language, ECU? currentEcu)
        {
            throw new NotImplementedException();
        }
    }
}
