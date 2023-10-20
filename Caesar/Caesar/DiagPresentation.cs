using Caesar.Enums;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Caesar
{
    public class DiagPresentation : CaesarObject
    {
        public int DataSize = 0;
        public string? Qualifier;
        public CaesarStringReference? Description;
        public CaesarTable<Scale>? Scales;
        private int? ChoiceAddress;
        private int? NumberChoices;
        public List<Choice>? Choices;
        public float ScaledMaximum;
        public int? Unk8;
        public int? Unk9;
        public float ScaledMinimum;
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
        public int? FixedLength;
        public InternalDataType InternalDataType; // discovered by @prj : #37
        public DataType DataType;
        public int? Unk1d;
        public bool Signed; // discovered by @prj : #37
        public ByteOrder ByteOrder; // discovered by @prj : #37 ; Unset = HiLo, 1 = LoHi
        public int? MinLength;

        public int? MaxLength;
        public int? HexDumpDataLength;
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
            if (FixedLength != null)
            {
                workingBytes = inBytes.Skip(inPreparation.BitPosition / 8).Take((int)FixedLength).ToArray();
            }

            bool isEnumType = !Signed && (DataType == DataType.Byte || (Scales != null && Scales.Count > 1));

            // hack: sometimes hybrid types (regularly parsed as an scaled value if within bounds) are misinterpreted as pure enums
            // this is a temporary fix for kilometerstand until there's a better way to ascertain its type
            // this also won't work on other similar cases without a unit string e.g. error instance counter (Häufigkeitszähler)
            if (DisplayedUnit?.Text == "km")
            {
                isEnumType = false;
            }

            if (workingBytes.Length != FixedLength)
            {
                return $"InBytes [{BitUtility.BytesToHex(workingBytes)}] length mismatch (expecting {FixedLength})";
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
            ParamType dataType = GetDataType();
            int rawIntInterpretation = 0;

            string humanReadableType = $"UnhandledType:{dataType}";
            string parsedValue = BitUtility.BytesToHex(workingBytes, true);
            if (dataType == ParamType.Unknown)
            {
                // parse as a regular int (BE)
                for (int i = 0; i < workingBytes.Length; i++)
                {
                    rawIntInterpretation <<= 8;
                    rawIntInterpretation |= workingBytes[i];
                }

                humanReadableType = "IntegerType";

                parsedValue = rawIntInterpretation.ToString();
                if (dataType == ParamType.Unknown && scalesList.Count > 0)
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
            else if (dataType == ParamType.Float)
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

                if (InternalDataType == Enums.InternalDataType.Float)
                {
                    // interpret as big-endian float, https://github.com/jglim/CaesarSuite/issues/37
                    parsedValue = BitUtility.ToFloat(rawUIntInterpretation).ToString("");
                    humanReadableType = "Float [!]";
                }
                else if (InternalDataType == Enums.InternalDataType.Raw)
                {
                    // haven't seen this one around, will parse as a regular int (BE) for now
                    humanReadableType = "UnsignedIntegerType [!]";
                    parsedValue = rawUIntInterpretation.ToString();
                }
            }
            else if (dataType == ParamType.Choice)
            {
                humanReadableType = "HexdumpType";
            }
            else if (dataType == ParamType.String)
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

        public int GetBitSize()
        {
            int resultBitSize = 0;
            if (FixedLength != null && FixedLength > 0)
            {
                resultBitSize = (int)FixedLength;
            }
            else if (MaxLength != null)
            {
                resultBitSize = (int)MaxLength;
            }

            // if value was specified in bytes, convert to bits
            if (DataType == DataType.Byte)
            {
                resultBitSize *= 8;
            }

            return resultBitSize;
        }

        public ParamType GetDataType()
        {
            // see DIDiagServiceRealPresType
            if (Unk14 != null)
            {
                return ParamType.Unknown; //20;
            }

            // does the value have scale structures attached to it? 
            // supposed to parse scale struct and check if we can return 20
            if (Scales != null)
            {
                return ParamType.Unknown; //20; // scaled value
            }
            else
            {
                if (ChoiceAddress != null || Unk17 != null || Unk19 != null || HexDumpDataLength != null)
                {
                    return ParamType.Choice;// 18; // hexdump raw
                }
                else if (InternalDataType != InternalDataType.Unknown)
                {
                    switch(InternalDataType)
                    {
                        case Enums.InternalDataType.Ascii:
                            return ParamType.String;
                        case Enums.InternalDataType.Unicode:
                            return ParamType.Unicode;
                        case Enums.InternalDataType.Float:
                            return ParamType.Float;
                        case Enums.InternalDataType.Hex:
                            return ParamType.Dump;
                        case Enums.InternalDataType.Invalid:
                            throw new ArgumentException("InternalDataType out of range!");
                        case Enums.InternalDataType.Numeric:
                        case Enums.InternalDataType.Raw:
                        default:
                            //if ((TypeLength_1A == null) || (Type_1C == null))
                            //{
                            //    Console.WriteLine("typelength and type must be valid");
                            //    return ParamType.Unknown;
                            //    // might be good to throw an exception here
                            //}
                            return ParamType.UWord;
                    }
                }
                else
                {
                    if (Signed)
                    {
                        return ParamType.SLong;//result = 5; // ?? haven't seen this one around
                    }
                    else
                    {
                        return ParamType.ULong;//result = 2; // ?? haven't seen this one around
                    }
                }
                //return result;
            }
        }

        public void PrintDebug()
        {
            Console.WriteLine("Presentation: ");
            Console.WriteLine($"{nameof(Qualifier)}: {Qualifier}");


            //Console.WriteLine($"{nameof(Description_CTF)}: {Description_CTF}");
            Console.WriteLine($"{nameof(Scales)}: {Scales}");

            Console.WriteLine($"{nameof(ChoiceAddress)}: {ChoiceAddress}");
            Console.WriteLine($"{nameof(NumberChoices)}: {NumberChoices}");
            Console.WriteLine($"{nameof(ScaledMaximum)}: {ScaledMaximum}");
            Console.WriteLine($"{nameof(Unk8)}: {Unk8}");

            Console.WriteLine($"{nameof(Unk9)}: {Unk9}");
            Console.WriteLine($"{nameof(ScaledMinimum)}: {ScaledMinimum}");
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
            Console.WriteLine($"{nameof(Signed)}: {Signed}");
            Console.WriteLine($"{nameof(ByteOrder)}: {ByteOrder}");
            Console.WriteLine($"{nameof(MinLength)}: {MinLength}");

            Console.WriteLine($"{nameof(MaxLength)}: {MaxLength}");
            Console.WriteLine($"{nameof(HexDumpDataLength)}: {HexDumpDataLength}");
            Console.WriteLine($"{nameof(Unk23)}: {Unk23}");
            Console.WriteLine($"{nameof(Unk24)}: {Unk24}");

            Console.WriteLine($"{nameof(Unk25)}: {Unk25}");
            Console.WriteLine($"{nameof(Unk26)}: {Unk26}");
            /**/


            Console.WriteLine($"{nameof(Description)}: {Description?.Text}");
            Console.WriteLine($"{nameof(DisplayedUnit)}: {DisplayedUnit?.Text}");
            Console.WriteLine($"{nameof(Description2)}: {Description2?.Text}");
            Console.WriteLine($"Type: {GetDataType()}");
            Console.WriteLine($"{nameof(DataType)}: {DataType}");
            Console.WriteLine($"{nameof(FixedLength)}: {FixedLength}");
            Console.WriteLine($"Scales: {Scales}");

            Console.WriteLine("Presentation end");
        }

        public string CopyMinDebug()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("PRES: ");
            sb.Append($" {nameof(ChoiceAddress)}: {ChoiceAddress}");
            sb.Append($" {nameof(NumberChoices)}: {NumberChoices}");
            sb.Append($" {nameof(ScaledMaximum)}: {ScaledMaximum}");
            sb.Append($" {nameof(Unk8)}: {Unk8}");
            sb.Append($" {nameof(Unk9)}: {Unk9}");
            sb.Append($" {nameof(ScaledMinimum)}: {ScaledMinimum}");
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
            sb.Append($" {nameof(Signed)}: {Signed}");
            sb.Append($" {nameof(ByteOrder)}: {ByteOrder}");
            sb.Append($" {nameof(MinLength)}: {MinLength}");
            sb.Append($" {nameof(MaxLength)}: {MaxLength}");
            sb.Append($" {nameof(HexDumpDataLength)}: {HexDumpDataLength}");
            sb.Append($" {nameof(Unk23)}: {Unk23}");
            sb.Append($" {nameof(Unk24)}: {Unk24}");
            sb.Append($" {nameof(Unk25)}: {Unk25}");
            sb.Append($" {nameof(Unk26)}: {Unk26}");
            sb.Append($" {nameof(AbsoluteAddress)}: 0x{AbsoluteAddress:X8}");
            sb.Append($" {nameof(DataType)}: {DataType}");
            sb.Append($" {nameof(FixedLength)}: {FixedLength}");
            sb.Append($" Type: {GetDataType()}");
            sb.Append($" {nameof(Scales)}: {Scales}");
            sb.Append($" {nameof(Qualifier)}: {Qualifier}");
            return sb.ToString();
        }

        protected override bool ReadHeader(CaesarReader reader)
        {
            RelativeAddress = reader.ReadInt32();
            DataSize = reader.ReadInt32();
            return true;
        }

        protected override void ReadData(CaesarReader reader, CaesarContainer container)
        {
            if (ParentObject == null)
            {
                return;
            }
            Bitflags = reader.ReadUInt32();

            Bitflags |= (ulong)reader.ReadUInt16() << 32;

            Qualifier = reader.ReadBitflagStringWithReader(ref Bitflags, AbsoluteAddress);

            Description = reader.ReadBitflagStringRef(ref Bitflags, container);

            Scales = reader.ReadBitflagSubTable<Scale>(this, container);

            ChoiceAddress = reader.ReadBitflagInt32(ref Bitflags);
            NumberChoices = reader.ReadBitflagInt32(ref Bitflags);

            ScaledMaximum = reader.ReadBitflagFloat(ref Bitflags) ?? float.MaxValue;
            Unk8 = reader.ReadBitflagInt32(ref Bitflags);

            Unk9 = reader.ReadBitflagInt32(ref Bitflags);
            ScaledMinimum = reader.ReadBitflagFloat(ref Bitflags) ?? float.MinValue;
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
            FixedLength = reader.ReadBitflagInt32(ref Bitflags);
            int intTypeTmp = reader.ReadBitflagInt8(ref Bitflags) ?? 0;
            
            if(!Enum.IsDefined(typeof(InternalDataType), intTypeTmp))
            {
                throw new ArgumentException();
            }
            InternalDataType = (InternalDataType)intTypeTmp;
            DataType = (DataType)(reader.ReadBitflagInt8(ref Bitflags) ?? 0);

            Unk1d = reader.ReadBitflagInt8(ref Bitflags);
            int signBit = (reader.ReadBitflagInt8(ref Bitflags) ?? 0);
            if(signBit > 1)
            {
                throw new ArgumentException();
            }
            Signed = signBit != 0;
             
            ByteOrder = (Caesar.Enums.ByteOrder)(reader.ReadBitflagInt8(ref Bitflags) ?? 0);

            // for variable-length presentations, these next two items detail the possible size
            MinLength = reader.ReadBitflagInt32(ref Bitflags);
            MaxLength = reader.ReadBitflagInt32(ref Bitflags);

            HexDumpDataLength = reader.ReadBitflagInt32(ref Bitflags);
            Unk23 = reader.ReadBitflagInt16(ref Bitflags);
            Unk24 = reader.ReadBitflagInt32(ref Bitflags);

            Unk25 = reader.ReadBitflagInt32(ref Bitflags);
            Unk26 = reader.ReadBitflagInt32(ref Bitflags);

            if (ChoiceAddress != null && NumberChoices != null)
            {
                Choices = new List<Choice>();
                reader.BaseStream.Seek(AbsoluteAddress + (int)ChoiceAddress, SeekOrigin.Begin);
                for (int i = 0; i < NumberChoices; i++)
                {
                    int choiceValue = reader.ReadInt32();
                    CaesarStringReference choiceText = reader.ReadStringRef(container);
                    Choices.Add(new Choice(choiceValue, choiceText));
                }
            }
        }
    }
}
