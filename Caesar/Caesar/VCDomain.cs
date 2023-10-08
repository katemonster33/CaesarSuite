using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Caesar
{
    public class VCDomain
    {

        public string? Qualifier;
        public int? Name_CTF;
        public int? Description_CTF;
        public string? ReadServiceName;
        public string? WriteServiceName;
        private int? FragmentCount;
        private int? FragmentTableOffset;
        public int? DumpSize;
        private int? DefaultStringCount; // exposed as DefaultData.Count
        private int? StringTableOffset; // exposed as DefaultData
        public int? Unk1;

        public List<VCFragment> VCFragments = new List<VCFragment>();
        [Newtonsoft.Json.JsonIgnore]
        public ECU ParentECU;

        public List<Tuple<string, byte[]>> DefaultData = new List<Tuple<string, byte[]>>();

        public long BaseAddress;
        public int Index;

        public void Restore(CTFLanguage language, ECU parentEcu) 
        {
            ParentECU = parentEcu;
            foreach (VCFragment fragment in VCFragments) 
            {
                fragment.Restore(parentEcu, this, language);
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
            ulong bitflags = reader.ReadUInt16();

            Qualifier = reader.ReadBitflagStringWithReader(ref bitflags, baseAddress);
            Name_CTF = reader.ReadBitflagInt32(ref bitflags);
            Description_CTF = reader.ReadBitflagInt32(ref bitflags);
            ReadServiceName = reader.ReadBitflagStringWithReader(ref bitflags, baseAddress);
            WriteServiceName = reader.ReadBitflagStringWithReader(ref bitflags, baseAddress);
            FragmentCount = reader.ReadBitflagInt32(ref bitflags);
            FragmentTableOffset = reader.ReadBitflagInt32(ref bitflags) + (int)baseAddress; // demoting long (warning)
            DumpSize = reader.ReadBitflagInt32(ref bitflags);
            DefaultStringCount = reader.ReadBitflagInt32(ref bitflags);
            StringTableOffset = reader.ReadBitflagInt32(ref bitflags);
            Unk1 = reader.ReadBitflagInt16(ref bitflags);

            // PrintDebug();

            VCFragments = new List<VCFragment>();
            if (FragmentCount != null && FragmentTableOffset != null)
            {
                for (int fragmentIndex = 0; fragmentIndex < FragmentCount; fragmentIndex++)
                {
                    VCFragment fragment = new VCFragment(reader, this, (long)FragmentTableOffset, fragmentIndex, language, parentEcu);
                    VCFragments.Add(fragment);
                }
            }
            // ValidateFragmentCoverage();

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
            if (DumpSize != null)
            {
                int bitCursor = 0;
                int expectedLengthInBits = (int)DumpSize * 8;
                List<VCFragment> fragments = new List<VCFragment>(VCFragments);
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
            Console.WriteLine($"{nameof(Name_CTF)} : {Name_CTF}");
            Console.WriteLine($"{nameof(Description_CTF)} : {Description_CTF}");
            Console.WriteLine($"{nameof(ReadServiceName)} : {ReadServiceName}");
            Console.WriteLine($"{nameof(WriteServiceName)} : {WriteServiceName}");

            Console.WriteLine($"{nameof(FragmentCount)} : {FragmentCount}");
            Console.WriteLine($"{nameof(FragmentTableOffset)} : 0x{FragmentTableOffset:X}");
            Console.WriteLine($"{nameof(DumpSize)} : {DumpSize}");
            Console.WriteLine($"{nameof(DefaultStringCount)} : {DefaultStringCount}");
            Console.WriteLine($"{nameof(StringTableOffset)} : {StringTableOffset}");
            Console.WriteLine($"{nameof(Unk1)} : {Unk1}");

        }
    }
}
