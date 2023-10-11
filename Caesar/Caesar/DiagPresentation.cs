using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Caesar
{
    public class DiagPresentation : CaesarObject
    {
        public string? Qualifier;
        public CaesarStringReference? Description;
        public CaesarTable<Scale>? Scales;
        public int? Unk5;
        public int? NumberChoices;
        public int? Unk7;
        public int? Unk8;
        public int? Unk9;
        public int? UnkA;
        public int? UnkB;
        public int? UnkC;
        public int? UnkD;
        public int? UnkE;
        public int? UnkF;
        public CaesarStringReference? DisplayedUnit;
        public int? Unk11;
        public int? Unk12;
        public int? EnumMaxValue;
        public int? Unk14;
        public int? Unk15;
        public CaesarStringReference? Description2;
        public int? Unk17;
        public int? Unk18;
        public int? Unk19;
        public int? TypeLength_1A;
        public int? InternalDataType; // discovered by @prj : #37
        public int? Type_1C;
        public int? Unk1d;
        public int? SignBit; // discovered by @prj : #37
        public int? ByteOrder; // discovered by @prj : #37 ; Unset = HiLo, 1 = LoHi
        public int? Unk20;

        public int? TypeLengthBytesMaybe_21;
        public int? Unk22;
        public int? Unk23;
        public int? Unk24;
        public int? Unk25;
        public int? Unk26;

        public int PresentationIndex;

        [Newtonsoft.Json.JsonIgnore]
        public CTFLanguage Language;


        public void Restore(CTFLanguage language) 
        {
            Language = language;
        }

        public DiagPresentation() 
        {
            Language = new CTFLanguage();
        }

        // 0x05 [6,   4,4,4,4,  4,4,4,4,  4,4,4,4,  2,2,2,4,      4,4,4,4,   4,4,4,4,   4,4,1,1,  1,1,1,4,     4,4,2,4,   4,4],


        public string InterpretData(byte[] inBytes, DiagPreparation inPreparation, bool describe = true)
        {
            // might be relevant: DMPrepareSingleDatum, DMPresentSingleDatum

            bool isDebugBuild = false;
#if DEBUG
            isDebugBuild = true;
#endif

            string descriptionPrefix = describe ? $"{Description?.Text}: " : "";
            byte[] workingBytes = new byte[0];
            if (TypeLength_1A != null)
            {
                workingBytes = inBytes.Skip(inPreparation.BitPosition / 8).Take((int)TypeLength_1A).ToArray();
            }

            bool isEnumType = (SignBit == 0) && ((Type_1C == 1) || (Scales != null && Scales.Count > 1));

            // hack: sometimes hybrid types (regularly parsed as an scaled value if within bounds) are misinterpreted as pure enums
            // this is a temporary fix for kilometerstand until there's a better way to ascertain its type
            // this also won't work on other similar cases without a unit string e.g. error instance counter (Häufigkeitszähler)
            if (DisplayedUnit?.Text == "km") 
            {
                isEnumType = false;
            }

            if (workingBytes.Length != TypeLength_1A)
            {
                return $"InBytes [{BitUtility.BytesToHex(workingBytes)}] length mismatch (expecting {TypeLength_1A})";
            }
            List<Scale> scalesList = Scales != null ? Scales.GetObjects() : new List<Scale>();
            // handle booleans first since they're the edge case where they can cross byte boundaries
            if (inPreparation.SizeInBits == 1)
            {
                int bytesToSkip = (int)(inPreparation.BitPosition / 8);
                int bitsToSkip = inPreparation.BitPosition % 8;
                byte selectedByte = inBytes[bytesToSkip];

                int selectedBit = (selectedByte >> bitsToSkip) & 1;
                if (isEnumType && scalesList.Count > selectedBit)
                {
                    return $"{descriptionPrefix}{scalesList[selectedBit].EnumDescription?.Text} {DisplayedUnit?.Text}";
                }
                else 
                {
                    return $"{descriptionPrefix}{selectedBit} {DisplayedUnit?.Text}";
                }
            }

            // everything else should be aligned to byte boundaries
            if (inPreparation.BitPosition % 8 != 0)
            {
                return "BitOffset was outside byte boundary (skipped)";
            }
            int dataType = GetDataType();
            int rawIntInterpretation = 0;

            string humanReadableType = $"UnhandledType:{dataType}";
            string parsedValue = BitUtility.BytesToHex(workingBytes, true);
            if (dataType == 20)
            {
                // parse as a regular int (BE)
                for (int i = 0; i < workingBytes.Length; i++)
                {
                    rawIntInterpretation <<= 8;
                    rawIntInterpretation |= workingBytes[i];
                }

                humanReadableType = "IntegerType";

                parsedValue = rawIntInterpretation.ToString();
                if (dataType == 20 && scalesList.Count > 0)
                {
                    humanReadableType = "ScaledType";

                    double valueToScale = rawIntInterpretation;

                    // if there's only one scale, use it as-is
                    // if there's more than one, use the first scale as an interim solution;
                    // the results of stacking scales does not make sense
                    // there might be a better, non-hardcoded (0) solution to this, and perhaps with a sig-fig specifier
                    var singleScale = scalesList[0];
                    if (singleScale.MultiplyFactor != null && singleScale.AddConstOffset != null)
                    {
                        valueToScale *= (double)singleScale.MultiplyFactor;
                        valueToScale += (double)singleScale.AddConstOffset;
                    }

                    parsedValue = valueToScale.ToString("0.000000");
                }
            }
            else if (dataType == 6) 
            {
                // type 6 refers to either internal presentation types 8 (ieee754 float) or 5 (unsigned int?)
                // these values are tagged with an exclamation [!] i (jglim) am not sure if they will work correctly yet
                // specifically, i am not sure if the big endian float parsing is done correctly
                uint rawUIntInterpretation = 0;
                for (int i = 0; i < 4; i++)
                {
                    rawUIntInterpretation <<= 8;
                    rawUIntInterpretation |= workingBytes[i];
                }

                if (InternalDataType == 8)
                {
                    // interpret as big-endian float, https://github.com/jglim/CaesarSuite/issues/37
                    parsedValue = BitUtility.ToFloat(rawUIntInterpretation).ToString("");
                    humanReadableType = "Float [!]";
                }
                else if (InternalDataType == 5) 
                {
                    // haven't seen this one around, will parse as a regular int (BE) for now
                    humanReadableType = "UnsignedIntegerType [!]";
                    parsedValue = rawUIntInterpretation.ToString();
                }
            }
            else if (dataType == 18)
            {
                humanReadableType = "HexdumpType";
            }
            else if (dataType == 17)
            {
                humanReadableType = "StringType";
                parsedValue = Encoding.UTF8.GetString(workingBytes);
            }

            if (isEnumType && Scales != null)
            {
                // discovered by @VladLupashevskyi in https://github.com/jglim/CaesarSuite/issues/27
                // if an enum is specified, the inclusive upper bound and lower bound will be defined in the scale object

                bool useNewInterpretation = false;
                foreach (Scale scale in scalesList)
                {
                    if ((scale.EnumUpBound > 0) || (scale.EnumLowBound > 0))
                    {
                        useNewInterpretation = true;
                        break;
                    }
                }

                if (useNewInterpretation)
                {
                    foreach (Scale scale in scalesList)
                    {
                        if ((rawIntInterpretation >= scale.EnumLowBound) && (rawIntInterpretation <= scale.EnumUpBound))
                        {
                            return $"{descriptionPrefix}{scale.EnumDescription?.Text} {DisplayedUnit?.Text}";
                        }
                    }
                }
                else 
                {
                    // original implementation, probably incorrect
                    if (rawIntInterpretation < scalesList.Count)
                    {
                        return $"{descriptionPrefix}{scalesList[rawIntInterpretation].EnumDescription?.Text} {DisplayedUnit?.Text}";
                    }
                }
                return $"{descriptionPrefix}(Enum not found) {DisplayedUnit?.Text}";
                // this bit below for troubleshooting problematic presentations
                /*
                if (rawIntInterpretation < Scales.Count)
                {
                    return $"{descriptionPrefix}{Language.GetString(Scales[rawIntInterpretation].EnumDescription)} {DisplayedUnitString}";
                }
                else 
                {
                    // seems like an enum-like value broke
                    return $"{descriptionPrefix}{Language.GetString(Scales[0].EnumDescription)} {DisplayedUnitString} [!]";
                }
                */
            }
            else
            {
                if (isDebugBuild)
                {
                    return $"{descriptionPrefix}{parsedValue} {DisplayedUnit?.Text} ({humanReadableType})";
                }
                else
                {
                    return $"{descriptionPrefix}{parsedValue} {DisplayedUnit?.Text}";
                }
            }
        }

        public int GetDataType() 
        {
            // see DIDiagServiceRealPresType
            int result = -1;
            if (Unk14 != -1) 
            {
                return 20;
            }

            // does the value have scale structures attached to it? 
            // supposed to parse scale struct and check if we can return 20
            if (Scales != null)
            {
                return 20; // scaled value
            }
            else
            {
                if (Unk5 != -1)
                {
                    return 18; // hexdump raw
                }
                if (Unk17 != -1)
                {
                    return 18; // hexdump raw
                }
                if (Unk19 != -1)
                {
                    return 18; // hexdump raw
                }
                if (Unk22 != -1)
                {
                    return 18; // hexdump raw
                }
                if (InternalDataType != -1)
                {
                    if (InternalDataType == 6)
                    {
                        return 17; // ascii dump
                    }
                    else if (InternalDataType == 7)
                    {
                        return 22; // ?? haven't seen this one around
                    }
                    else if (InternalDataType == 8)
                    {
                        result = 6; // IEEE754 float, discovered by @prj in https://github.com/jglim/CaesarSuite/issues/37
                    }
                    else if (InternalDataType == 5) 
                    {
                        // UNSIGNED integer (i haven't seen a const for uint around, sticking it into a regular int for now)
                        // this will be an issue for 32-bit+ uints
                        // see DT_STO_Zaehler_Programmierversuche_Reprogramming and DT_STO_ID_Aktive_Diagnose_Information_Version
                        result = 6; 
                    }
                }
                else 
                {
                    if ((TypeLength_1A == -1) || (Type_1C == -1)) 
                    {
                        Console.WriteLine("typelength and type must be valid");
                        // might be good to throw an exception here
                    }
                    if ((SignBit == 1) || (SignBit == 2))
                    {
                        result = 5; // ?? haven't seen this one around
                    }
                    else 
                    {
                        result = 2; // ?? haven't seen this one around
                    }
                }
                return result;
            }
        }

        public void PrintDebug()
        {
            Console.WriteLine("Presentation: ");
            Console.WriteLine($"{nameof(Qualifier)}: {Qualifier}");


            //Console.WriteLine($"{nameof(Description_CTF)}: {Description_CTF}");
            Console.WriteLine($"{nameof(Scales)}: {Scales}");

            Console.WriteLine($"{nameof(Unk5)}: {Unk5}");
            Console.WriteLine($"{nameof(NumberChoices)}: {NumberChoices}");
            Console.WriteLine($"{nameof(Unk7)}: {Unk7}");
            Console.WriteLine($"{nameof(Unk8)}: {Unk8}");

            Console.WriteLine($"{nameof(Unk9)}: {Unk9}");
            Console.WriteLine($"{nameof(UnkA)}: {UnkA}");
            Console.WriteLine($"{nameof(UnkB)}: {UnkB}");
            Console.WriteLine($"{nameof(UnkC)}: {UnkC}");

            Console.WriteLine($"{nameof(UnkD)}: {UnkD}");
            Console.WriteLine($"{nameof(UnkE)}: {UnkE}");
            Console.WriteLine($"{nameof(UnkF)}: {UnkF}");
            //Console.WriteLine($"{nameof(DisplayedUnit_CTF)}: {DisplayedUnit_CTF}");

            Console.WriteLine($"{nameof(Unk11)}: {Unk11}");
            Console.WriteLine($"{nameof(Unk12)}: {Unk12}");
            Console.WriteLine($"{nameof(EnumMaxValue)}: {EnumMaxValue}");
            Console.WriteLine($"{nameof(Unk14)}: {Unk14}");

            Console.WriteLine($"{nameof(Unk15)}: {Unk15}");
            // Console.WriteLine($"{nameof(Description2_CTF)}: {Description2_CTF}");
            Console.WriteLine($"{nameof(Unk17)}: {Unk17}");
            Console.WriteLine($"{nameof(Unk18)}: {Unk18}");

            Console.WriteLine($"{nameof(Unk19)}: {Unk19}");
            Console.WriteLine($"{nameof(InternalDataType)}: {InternalDataType}");

            Console.WriteLine($"{nameof(Unk1d)}: {Unk1d}");
            Console.WriteLine($"{nameof(SignBit)}: {SignBit}");
            Console.WriteLine($"{nameof(ByteOrder)}: {ByteOrder}");
            Console.WriteLine($"{nameof(Unk20)}: {Unk20}");

            Console.WriteLine($"{nameof(TypeLengthBytesMaybe_21)}: {TypeLengthBytesMaybe_21}");
            Console.WriteLine($"{nameof(Unk22)}: {Unk22}");
            Console.WriteLine($"{nameof(Unk23)}: {Unk23}");
            Console.WriteLine($"{nameof(Unk24)}: {Unk24}");

            Console.WriteLine($"{nameof(Unk25)}: {Unk25}");
            Console.WriteLine($"{nameof(Unk26)}: {Unk26}");
            /**/


            Console.WriteLine($"{nameof(Description)}: {Description?.Text}");
            Console.WriteLine($"{nameof(DisplayedUnit)}: {DisplayedUnit?.Text}");
            Console.WriteLine($"{nameof(Description2)}: {Description2?.Text}");
            Console.WriteLine($"Type: {GetDataType()}");
            Console.WriteLine($"{nameof(Type_1C)}: {Type_1C}");
            Console.WriteLine($"{nameof(TypeLength_1A)}: {TypeLength_1A}");
            Console.WriteLine($"Scales: {Scales}");

            Console.WriteLine("Presentation end");
        }

        public string CopyMinDebug()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("PRES: ");
            sb.Append($" {nameof(Unk5)}: {Unk5}");
            sb.Append($" {nameof(NumberChoices)}: {NumberChoices}");
            sb.Append($" {nameof(Unk7)}: {Unk7}");
            sb.Append($" {nameof(Unk8)}: {Unk8}");
            sb.Append($" {nameof(Unk9)}: {Unk9}");
            sb.Append($" {nameof(UnkA)}: {UnkA}");
            sb.Append($" {nameof(UnkB)}: {UnkB}");
            sb.Append($" {nameof(UnkC)}: {UnkC}");
            sb.Append($" {nameof(UnkD)}: {UnkD}");
            sb.Append($" {nameof(UnkE)}: {UnkE}");
            sb.Append($" {nameof(UnkF)}: {UnkF}");
            sb.Append($" {nameof(Unk11)}: {Unk11}");
            sb.Append($" {nameof(Unk12)}: {Unk12}");
            sb.Append($" {nameof(EnumMaxValue)}: {EnumMaxValue}");
            sb.Append($" {nameof(Unk14)}: {Unk14}");
            sb.Append($" {nameof(Unk15)}: {Unk15}");
            sb.Append($" {nameof(Unk17)}: {Unk17}");
            sb.Append($" {nameof(Unk18)}: {Unk18}");
            sb.Append($" {nameof(Unk19)}: {Unk19}");
            sb.Append($" {nameof(InternalDataType)}: {InternalDataType}");
            sb.Append($" {nameof(Unk1d)}: {Unk1d}");
            sb.Append($" {nameof(SignBit)}: {SignBit}");
            sb.Append($" {nameof(ByteOrder)}: {ByteOrder}");
            sb.Append($" {nameof(Unk20)}: {Unk20}");
            sb.Append($" {nameof(TypeLengthBytesMaybe_21)}: {TypeLengthBytesMaybe_21}");
            sb.Append($" {nameof(Unk22)}: {Unk22}");
            sb.Append($" {nameof(Unk23)}: {Unk23}");
            sb.Append($" {nameof(Unk24)}: {Unk24}");
            sb.Append($" {nameof(Unk25)}: {Unk25}");
            sb.Append($" {nameof(Unk26)}: {Unk26}");
            sb.Append($" {nameof(AbsoluteAddress)}: 0x{AbsoluteAddress:X8}");
            sb.Append($" {nameof(Type_1C)}: {Type_1C}");
            sb.Append($" {nameof(TypeLength_1A)}: {TypeLength_1A}");
            sb.Append($" Type: {GetDataType()}");
            sb.Append($" {nameof(Scales)}: {Scales}");
            sb.Append($" {nameof(Qualifier)}: {Qualifier}"); 
            return sb.ToString();
        }

        protected override bool ReadHeader(CaesarReader reader)
        {
            RelativeAddress = reader.ReadInt32();
            int dataSize = reader.ReadInt32();
            return true;
        }

        protected override void ReadData(CaesarReader reader, CaesarContainer container)
        {
            if(ParentObject == null)
            {
                return;
            }
            Bitflags = reader.ReadUInt32();

            Bitflags |= (ulong)reader.ReadUInt16() << 32;

            Qualifier = reader.ReadBitflagStringWithReader(ref Bitflags, AbsoluteAddress);

            Description = reader.ReadBitflagStringRef(ref Bitflags, container);

            Scales = reader.ReadBitflagSubTable<Scale>(this, container);

            Unk5 = reader.ReadBitflagInt32(ref Bitflags);
            NumberChoices = reader.ReadBitflagInt32(ref Bitflags);
            Unk7 = reader.ReadBitflagInt32(ref Bitflags);
            Unk8 = reader.ReadBitflagInt32(ref Bitflags);

            Unk9 = reader.ReadBitflagInt32(ref Bitflags);
            UnkA = reader.ReadBitflagInt32(ref Bitflags);
            UnkB = reader.ReadBitflagInt32(ref Bitflags);
            UnkC = reader.ReadBitflagInt32(ref Bitflags);

            UnkD = reader.ReadBitflagInt16(ref Bitflags);
            UnkE = reader.ReadBitflagInt16(ref Bitflags);
            UnkF = reader.ReadBitflagInt16(ref Bitflags);
            DisplayedUnit = reader.ReadBitflagStringRef(ref Bitflags, container);

            Unk11 = reader.ReadBitflagInt32(ref Bitflags);
            Unk12 = reader.ReadBitflagInt32(ref Bitflags);
            EnumMaxValue = reader.ReadBitflagInt32(ref Bitflags);
            Unk14 = reader.ReadBitflagInt32(ref Bitflags);

            Unk15 = reader.ReadBitflagInt32(ref Bitflags);
            Description2 = reader.ReadBitflagStringRef(ref Bitflags, container);
            Unk17 = reader.ReadBitflagInt32(ref Bitflags);
            Unk18 = reader.ReadBitflagInt32(ref Bitflags);

            Unk19 = reader.ReadBitflagInt32(ref Bitflags);
            TypeLength_1A = reader.ReadBitflagInt32(ref Bitflags);
            InternalDataType = reader.ReadBitflagInt8(ref Bitflags);
            Type_1C = reader.ReadBitflagInt8(ref Bitflags);

            Unk1d = reader.ReadBitflagInt8(ref Bitflags);
            SignBit = reader.ReadBitflagInt8(ref Bitflags);
            ByteOrder = reader.ReadBitflagInt8(ref Bitflags);
            Unk20 = reader.ReadBitflagInt32(ref Bitflags);

            TypeLengthBytesMaybe_21 = reader.ReadBitflagInt32(ref Bitflags);
            Unk22 = reader.ReadBitflagInt32(ref Bitflags);
            Unk23 = reader.ReadBitflagInt16(ref Bitflags);
            Unk24 = reader.ReadBitflagInt32(ref Bitflags);

            Unk25 = reader.ReadBitflagInt32(ref Bitflags);
            Unk26 = reader.ReadBitflagInt32(ref Bitflags);
        }
    }
}
