using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Caesar
{
    public class VCFragment : CaesarObject
    {
        public int ByteBitPos;
        public ushort ImplementationType;

        public CaesarStringReference? Name;
        public CaesarStringReference? Description;
        public int? ReadAccessLevel;
        public int? WriteAccessLevel;
        public int? ByteOrder;
        public int? RawBitLength;
        public int? IttOffset;
        public int? InfoPoolIndex;
        public int? MeaningB;
        public int? MeaningC;
        public int? CCFHandle;
        public int? VarcodeDumpSize;
        public byte[]? VarcodeDump;
        public CaesarBasicTable<VCSubfragment>? Subfragments;
        public string? Qualifier;

        public ushort ImplementationUpper;
        public ushort ImplementationLower;

        public int BitLength;


        [Newtonsoft.Json.JsonIgnore]
        private static readonly byte[] FragmentLengthTable = new byte[] { 0, 1, 4, 8, 0x10, 0x20, 0x40 };
        [Newtonsoft.Json.JsonIgnore]
        public VCDomain ParentDomain;
        [Newtonsoft.Json.JsonIgnore]
        public ECU ParentECU;

        public void Restore(ECU parentEcu, VCDomain parentDomain, CTFLanguage language) 
        {
            ParentECU = parentEcu;
            ParentDomain = parentDomain;
        }

        public VCFragment() 
        {
            ParentDomain = new VCDomain();
            ParentECU = new ECU();
        }

        public VCFragment(CaesarReader reader, VCDomain parentDomain, long fragmentTable, int fragmentIndex, CTFLanguage language, ECU parentEcu) 
        {
            // see DIOpenVarCodeFrag
            ParentDomain = parentDomain;
            ParentECU = parentEcu;

            long fragmentTableEntry = fragmentTable + (10 * fragmentIndex);
            reader.BaseStream.Seek(fragmentTableEntry, SeekOrigin.Begin);
            // no bitflag required for 10-byte table entry since it is mandatory
            int fragmentNewBaseOffset = reader.ReadInt32();

            ByteBitPos = reader.ReadInt32();
            ImplementationType = reader.ReadUInt16();

            // Console.WriteLine($"Fragment new base @ 0x{fragmentNewBaseOffset:X}, byteBitPos 0x{fragmentByteBitPos:X}, implementationType: 0x{implementationType:X}");
            long fragmentBaseAddress = fragmentTable + fragmentNewBaseOffset;
            reader.BaseStream.Seek(fragmentBaseAddress, SeekOrigin.Begin);
            Bitflags = reader.ReadUInt32();
            // Console.WriteLine($"Fragment new bitflag @ 0x{fragmentBitflags:X}");

            Name = reader.ReadBitflagStringRef(ref Bitflags, language);
            Description = reader.ReadBitflagStringRef(ref Bitflags, language);
            ReadAccessLevel = reader.ReadBitflagUInt8(ref Bitflags);
            WriteAccessLevel = reader.ReadBitflagUInt8(ref Bitflags);
            ByteOrder = reader.ReadBitflagUInt16(ref Bitflags);
            RawBitLength = reader.ReadBitflagInt32(ref Bitflags);
            IttOffset = reader.ReadBitflagInt32(ref Bitflags);
            InfoPoolIndex = reader.ReadBitflagInt32(ref Bitflags);
            MeaningB = reader.ReadBitflagInt32(ref Bitflags);
            MeaningC = reader.ReadBitflagInt32(ref Bitflags);
            CCFHandle = reader.ReadBitflagInt16(ref Bitflags);
            VarcodeDumpSize = reader.ReadBitflagInt32(ref Bitflags);
            VarcodeDump = reader.ReadBitflagDumpWithReader(ref Bitflags, VarcodeDumpSize, fragmentBaseAddress);
            AbsoluteAddress = (int)fragmentBaseAddress;
            Subfragments = reader.ReadBitflagSubTable<VCSubfragment>(this, language, parentEcu);
            Qualifier = reader.ReadBitflagStringWithReader(ref Bitflags, fragmentBaseAddress);

            // Console.WriteLine($"{nameof(fragmentName)} : {fragmentName}, child {fragmentNoOfSubFragments} @ 0x{fragmentSubfragmentFileOffset:X} base {fragmentBaseAddress:X}");

            
            if (ByteOrder  != null && ByteOrder != 0 && BitLength > 0)
            {
                //throw new Exception("Currently assumes everything is little-endian");
                Console.WriteLine($"WARNING: {Qualifier} (Size: {BitLength}) has an unsupported byte order. Please proceed with caution");
                //PrintDebug(true);
            }
            // PrintDebug();
            // Console.WriteLine($"implementation-default : {implementationType:X4} upper: {(implementationType & 0xFF0):X4} lower: {(implementationType & 0xF):X4}");
            FindFragmentSize(reader);
        }

        public VCSubfragment? GetSubfragmentConfiguration(byte[] variantCodingValue)
        {
            if (Subfragments != null)
            {
                byte[] variantBits = BitUtility.ByteArrayToBitArray(variantCodingValue);
                byte[] affectedBits = variantBits.Skip(ByteBitPos).Take(BitLength).ToArray();

                foreach (VCSubfragment subfragment in Subfragments.GetObjects())
                {
                    if (subfragment.Dump != null)
                    {
                        byte[] sfToCompare = BitUtility.ByteArrayToBitArray(subfragment.Dump).Take(BitLength).ToArray();
                        if (sfToCompare.SequenceEqual(affectedBits))
                        {
                            return subfragment;
                        }
                    }
                }
            }
            return null;
        }
        public byte[] SetSubfragmentConfiguration(byte[] variantCodingValue, string subfragmentName)
        {
            if (Subfragments != null)
            {
                foreach (VCSubfragment subfragment in Subfragments.GetObjects())
                {
                    if (subfragment.Name?.Text == subfragmentName)
                    {
                        return SetSubfragmentConfiguration(variantCodingValue, subfragment);
                    }
                }
            }
            throw new FormatException($"Requested subfragment {subfragmentName} could not be found in {Qualifier}");
        }

        public byte[] SetSubfragmentConfiguration(byte[] variantCodingValue, VCSubfragment subfragment)
        {
            byte[] variantBits = BitUtility.ByteArrayToBitArray(variantCodingValue);
            List<byte> result = new List<byte>(variantBits.Take(ByteBitPos));
            variantBits = variantBits.Skip(BitLength + ByteBitPos).ToArray();
            if (subfragment.Dump != null)
            {
                byte[] sfToSet = BitUtility.ByteArrayToBitArray(subfragment.Dump).Take(BitLength).ToArray();
                result.AddRange(sfToSet);
            }
            result.AddRange(variantBits);
            return BitUtility.BitArrayToByteArray(result.ToArray());
        }

        private void FindFragmentSize(BinaryReader reader)
        {
            ImplementationUpper = (ushort)(ImplementationType & 0xFF0);
            ImplementationLower = (ushort)(ImplementationType & 0xF);
            BitLength = 0;

            // fixup the bit length
            if (ImplementationLower > 6)
            {
                throw new NotImplementedException("The disassembly throws an exception when fragmentImplementationLower > 6, copying verbatim");
            }

            if (ImplementationUpper > 0x420 && InfoPoolIndex != null && ParentECU.GlobalInternalPresentations != null)
            {
                // Console.WriteLine($"fragment value upper: {fragmentImplementationUpper:X}");

                DiagPresentation pres = ParentECU.GlobalInternalPresentations.GetObjects()[(int)InfoPoolIndex];
                /*
                // depreciate use of ReadCBFWithOffset
                poolReader.BaseStream.Seek(ecu.Info_EntrySize * InfoPoolIndex, SeekOrigin.Begin);
                int presentationStructOffset = poolReader.ReadInt32();
                int presentationStructSize = poolReader.ReadInt32();

                //Console.WriteLine($"struct offset: 0x{presentationStructOffset:X} , size: {presentationStructSize} , meaningA 0x{fragmentMeaningA_Presentation:X} infoBase 0x{ecu.ecuInfoPool_fileoffset_7:X}\n");

                reader.BaseStream.Seek(presentationStructOffset + ecu.Info_BlockOffset, SeekOrigin.Begin);
                byte[] presentationStruct = reader.ReadBytes(presentationStructSize);

                int presentationMode = CaesarStructure.ReadCBFWithOffset(0x1C, CaesarStructure.StructureName.PRESENTATION_STRUCTURE, presentationStruct); // PRESS_Type
                int presentationLength = CaesarStructure.ReadCBFWithOffset(0x1A, CaesarStructure.StructureName.PRESENTATION_STRUCTURE, presentationStruct); // PRESS_TypeLength
                if (presentationLength > 0)
                {
                    BitLength = presentationLength;
                }
                else 
                {
                    BitLength = CaesarStructure.ReadCBFWithOffset(0x21, CaesarStructure.StructureName.PRESENTATION_STRUCTURE, presentationStruct); // ???
                }
                */
                if (pres.TypeLength_1A != null && pres.TypeLengthBytesMaybe_21 != null && pres.Type_1C != null)
                {
                    BitLength = (int)(pres.TypeLength_1A > 0 ? pres.TypeLength_1A : pres.TypeLengthBytesMaybe_21);
                    // if value was specified in bytes, convert to bits
                    if (pres.Type_1C == 0)
                    {
                        BitLength *= 8;
                    }
                }
            }
            else
            {
                if (ImplementationUpper == 0x420)
                {
                    BitLength = FragmentLengthTable[ImplementationLower];
                }
                else if (ImplementationUpper == 0x320)
                {
                    BitLength = FragmentLengthTable[ImplementationLower];
                }
                else if (ImplementationUpper == 0x330 && RawBitLength != null)
                {
                    BitLength = (int)RawBitLength;
                }
                else if (ImplementationUpper == 0x340)
                {
                    //throw new NotImplementedException("Requires implementation of ITT handle");
                    Console.WriteLine($"[!] Warning: Please avoid {ParentDomain.Qualifier} -> {Qualifier} as it could not be parsed (requires ITT).");
                }
                else
                {
                    throw new NotImplementedException($"No known fragment length format. Fragment upper: 0x{ImplementationUpper:X}");
                }
            }

            if (BitLength == 0)
            {
                // not sure if there are dummy entries that might trip below exception
                // throw new NotImplementedException("Fragment length cannot be zero");
            }
        }

        public void PrintDebug(bool verbose=false)
        {
            if (verbose)
            {

                Console.WriteLine($"{nameof(ByteBitPos)} : {ByteBitPos}");
                Console.WriteLine($"{nameof(BitLength)} : {BitLength}");
                Console.WriteLine($"{nameof(ImplementationType)} : {ImplementationType}");
                Console.WriteLine($"{nameof(ImplementationUpper)} : 0x{ImplementationUpper:X}");

                Console.WriteLine($"{nameof(Name)} : {Name}");
                Console.WriteLine($"{nameof(Description)} : {Description}");
                Console.WriteLine($"{nameof(ReadAccessLevel)} : {ReadAccessLevel}");
                Console.WriteLine($"{nameof(WriteAccessLevel)} : {WriteAccessLevel}");
                Console.WriteLine($"{nameof(ByteOrder)} : {ByteOrder}");
                Console.WriteLine($"{nameof(RawBitLength)} : {RawBitLength}");
                Console.WriteLine($"{nameof(IttOffset)} : {IttOffset}");
                Console.WriteLine($"{nameof(InfoPoolIndex)} : {InfoPoolIndex}");
                Console.WriteLine($"{nameof(MeaningB)} : {MeaningB}");
                Console.WriteLine($"{nameof(MeaningC)} : {MeaningC}");
                Console.WriteLine($"{nameof(CCFHandle)} : {CCFHandle}");
                Console.WriteLine($"{nameof(VarcodeDumpSize)} : {VarcodeDumpSize}");
                Console.WriteLine($"{nameof(VarcodeDump)} : {BitUtility.BytesToHex(VarcodeDump)}");
                Console.WriteLine($"{nameof(Subfragments)} : {Subfragments}");
                Console.WriteLine($"{nameof(Qualifier)} : {Qualifier}");
            }
            else 
            {
                Console.WriteLine($"{Qualifier}%{ByteBitPos}%{BitLength}%[{ImplementationUpper:X}/{ImplementationLower:X}]");
            }

        }

        protected override void ReadData(CaesarReader reader, CTFLanguage language, ECU? currentEcu)
        {
            throw new NotImplementedException();
        }
    }
}
