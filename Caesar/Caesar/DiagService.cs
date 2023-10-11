using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Caesar
{

    // DIAGJOB *__cdecl DIOpenDiagService(DI_ECUINFO *ecuHandle, char *serviceName, int ecuErrors)
    public class DiagService : CaesarObject
    {
        /*
    5	DT	DATA
    7	DL	DOWNLOAD
    10	FN|DNU	DIAGNOSTIC_U, FN
    19	DJ	DIAGNOSTIC_JOB
    21	SES	SESSION
    22	DT_STO	STORED DATA
    23	RT	ROUTINE
    24	IOC	IO CONTROL
    26	WVC	VARIANTCODING WRITE
    27	WVC	VARIANTCODING READ

         */
        public enum ServiceType
        {
            Data = 5,
            Download = 7,
            DiagnosticFunction = 10,
            DiagnosticJob = 19,
            Session = 21,
            StoredData = 22,
            Routine = 23,
            IoControl = 24,
            VariantCodingWrite = 26,
            VariantCodingRead = 27,
        }

        public int DataSize { get; set; }

        public string? Qualifier;

        public CaesarStringReference? Name;
        public CaesarStringReference? Description;

        public ushort? DataClass_ServiceType;
        public int? DataClass_ServiceTypeShifted;

        public ushort? IsExecutable;
        public ushort? ClientAccessLevel;
        public ushort? SecurityAccessLevel;

        public CaesarTable<ComParameter>? DiagComParameters;
        
        private int? Q_Count;
        private int? Q_Offset;

        private int? R_Count;
        private int? R_Offset;

        public string? InputRefNameMaybe;

        private int? V_Count;
        private int? V_Offset;

        public byte[]? RequestBytes;

        public ushort? Field50;

        public string? NegativeResponseName;
        public string? UnkStr3;
        public string? UnkStr4;

        public int? P_Count; // global vars?
        public int? P_Offset;

        private int? DiagServiceCodeCount;
        private int? DiagServiceCodeOffset;

        private int? S_Count;
        private int? S_Offset;

        private int? X_Count;
        private int? X_Offset;

        private int? Y_Count;
        private int? Y_Offset;

        private int? Z_Count;
        private int? Z_Offset;



        // these are inlined preparations
        public CaesarTable<DiagPreparation>? InputPreparations;
        public CaesarTable<DiagOutPreparationList>? OutputPreparations;

        [Newtonsoft.Json.JsonIgnore]
        public ECU ParentECU;

        public DiagService() 
        {
            RequestBytes = new byte[0];
            ParentECU = new ECU();
        }

        public long GetCALInt16Offset(CaesarReader reader) 
        {
            if (ParentObject == null) return 0;

            reader.BaseStream.Seek(AbsoluteAddress + ParentObject.AbsoluteAddress, SeekOrigin.Begin);

            Bitflags = reader.ReadUInt32();
            Bitflags |= (ulong)reader.ReadUInt32() << 32;

            reader.ReadBitflagStringWithReader(ref Bitflags, AbsoluteAddress); // Qualifier
            reader.ReadBitflagInt32(ref Bitflags); // Name
            reader.ReadBitflagInt32(ref Bitflags); // Description
            reader.ReadBitflagUInt16(ref Bitflags); // Type
            reader.ReadBitflagUInt16(ref Bitflags); // IsExecutable 
            if (reader.CheckAndAdvanceBitflag(ref Bitflags))
            {
                return reader.BaseStream.Position;
            }
            else 
            {
                return -1;
            }
        }


        public void PrintDebug() 
        {
            Console.WriteLine($"{Qualifier} - ReqBytes: {RequestBytes}, P: {P_Count}, Q: {Q_Count}, R: {R_Count}, S: {S_Count}, ComParams: {DiagComParameters}, Preparation: {InputPreparations}, V: {V_Count}, OutPres: {OutputPreparations}, X: {X_Count}, Y: {Y_Count}, Z: {Z_Count}, DSC {DiagServiceCodeCount}, field50: {Field50}");
            Console.WriteLine($"BaseAddress @ 0x{AbsoluteAddress:X}, NR: {NegativeResponseName}");
            Console.WriteLine($"V @ 0x{AbsoluteAddress + V_Offset:X}, count: {V_Count}");
        }

        protected override bool ReadHeader(CaesarReader reader)
        {
            RelativeAddress = reader.ReadInt32();
            DataSize = reader.ReadInt32();
            uint crc = reader.ReadUInt32();
            uint config = reader.ReadUInt16();

            return true;
        }

        public void Restore(CTFLanguage language, ECU parentEcu)
        {
            ParentECU = parentEcu;
            if (InputPreparations != null)
            {
                foreach (var prep in InputPreparations.GetObjects())
                {
                    prep.Restore(this, parentEcu);
                }
            }
            if (OutputPreparations != null)
            {
                foreach (var prep in OutputPreparations.GetObjects().SelectMany(lst => lst.GetObjects()))
                {
                    prep.Restore(this, parentEcu);
                }
            }
        }

        protected override void ReadData(CaesarReader reader, CaesarContainer container)
        {
            ParentECU = GetParentByType<ECU>() ?? new ECU();

            Bitflags = reader.ReadUInt32();
            Bitflags |= (ulong)reader.ReadUInt32() << 32;

            Qualifier = reader.ReadBitflagStringWithReader(ref Bitflags, AbsoluteAddress);

            Name = reader.ReadBitflagStringRef(ref Bitflags, container);
            Description = reader.ReadBitflagStringRef(ref Bitflags, container);

            DataClass_ServiceType = reader.ReadBitflagUInt16(ref Bitflags);
            DataClass_ServiceTypeShifted = 1 << (DataClass_ServiceType - 1);

            IsExecutable = reader.ReadBitflagUInt16(ref Bitflags);
            ClientAccessLevel = reader.ReadBitflagUInt16(ref Bitflags);
            SecurityAccessLevel = reader.ReadBitflagUInt16(ref Bitflags);

            DiagComParameters = reader.ReadBitflagSubTableAlt<ComParameter>(this, container);

            Q_Count = reader.ReadBitflagInt32(ref Bitflags);
            Q_Offset = reader.ReadBitflagInt32(ref Bitflags);

            R_Count = reader.ReadBitflagInt32(ref Bitflags);
            R_Offset = reader.ReadBitflagInt32(ref Bitflags);

            InputRefNameMaybe = reader.ReadBitflagStringWithReader(ref Bitflags, AbsoluteAddress);

            InputPreparations = reader.ReadBitflagSubTableAlt<DiagPreparation>(this, container);
            
            // array of DWORDs, probably reference to elsewhere
            V_Count = reader.ReadBitflagInt32(ref Bitflags);
            V_Offset = reader.ReadBitflagInt32(ref Bitflags);

            int? reqBytesCount = reader.ReadBitflagInt16(ref Bitflags);
            RequestBytes = reader.ReadBitflagDumpWithReader(ref Bitflags, reqBytesCount);

            // FIXME: run it through the entire dbr cbf directory, check if any file actually has more than 1 item in ResultPresentationSet
            OutputPreparations = reader.ReadBitflagSubTableAlt<DiagOutPreparationList>(this, container);


            Field50 = reader.ReadBitflagUInt16(ref Bitflags);

            NegativeResponseName = reader.ReadBitflagStringWithReader(ref Bitflags, AbsoluteAddress); // negative response name
            UnkStr3 = reader.ReadBitflagStringWithReader(ref Bitflags, AbsoluteAddress);
            UnkStr4 = reader.ReadBitflagStringWithReader(ref Bitflags, AbsoluteAddress);

            P_Count = reader.ReadBitflagInt32(ref Bitflags);
            P_Offset = reader.ReadBitflagInt32(ref Bitflags);

            DiagServiceCodeCount = reader.ReadBitflagInt32(ref Bitflags);
            DiagServiceCodeOffset = reader.ReadBitflagInt32(ref Bitflags);

            S_Count = reader.ReadBitflagInt16(ref Bitflags);
            S_Offset = reader.ReadBitflagInt32(ref Bitflags);

            X_Count = reader.ReadBitflagInt32(ref Bitflags);
            X_Offset = reader.ReadBitflagInt32(ref Bitflags);

            Y_Count = reader.ReadBitflagInt32(ref Bitflags);
            Y_Offset = reader.ReadBitflagInt32(ref Bitflags);

            Z_Count = reader.ReadBitflagInt32(ref Bitflags);
            Z_Offset = reader.ReadBitflagInt32(ref Bitflags);

            // DJ_Zugriffsberechtigung_Abgleich
            // DJ_Zugriffsberechtigung
            // DT_Abgasklappe_kontinuierlich
            // FN_HardReset
            // WVC_Implizite_Variantenkodierung_Write

            // NR_Disable_Resp_required noexec
            // DT_Laufzeiten_Resetzaehler_nicht_implementiert exec
            /*
            if (false && qualifierName.Contains("RVC_SCN_Variantencodierung_VGS_73_Lesen"))
            {

                Console.WriteLine($"{nameof(field50)} : {field50}");
                Console.WriteLine($"{nameof(IsExecutable)} : {IsExecutable} {IsExecutable != 0}");
                Console.WriteLine($"{nameof(AccessLevel)} : {AccessLevel}");
                Console.WriteLine($"{nameof(SecurityAccessLevel)} : {SecurityAccessLevel}");
                Console.WriteLine($"{nameof(DataClass)} : {DataClass}");



                Console.WriteLine($"{qualifierName} - ReqBytes: {RequestBytes_Count}, P: {P_Count}, Q: {Q_Count}, R: {R_Count}, S: {S_Count}, T: {T_Count}, Preparation: {U_prep_Count}, V: {V_Count}, W: {W_Count}, X: {X_Count}, Y: {Y_Count}, Z: {Z_Count}, DSC {DiagServiceCodeCount}");
                Console.WriteLine($"at 0x{baseAddress:X}, W @ 0x{W_Offset:X}, DSC @ 0x{DiagServiceCodeOffset:X}");
                Console.WriteLine($"ReqBytes: {BitUtility.BytesToHex(RequestBytes)}");
            }
            */
            //Console.WriteLine($"{qualifierName} - O: {RequestBytes_Count}, P: {P_Count}, Q: {Q_Count}, R: {R_Count}, S: {S_Count}, T: {T_Count}, U: {U_Count}, V: {V_Count}, W: {W_Count}, X: {X_Count}, Y: {Y_Count}, Z: {Z_Count}, DSC {DiagServiceCodeCount}");


            byte[] dscPool = ParentECU.CFFHeader.DSCPool;
            if (DiagServiceCodeOffset != null && DiagServiceCodeCount != null)
            {
                long dscTableBaseAddress = AbsoluteAddress + (long)DiagServiceCodeOffset;

                using (BinaryReader dscPoolReader = new BinaryReader(new MemoryStream(dscPool)))
                {
                    for (int dscIndex = 0; dscIndex < DiagServiceCodeCount; dscIndex++)
                    {
                        reader.BaseStream.Seek(dscTableBaseAddress + (4 * dscIndex), SeekOrigin.Begin);
                        long dscEntryBaseAddress = reader.ReadInt32() + dscTableBaseAddress;
                        reader.BaseStream.Seek(dscEntryBaseAddress, SeekOrigin.Begin);

                        ulong dscEntryBitflags = reader.ReadUInt16();
                        uint? idk1 = reader.ReadBitflagUInt8(ref dscEntryBitflags);
                        uint? idk2 = reader.ReadBitflagUInt8(ref dscEntryBitflags);
                        int? dscPoolOffset = reader.ReadBitflagInt32(ref dscEntryBitflags);
                        string? dscQualifier = reader.ReadBitflagStringWithReader(ref dscEntryBitflags, dscEntryBaseAddress);

                        if (dscPoolOffset != null)
                        {
                            dscPoolReader.BaseStream.Seek((int)dscPoolOffset * 8, SeekOrigin.Begin);
                            long dscRecordOffset = dscPoolReader.ReadInt32() + ParentECU.CFFHeader.DscBlockOffset;
                            int dscRecordSize = dscPoolReader.ReadInt32();

                            reader.BaseStream.Seek(dscRecordOffset, SeekOrigin.Begin);

                            // Console.WriteLine($"DSC {qualifierName} @ 0x{dscTableBaseAddress:X8} {idk1}/{idk2} pool @ 0x{dscPoolOffset:X}, name: {dscQualifier}");
                            byte[] dscBytes = reader.ReadBytes(dscRecordSize);
#if DEBUG
                            //string dscName = $"{parentEcu.Qualifier}_{Qualifier}_{dscIndex}.pal";
                            //Console.WriteLine($"Exporting DSC: {dscName}");
                            //File.WriteAllBytes(dscName, dscBytes);
#endif
                            // at this point, the DSC binary is available in dscBytes, intended for use in DSCContext (but is currently unimplemented)
                            // Console.WriteLine($"DSC actual at 0x{dscRecordOffset:X}, size=0x{dscRecordSize:X}\n");
                        }
                    }

                }
            }
        }
    }
}




