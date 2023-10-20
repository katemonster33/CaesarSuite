using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Caesar
{
    public class DiagPreparation : CaesarObject
    {
        public string? Qualifier;
        public CaesarStringReference? Name;
        public int? Unk1;
        public int? Unk2;
        public int? AlternativeBitWidth;
        public int? IITOffset;
        private int? InfoPoolIndex;
        private int? PresPoolIndex;
        public DiagPresentation? Presentation;
        public int? Field1E;
        public int? SystemParam;
        public int? DumpMode;
        private int? DumpSize;
        public byte[]? Dump;

        public int BitPosition;

        public ushort ModeConfig;

        public int SizeInBits = 0;

        public static readonly byte[] IntegerSizeMapping = new byte[] { 0x00, 0x01, 0x04, 0x08, 0x10, 0x20, 0x40 };

        public InferredDataType FieldType;

        public enum InferredDataType 
        {
            UnassignedType,
            IntegerType,
            NativeInfoPoolType,
            NativePresentationType,
            UnhandledITType,
            UnhandledSP17Type,
            UnhandledType,
            BitDumpType,
            ExtendedBitDumpType,
        }

        public void Restore(DiagService parentDiagService, ECU parentEcu) 
        {
            SetSizeInBits(parentDiagService, parentEcu);
        }

        // look at.. DIInternalRetrieveConstParamPreparation
        // 
        private void SetSizeInBits(DiagService parentDiagService, ECU parentEcu) 
        {
            // if (modeConfig & 0xF00) == 0x300, the value is a const param: DIIsConstParameter

            // VCFragment does the same thing.. with the same ITT exception
            // BitPosition /= 8

            // look for the string "nImplType <= 6"
            uint modeE = (uint)ModeConfig & 0xF000;
            uint modeH = (uint)ModeConfig & 0xFF0;
            uint modeL = (uint)ModeConfig & 0xF;
            int resultBitSize = 0;


            if ((ModeConfig & 0xF00) == 0x300) // this check is made in DIDiagServiceRetrievePreparation
            {
                if (modeL > 6)
                {
                    throw new Exception("nImplType <= 6; trying to map a data type that cannot exist");
                }

                // const params : 0x320, 0x330, 0x340
                if (modeH == 0x320)
                {
                    // this behavior is confirmed
                    resultBitSize = IntegerSizeMapping[modeL];
                    FieldType = InferredDataType.IntegerType;
                }
                else if (modeH == 0x330 && AlternativeBitWidth != null)
                {
                    // this behavior is also okay
                    resultBitSize = (int)AlternativeBitWidth; // inPres + 20
                    FieldType = InferredDataType.BitDumpType;
                }
                else if (modeH == 0x340)
                {
                    // from dasm, but unimplemented
                    // DIInternalRetrieveConstParamPreparation
                    FieldType = InferredDataType.UnhandledITType;
                    throw new NotImplementedException("WARNING: valid but unhandled data size (ITT not parsed)");
                    // resultBitSize = 0; // inPres + 20
                }
            }
            else 
            {
                // if systemparam is -1.. load a default system type
                if (SystemParam == null)
                {
                    // apparently both 0x2000 and 0x8000 source from different pools, but use the same PRESENTATION structure
                    if (modeE == 0x8000 && InfoPoolIndex != null)
                    {
                        FieldType = InferredDataType.NativeInfoPoolType;
                        if (parentEcu.GlobalInternalPresentations != null)
                        {
                            Presentation = parentEcu.GlobalInternalPresentations.GetObjects()[(int)InfoPoolIndex];
                            resultBitSize = Presentation.GetBitSize();
                        }
                    }
                    else if (modeE == 0x2000 && PresPoolIndex != null)
                    {
                        FieldType = InferredDataType.NativePresentationType;
                        if (parentEcu.GlobalPresentations != null)
                        {
                            Presentation = parentEcu.GlobalPresentations.GetObjects()[(int)PresPoolIndex];
                            resultBitSize = Presentation.GetBitSize();
                        }
                    }
                    else 
                    {
                        // should throw an exception?
                        //Console.WriteLine($"WARNING: Unknown or unhandled type for for {qualifier}");
                        throw new Exception($"Attempted to load an unknown system type for {Qualifier}");
                    }
                }
                else 
                {
                    // not a const param, not a native param, this is a special param, parsed at DIInternalRetrieveSpecialPreparation
                    // DIInternalRetrieveSpecialPreparation officially supports 0x410, 0x420 only
                    if (modeH == 0x410)
                    {
                        int reducedSysParam = (int)SystemParam - 0x10;
                        if (reducedSysParam == 0 && parentDiagService.RequestBytes != null)
                        {
                            // specifically requests for LOBYTE (& 0xFF)
                            int resultByteSize = (parentDiagService.RequestBytes.Length & 0xFF) - (BitPosition / 8);
                            resultBitSize = resultByteSize * 8;
                            FieldType = InferredDataType.ExtendedBitDumpType;
                            // Console.WriteLine($"0x{modeH:X} debug for {qualifier} (L: {modeL}) (BitWidth: {AlternativeBitWidth} SP: {SystemParam}), sz: {resultBitSize} b ({resultBitSize/8} B)");
                        }
                        else if (reducedSysParam == 17 && parentEcu.GlobalDiagServices != null)
                        {
                            // open a diagservice based on inputRef name
                            // this is experimental, haven't seen a cbf that uses this yet
                            Console.WriteLine($"Parsing experimental 0x410 prep with sysparam 17 at {Qualifier}");
                            DiagService? referencedDs = parentEcu.GlobalDiagServices.GetObjects().Find(x => x.Qualifier == parentDiagService.InputRefNameMaybe);
                            if (referencedDs != null && referencedDs.DataClass_ServiceTypeShifted != null && parentDiagService.RequestBytes != null && referencedDs.RequestBytes != null)
                            {
                                bool referencedDsHasRequestData = referencedDs.RequestBytes.Length > 0; // supposed to check if requestMessage is valid too
                                int internalType = (int)referencedDs.DataClass_ServiceTypeShifted;
                                if (((referencedDs.DataClass_ServiceTypeShifted & 0xC) > 0) && referencedDsHasRequestData)
                                {
                                    if ((referencedDs.DataClass_ServiceTypeShifted & 4) > 0)
                                    {
                                        internalType = 0x10000000;
                                    }
                                    else
                                    {
                                        internalType = 0x20000000;
                                    }
                                }
                                if ((internalType & 0x10000) != 0 && parentDiagService.P_Count != null)
                                {
                                    // referenced type is a global variable
                                    resultBitSize = (int)(parentDiagService.P_Count * 8);
                                    FieldType = InferredDataType.UnhandledSP17Type;
                                }
                                else
                                {
                                    // use pres dump length
                                    FieldType = InferredDataType.UnhandledSP17Type;
                                    resultBitSize = parentDiagService.RequestBytes.Length * 8;
                                }
                            }
                            else
                            {
                                Console.WriteLine($"0x410 : sys param: 17 for qualifier {Qualifier} could not find referenced DiagService with index {parentDiagService.InputRefNameMaybe}");
                                // throw new NotImplementedException
                            }
                        }
                        else
                        {
                            throw new Exception($"Invalid system parameter for {Qualifier}");
                        }
                    }
                    else if (modeH == 0x420)
                    {
                        if (modeL > 6)
                        {
                            throw new Exception("nImplType <= 6; trying to map a data type that cannot exist");
                        }
                        FieldType = InferredDataType.IntegerType;
                        resultBitSize = IntegerSizeMapping[modeL];
                    }
                    else if (modeH == 0x430 && AlternativeBitWidth != null)
                    {
                        // mode 0x430 is nonstandard and doesn't seem to exist in the function that I was disassembling
                        /*
                            AlternativeBitWidth : 128
                            SystemParam : 37
                            
                            See 0x320 vs 0x330, seems to be similar
                        */

                        resultBitSize = (int)AlternativeBitWidth; // inPres + 20
                        FieldType = InferredDataType.BitDumpType;
                    }
                    else
                    {
                        FieldType = InferredDataType.UnhandledType;
                        Console.WriteLine($"Unhandled type: {modeH} for {Qualifier}");
                        PrintDebug();
                        throw new Exception($"Attempted to load an unknown special param type for {Qualifier}");
                        //Console.WriteLine($"{qualifier} ({poolThing}/{ParentECU.ecuInfoPool_tableEntryCount})\n{BitUtility.BytesToHex(presentationStruct)}\n\n");
                    }
                }
            }


            /*
            if (modeH == 0x430)
            {
                // guessed
                if (verbose)
                {
                    Console.WriteLine($"Unsupported 0x{modeH:X} behavior for {qualifier} (L: {modeL}) (BitWidth: {AlternativeBitWidth} ByteWidth: {SystemParam})");
                }
                //PrintDebug();
                resultBitSize = AlternativeBitWidth; // alternate bit width is 128 which should be a nice 16 bytes
            }
            else if (modeH > 0x430)
            {
                // guessed from varcoding behavior
                if ((PresPool == 0) && (AvailableBitWidth_PoolThing == 0))
                {
                    return 0;
                }
                else
                {
                    //Console.WriteLine($"No idea how to handle Pres 0x750 from {qualifier} : {PresPool}");
                }
                Console.WriteLine($"No idea how to handle 0x{modeH:X} from {qualifier} ({PresPool}, {AvailableBitWidth_PoolThing})");
            }
            */
            SizeInBits = resultBitSize;
        }


        public void PrintDebug()
        {
            Console.WriteLine($"{nameof(Qualifier)} : {Qualifier}");
            Console.WriteLine($"{nameof(BitPosition)} : {BitPosition}");
            Console.WriteLine($"{nameof(ModeConfig)} : 0x{ModeConfig:X}");
            Console.WriteLine($"Mode H : 0x{ModeConfig & 0xFF0:X}, L : 0x{ModeConfig & 0xF:X}");
            Console.WriteLine($"{nameof(SizeInBits)} : 0x{SizeInBits:X}");
            Console.WriteLine($"{nameof(Name)} : {Name}");
            Console.WriteLine($"{nameof(Unk1)} : {Unk1}");
            Console.WriteLine($"{nameof(Unk2)} : {Unk2}");
            Console.WriteLine($"{nameof(AlternativeBitWidth)} : {AlternativeBitWidth}");
            Console.WriteLine($"{nameof(IITOffset)} : {IITOffset}");
            Console.WriteLine($"{nameof(InfoPoolIndex)} : {InfoPoolIndex}");
            Console.WriteLine($"{nameof(PresPoolIndex)} : {PresPoolIndex}");
            Console.WriteLine($"{nameof(Field1E)} : {Field1E}");
            Console.WriteLine($"{nameof(SystemParam)} : {SystemParam}");
            // Console.WriteLine($"{nameof(noIdea_T)} : {language.GetString(noIdea_T)}");
            Console.WriteLine($"{nameof(Dump)} : {BitUtility.BytesToHex(Dump)}");
            Console.WriteLine("---------------");
        }

        protected override bool ReadHeader(CaesarReader reader)
        {
            base.ReadHeader(reader);

            BitPosition = reader.ReadInt32(); // file: 4 (DW)
            ModeConfig = reader.ReadUInt16(); // file: 8 (W)
            return true;
        }

        protected override void ReadData(CaesarReader reader, CaesarContainer container)
        {
            Bitflags = reader.ReadUInt32();

            Qualifier = reader.ReadBitflagStringWithReader(ref Bitflags, AbsoluteAddress);
            Name = reader.ReadBitflagStringRef(ref Bitflags, container);
            Unk1 = reader.ReadBitflagUInt8(ref Bitflags);
            Unk2 = reader.ReadBitflagUInt8(ref Bitflags);
            AlternativeBitWidth = reader.ReadBitflagInt32(ref Bitflags);
            IITOffset = reader.ReadBitflagInt32(ref Bitflags);
            InfoPoolIndex = reader.ReadBitflagInt32(ref Bitflags);
            PresPoolIndex = reader.ReadBitflagInt32(ref Bitflags);
            Field1E = reader.ReadBitflagInt32(ref Bitflags);
            SystemParam = reader.ReadBitflagInt16(ref Bitflags);
            DumpMode = reader.ReadBitflagInt16(ref Bitflags);
            DumpSize = reader.ReadBitflagInt32(ref Bitflags);
            if (DumpMode == 5)
            {
                // dump is actually a string, use
                // CaesarReader.ReadBitflagDumpWithReaderAsString
            }
            Dump = reader.ReadBitflagDumpWithReader(ref Bitflags, DumpSize, AbsoluteAddress);
        }
    }
}
