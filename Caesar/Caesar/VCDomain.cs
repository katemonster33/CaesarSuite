﻿using System;
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

        public List<Tuple<string, byte[]>> DefaultData = new List<Tuple<string, byte[]>>();

        public void Restore(CTFLanguage language, ECU parentEcu) 
        {
            if (VCFragments != null)
            {
                foreach (VCFragment fragment in VCFragments.GetObjects())
                {
                    fragment.Restore(parentEcu);
                    fragment.ParentDomain = this;
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

        protected override bool ReadHeader(CaesarReader reader)
        {
            base.ReadHeader(reader);

            int entrySize = reader.ReadInt32();
            uint entryCrc = reader.ReadUInt32();

            return true;
        }

        protected override void ReadData(CaesarReader reader, CaesarContainer container)
        {
            Bitflags = reader.ReadUInt16();

            Qualifier = reader.ReadBitflagStringWithReader(ref Bitflags, AbsoluteAddress);
            Name = reader.ReadBitflagStringRef(ref Bitflags, container);
            Description = reader.ReadBitflagStringRef(ref Bitflags, container);
            ReadServiceName = reader.ReadBitflagStringWithReader(ref Bitflags, AbsoluteAddress);
            WriteServiceName = reader.ReadBitflagStringWithReader(ref Bitflags, AbsoluteAddress);

            VCFragments = reader.ReadBitflagSubTableAlt<VCFragment>(this, container);
            DumpSize = reader.ReadBitflagInt32(ref Bitflags);
            DefaultStringCount = reader.ReadBitflagInt32(ref Bitflags);
            StringTableOffset = reader.ReadBitflagInt32(ref Bitflags);
            Unk1 = reader.ReadBitflagInt16(ref Bitflags);

            // PrintDebug();

            DefaultData = new List<Tuple<string, byte[]>>();
            if (DefaultStringCount != null && StringTableOffset != null)
            {
                long stringTableBaseAddress = (long)StringTableOffset + AbsoluteAddress;
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
                    CaesarStringReference? valueType_T = reader.ReadBitflagStringRef(ref strBitflags, container);
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

                    if (valueType_T != null && valueType_T.Text != null)// this value is almost always "default"; can probably let the hardcoded string pass
                    {
                        DefaultData.Add(new Tuple<string, byte[]>(valueType_T.Text, blob));
                    }
                    //Console.WriteLine($"Blob: {BitUtility.BytesToHex(blob)} @ {valueType}");
                    //Console.WriteLine($"String base address: 0x{stringBaseAddress:X}");
                }
            }
        }
    }
}