/*
// originally EnvironmentContext, removed because of overlap
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Caesar
{
    public class EnvironmentContext
    {
        // see : const char *__cdecl DIGetComfortErrorCode(DI_ECUINFO *ecuh, unsigned int dtcIndex)
        public string Qualifier;

        public long BaseAddress;
        public int PoolIndex;
        public int Name_CTF;
        public int Description_CTF;
        public int ServiceTypeMaybe;
        public int AccessLevelType_Maybe;
        public int AccessLevelType_Maybe2;
        public int PresentationTableCount;
        public int PresentationTableOffset;
        public int PresentationTableRowSize_Maybe; // see diagservice for similar layout, seems unused (uint16)

        public ECU ParentECU;
        CTFLanguage Language;

        public EnvironmentContext(BinaryReader reader, CTFLanguage language, long baseAddress, int poolIndex, ECU parentEcu)
        {
            ParentECU = parentEcu;
            PoolIndex = poolIndex;
            BaseAddress = baseAddress;
            Language = language;
            reader.BaseStream.Seek(baseAddress, SeekOrigin.Begin);

            // layout seems very similar to DiagService
            ulong bitflags = reader.ReadUInt32();
            ulong bitflagsExtended = reader.ReadUInt32();

            Qualifier = CaesarReader.ReadBitflagStringWithReader(ref bitflags, reader, baseAddress);
            Name_CTF = CaesarReader.ReadBitflagInt32(ref bitflags, reader, -1);
            Description_CTF = CaesarReader.ReadBitflagInt32(ref bitflags, reader, -1);

            ServiceTypeMaybe = CaesarReader.ReadBitflagInt16(ref bitflags, reader);
            AccessLevelType_Maybe = CaesarReader.ReadBitflagInt16(ref bitflags, reader);
            AccessLevelType_Maybe2 = CaesarReader.ReadBitflagInt16(ref bitflags, reader);

            // doesn't seem to be set for any files in my library
            for (int i = 0; i < 14; i++) 
            {
                if (CaesarReader.CheckAndAdvanceBitflag(ref bitflags))
                {
                    throw new Exception("Sorry, The parser for EnvironmentContext has encountered an unknown bitflag; please open an issue and indicate your CBF file name.");
                }
            }

            // these describe the table to the presentation
            PresentationTableCount = CaesarReader.ReadBitflagInt32(ref bitflags, reader);
            PresentationTableOffset = CaesarReader.ReadBitflagInt32(ref bitflags, reader);
            PresentationTableRowSize_Maybe = CaesarReader.ReadBitflagInt16(ref bitflags, reader);

            // ... looks like DiagService?!
        }

        public void PrintDebug()
        {
            Console.WriteLine(Qualifier);
        }
    }
}
*/