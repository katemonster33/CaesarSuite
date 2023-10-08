using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Caesar
{
    public class DiagPresentation
    {
        public string? Qualifier;
        public CaesarStringReference? Description;
        public int? ScaleTableOffset;
        public int? ScaleCountMaybe;
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
        public int? DisplayedUnit_CTF;
        public int? Unk11;
        public int? Unk12;
        public int? EnumMaxValue;
        public int? Unk14;
        public int? Unk15;
        public int? Description2_CTF;
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
        // public string DescriptionString;
        // public string DisplayedUnitString;
        // public string DescriptionString2;

        private long BaseAddress;
        public int PresentationIndex;



        [Newtonsoft.Json.JsonIgnore]
        public string? DisplayedUnitString { get { return Language.GetString(DisplayedUnit_CTF); } }
        [Newtonsoft.Json.JsonIgnore]
        public string? DescriptionString2 { get { return Language.GetString(Description2_CTF); } }

        [Newtonsoft.Json.JsonIgnore]
        public CTFLanguage Language;

        public List<Scale>? Scales;

        public void Restore(CTFLanguage language) 
        {
            Language = language;
            if(Scales != null)
            {
                foreach (Scale s in Scales)
                {
                    s.Restore(language);
                }
            }
        }

        public DiagPresentation() 
        {
            Language = new CTFLanguage();
        }

        // 0x05 [6,   4,4,4,4,  4,4,4,4,  4,4,4,4,  2,2,2,4,      4,4,4,4,   4,4,4,4,   4,4,1,1,  1,1,1,4,     4,4,2,4,   4,4],

        public DiagPresentation(CaesarReader reader, long baseAddress, int presentationsIndex, CTFLanguage language) 
        {
            BaseAddress = baseAddress;
            PresentationIndex = presentationsIndex;
            Language = language;

            reader.BaseStream.Seek(baseAddress, SeekOrigin.Begin);
            ulong bitflags = reader.ReadUInt32();
            
            ulong extendedBitflags = reader.ReadUInt16(); // skip 2 bytes

            Qualifier = reader.ReadBitflagStringWithReader(ref bitflags, BaseAddress);

            Description = reader.ReadBitflagStringRef(ref bitflags, language);
            ScaleTableOffset = reader.ReadBitflagInt32(ref bitflags);
            ScaleCountMaybe = reader.ReadBitflagInt32(ref bitflags);

            Unk5 = reader.ReadBitflagInt32(ref bitflags);
            NumberChoices = reader.ReadBitflagInt32(ref bitflags);
            Unk7 = reader.ReadBitflagInt32(ref bitflags);
            Unk8 = reader.ReadBitflagInt32(ref bitflags);

            Unk9 = reader.ReadBitflagInt32(ref bitflags);
            UnkA = reader.ReadBitflagInt32(ref bitflags);
            UnkB = reader.ReadBitflagInt32(ref bitflags);
            UnkC = reader.ReadBitflagInt32(ref bitflags);

            UnkD = reader.ReadBitflagInt16(ref bitflags);
            UnkE = reader.ReadBitflagInt16(ref bitflags);
            UnkF = reader.ReadBitflagInt16(ref bitflags);
            DisplayedUnit_CTF = reader.ReadBitflagInt32(ref bitflags);

            Unk11 = reader.ReadBitflagInt32(ref bitflags);
            Unk12 = reader.ReadBitflagInt32(ref bitflags);
            EnumMaxValue = reader.ReadBitflagInt32(ref bitflags);
            Unk14 = reader.ReadBitflagInt32(ref bitflags);

            Unk15 = reader.ReadBitflagInt32(ref bitflags);
            Description2_CTF = reader.ReadBitflagInt32(ref bitflags);
            Unk17 = reader.ReadBitflagInt32(ref bitflags);
            Unk18 = reader.ReadBitflagInt32(ref bitflags);

            Unk19 = reader.ReadBitflagInt32(ref bitflags);
            TypeLength_1A = reader.ReadBitflagInt32(ref bitflags);
            InternalDataType = reader.ReadBitflagInt8(ref bitflags);
            Type_1C = reader.ReadBitflagInt8(ref bitflags);

            Unk1d = reader.ReadBitflagInt8(ref bitflags);
            SignBit = reader.ReadBitflagInt8(ref bitflags);
            ByteOrder = reader.ReadBitflagInt8(ref bitflags);
            Unk20 = reader.ReadBitflagInt32(ref bitflags);

            bitflags = extendedBitflags;

            TypeLengthBytesMaybe_21 = reader.ReadBitflagInt32(ref bitflags);
            Unk22 = reader.ReadBitflagInt32(ref bitflags);
            Unk23 = reader.ReadBitflagInt16(ref bitflags);
            Unk24 = reader.ReadBitflagInt32(ref bitflags);

            Unk25 = reader.ReadBitflagInt32(ref bitflags);
            Unk26 = reader.ReadBitflagInt32(ref bitflags);


            if(ScaleTableOffset != null)
            {
                long scaleTableBase = BaseAddress + (long)ScaleTableOffset;
                Scales = new List<Scale>();
                for (int i = 0; i < ScaleCountMaybe; i++)
                {
                    reader.BaseStream.Seek(scaleTableBase + (i * 4), SeekOrigin.Begin);
                    int entryRelativeOffset = reader.ReadInt32();

                    Scale scale = new Scale(reader, scaleTableBase + entryRelativeOffset, language);
                    Scales.Add(scale);
                }
            }
            else
            {
                Scales = null;
            }
        }

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

            bool isEnumType = (SignBit == 0) && ((Type_1C == 1) || (ScaleCountMaybe > 1));

            // hack: sometimes hybrid types (regularly parsed as an scaled value if within bounds) are misinterpreted as pure enums
            // this is a temporary fix for kilometerstand until there's a better way to ascertain its type
            // this also won't work on other similar cases without a unit string e.g. error instance counter (Häufigkeitszähler)
            if (DisplayedUnitString == "km") 
            {
                isEnumType = false;
            }

            if (workingBytes.Length != TypeLength_1A)
            {
                return $"InBytes [{BitUtility.BytesToHex(workingBytes)}] length mismatch (expecting {TypeLength_1A})";
            }

            // handle booleans first since they're the edge case where they can cross byte boundaries
            if (inPreparation.SizeInBits == 1)
            {
                int bytesToSkip = (int)(inPreparation.BitPosition / 8);
                int bitsToSkip = inPreparation.BitPosition % 8;
                byte selectedByte = inBytes[bytesToSkip];

                int selectedBit = (selectedByte >> bitsToSkip) & 1;
                if (isEnumType && Scales != null && Scales.Count > selectedBit)
                {
                    return $"{descriptionPrefix}{Language.GetString(Scales[selectedBit].EnumDescription)} {DisplayedUnitString}";
                }
                else 
                {
                    return $"{descriptionPrefix}{selectedBit} {DisplayedUnitString}";
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
                if (dataType == 20 && Scales != null && Scales.Count > 0)
                {
                    humanReadableType = "ScaledType";

                    double valueToScale = rawIntInterpretation;

                    // if there's only one scale, use it as-is
                    // if there's more than one, use the first scale as an interim solution;
                    // the results of stacking scales does not make sense
                    // there might be a better, non-hardcoded (0) solution to this, and perhaps with a sig-fig specifier
                    if (Scales[0].MultiplyFactor != null && Scales[0].AddConstOffset != null)
                    {
                        valueToScale *= (double)Scales[0].MultiplyFactor;
                        valueToScale += (double)Scales[0].AddConstOffset;
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
                foreach (Scale scale in Scales)
                {
                    if ((scale.EnumUpBound > 0) || (scale.EnumLowBound > 0))
                    {
                        useNewInterpretation = true;
                        break;
                    }
                }

                if (useNewInterpretation)
                {
                    foreach (Scale scale in Scales)
                    {
                        if ((rawIntInterpretation >= scale.EnumLowBound) && (rawIntInterpretation <= scale.EnumUpBound))
                        {
                            return $"{descriptionPrefix}{Language.GetString(scale.EnumDescription)} {DisplayedUnitString}";
                        }
                    }
                }
                else 
                {
                    // original implementation, probably incorrect
                    if (rawIntInterpretation < Scales.Count)
                    {
                        return $"{descriptionPrefix}{Language.GetString(Scales[rawIntInterpretation].EnumDescription)} {DisplayedUnitString}";
                    }
                }
                return $"{descriptionPrefix}(Enum not found) {DisplayedUnitString}";
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
                    return $"{descriptionPrefix}{parsedValue} {DisplayedUnitString} ({humanReadableType})";
                }
                else
                {
                    return $"{descriptionPrefix}{parsedValue} {DisplayedUnitString}";
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
            if (ScaleTableOffset != -1)
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
            Console.WriteLine($"{nameof(ScaleTableOffset)}: {ScaleTableOffset}");
            Console.WriteLine($"{nameof(ScaleCountMaybe)}: {ScaleCountMaybe}");

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
            Console.WriteLine($"{nameof(DisplayedUnitString)}: {DisplayedUnitString}");
            Console.WriteLine($"{nameof(DescriptionString2)}: {DescriptionString2}");
            Console.WriteLine($"Type: {GetDataType()}");
            Console.WriteLine($"{nameof(Type_1C)}: {Type_1C}");
            Console.WriteLine($"{nameof(TypeLength_1A)}: {TypeLength_1A}");
            Console.WriteLine($"ScaleOffset: 0x{(ScaleTableOffset + BaseAddress):X}, base of pres @ 0x{BaseAddress:X}");

            if (Scales != null)
            {
                foreach (Scale s in Scales)
                {
                    Console.WriteLine("Scale: ");
                    s.PrintDebug();
                }
            }
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
            sb.Append($" {nameof(BaseAddress)}: 0x{BaseAddress:X8}");
            sb.Append($" {nameof(Type_1C)}: {Type_1C}");
            sb.Append($" {nameof(TypeLength_1A)}: {TypeLength_1A}");
            sb.Append($" Type: {GetDataType()}");
            sb.Append($" {nameof(ScaleTableOffset)}: {ScaleTableOffset}");
            sb.Append($" {nameof(Qualifier)}: {Qualifier}"); sb.Append($" {nameof(ScaleCountMaybe)}: {ScaleCountMaybe}");
            if (ScaleCountMaybe > 0)
            {
                sb.Append($" {Language.GetString(Scales[0].EnumDescription)}");
            }
            return sb.ToString();
        }


    }
}
