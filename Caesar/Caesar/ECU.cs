using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Caesar
{
    public class ECU : CaesarObject
    {
        public string? Qualifier;
        public CaesarStringReference? EcuName;
        public CaesarStringReference? EcuDescription;
        public string? EcuXmlVersion;
        public CaesarTable<ECUInterface>? ECUInterfaces;
        public CaesarTable<ECUInterfaceSubtype>? ECUInterfaceSubtypes;
        public string? EcuInitializationDiagServiceName;
        public string? UnkStr7;
        public string? UnkStr8;

        public int? IgnitionRequired;
        public int? Unk2;
        public int? UnkBlockCount;
        public int? UnkBlockOffset;
        public int? EcuSgmlSource;
        public int? Unk6RelativeOffset;

        public CaesarTable<ECUVariant>? ECUVariants;

        public CaesarTable<DiagService>? GlobalDiagServices;

        public CaesarTable<DTC>? GlobalDTCs;

        public CaesarTable<EnvironmentContext>? GlobalEnvironmentContexts;

        public CaesarTable<VCDomain>? GlobalVCDs;

        public CaesarTable<DiagPresentation>? GlobalPresentations;

        public CaesarTable<DiagPresentation>? GlobalPrepPresentations;

        private int? Unk_BlockOffset;
        private int? Unk_EntryCount;
        private int? Unk_EntrySize;
        private int? Unk_BlockSize;

        public int? Unk39;

        [System.Text.Json.Serialization.JsonIgnore]
        public CTFLanguage Language;

        [System.Text.Json.Serialization.JsonIgnore]
        public CFFHeader CFFHeader;

        byte[] cachedUnkPool = new byte[] { };

        public void Restore(CTFLanguage language, CaesarContainer parentContainer)
        {
            Language = language;

            if (GlobalVCDs != null)
            {
                foreach (VCDomain vc in GlobalVCDs.GetObjects())
                {
                    vc.Restore(language, this);
                }
            }
            if(ECUInterfaceSubtypes != null)
            {
                foreach (ECUInterfaceSubtype iface in ECUInterfaceSubtypes.GetObjects())
                {
                    iface.Restore(language);
                }
            }
            if (ECUVariants != null)
            {
                foreach (ECUVariant variant in ECUVariants.GetObjects())
                {
                    variant.Restore(language, this);
                }
            }

            GlobalDiagServices?.GetObjects().ForEach(ds => ds.Restore(language, this));

            GlobalEnvironmentContexts?.GetObjects().ForEach(ds => ds.Restore(language, this));
        }

        public byte[] ReadECUUnkPool(BinaryReader reader)
        {
            if (cachedUnkPool.Length == 0 && Unk_BlockOffset != null && Unk_EntryCount != null && Unk_EntrySize != null)
            {
                cachedUnkPool = ReadEcuPool(reader, (int)Unk_BlockOffset, (int)Unk_EntryCount, (int)Unk_EntrySize);
            }
            return cachedUnkPool;
        }

        public byte[] ReadEcuPool(BinaryReader reader, int addressToReadFrom, int multiplier1, int multiplier2)
        {
            reader.BaseStream.Seek(addressToReadFrom, SeekOrigin.Begin);
            return reader.ReadBytes(multiplier1 * multiplier2);
        }

        public ECU()
        {
            GlobalDTCs = new CaesarTable<DTC>();
            GlobalEnvironmentContexts = new CaesarTable<EnvironmentContext>();
            Language = new CTFLanguage();
            CFFHeader = new CFFHeader();
        }

        //public void CreateUnk(BinaryReader reader, CTFLanguage language)
        //{
        //    byte[] unkPool = ReadECUUnkPool(reader);
        //    using (BinaryReader poolReader = new BinaryReader(new MemoryStream(unkPool)))
        //    {
        //        for (int unkIndex = 0; unkIndex < Unk_EntryCount; unkIndex++)
        //        {
        //        }
        //    }
        //}

        public override string ToString()
        {
            return $"ECU: Name={Qualifier}, " +
                $"{nameof(EcuName)}={EcuName}, " +
                $"{nameof(EcuDescription)}={EcuDescription}, " +
                $"ecuXmlVersion={EcuXmlVersion}, " +
                $"{nameof(ECUInterfaces)}={ECUInterfaces}, " +
                $"{nameof(ECUInterfaceSubtypes)}={ECUInterfaceSubtypes}, " +
                $"EcuInitializationDiagServiceName: {EcuInitializationDiagServiceName}, " +
                $"euIdk7={UnkStr7}, " +
                $"ecuIdk8={UnkStr8}, " +
                $"{nameof(IgnitionRequired)}={IgnitionRequired}, " +
                $"{nameof(Unk2)}={Unk2}, " +
                $"{nameof(UnkBlockCount)}={UnkBlockCount}, " +
                $"{nameof(UnkBlockOffset)}={UnkBlockOffset}, " +
                $"{nameof(EcuSgmlSource)}={EcuSgmlSource}, " +
                $"{nameof(Unk6RelativeOffset)}=0x{Unk6RelativeOffset:X}, " +

                $"{nameof(ECUVariants)}={ECUVariants}, " +
                $"{nameof(GlobalDiagServices)}={GlobalDiagServices}, " +
                $"{nameof(GlobalDTCs)}={GlobalDTCs}, " +
                $"{nameof(GlobalEnvironmentContexts)}={GlobalEnvironmentContexts}, " +

                // Console.WriteLine("--- bitflag load 2 ---");

                //$"{nameof(Env_BlockSize)}=0x{Env_BlockSize:X}, " +
                $"{nameof(GlobalVCDs)}={GlobalVCDs:X}, " +
                $"{nameof(GlobalPresentations)}={GlobalPresentations}, " +
                $"{nameof(GlobalPrepPresentations)}={GlobalPrepPresentations}, " +
                $"{nameof(Unk_BlockOffset)}=0x{Unk_BlockOffset:X}, " +
                $"{nameof(Unk_EntryCount)}={Unk_EntryCount}, " +
                $"{nameof(Unk_EntrySize)}={Unk_EntrySize}, " +
                $"{nameof(Unk_BlockSize)}={Unk_BlockSize}, " +
                $"{nameof(Unk39)}={Unk39}";
        }

        protected override void ReadData(CaesarReader reader, CaesarContainer container)
        {
            CFFHeader = GetParentByType<CFFHeader>() ?? new CFFHeader();
            // Read 32+16 bits
            Bitflags = reader.ReadUInt32();
            Bitflags |= (ulong)reader.ReadUInt16() << 32;

            // Console.WriteLine($"ECU bitflags: {ecuBitFlags:X}");

            // advancing forward to ecuBase + 10
            int ecuHdrIdk1 = reader.ReadInt32(); // no idea
            // Console.WriteLine($"Skipping: {ecuHdrIdk1:X8}");

            Qualifier = reader.ReadBitflagStringWithReader(ref Bitflags, AbsoluteAddress);
            EcuName = reader.ReadBitflagStringRef(ref Bitflags, container);
            EcuDescription = reader.ReadBitflagStringRef(ref Bitflags, container);
            EcuXmlVersion = reader.ReadBitflagStringWithReader(ref Bitflags, AbsoluteAddress);
            ECUInterfaces = reader.ReadBitflagSubTableAlt<ECUInterface>(this, container);
            ECUInterfaceSubtypes = reader.ReadBitflagSubTableAlt<ECUInterfaceSubtype>(this, container);
            EcuInitializationDiagServiceName = reader.ReadBitflagStringWithReader(ref Bitflags, AbsoluteAddress);
            UnkStr7 = reader.ReadBitflagStringWithReader(ref Bitflags, AbsoluteAddress);
            UnkStr8 = reader.ReadBitflagStringWithReader(ref Bitflags, AbsoluteAddress);

            // Console.WriteLine($"{nameof(dataBufferOffsetRelativeToFile)} : 0x{dataBufferOffsetRelativeToFile:X}");

            IgnitionRequired = reader.ReadBitflagInt16(ref Bitflags);
            Unk2 = reader.ReadBitflagInt16(ref Bitflags);
            UnkBlockCount = reader.ReadBitflagInt16(ref Bitflags);
            UnkBlockOffset = reader.ReadBitflagInt32(ref Bitflags);
            EcuSgmlSource = reader.ReadBitflagInt16(ref Bitflags);
            Unk6RelativeOffset = reader.ReadBitflagInt32(ref Bitflags);

            int oldAddress = AbsoluteAddress;
            int dataBufferOffsetRelativeToFile = (CFFHeader.StringPoolSize ?? 0) + StubHeader.StubHeaderSize + CFFHeader.CffHeaderSize + 4;
            AbsoluteAddress = (int)dataBufferOffsetRelativeToFile;

            ECUVariants = reader.ReadBitflagTable<ECUVariant>(this, container);

            GlobalDiagServices = reader.ReadBitflagTable<DiagService>(this, container);

            GlobalDTCs = reader.ReadBitflagTable<DTC>(this, container);

            GlobalEnvironmentContexts = reader.ReadBitflagTable<EnvironmentContext>(this, container);

            GlobalVCDs = reader.ReadBitflagTable<VCDomain>(this, container);

            GlobalPresentations = reader.ReadBitflagTable<DiagPresentation>(this, container);

            GlobalPrepPresentations = reader.ReadBitflagTable<DiagPresentation>(this, container);

            Unk_BlockOffset = reader.ReadBitflagInt32(ref Bitflags) + dataBufferOffsetRelativeToFile;
            Unk_EntryCount = reader.ReadBitflagInt32(ref Bitflags);
            Unk_EntrySize = reader.ReadBitflagInt32(ref Bitflags);
            Unk_BlockSize = reader.ReadBitflagInt32(ref Bitflags);

            Unk39 = reader.ReadBitflagInt32(ref Bitflags);
            //PrintDebug();
        }
    }
}

/*
 example env records

ENV_48_Data_Record_1_StdEnvData
ENV_56_StdEnv_OccurenceFlag
ENV_64_StdEnv_OriginalOdometerValue
ENV_80_StdEnv_MostRecentOdometerValue
ENV_96_StdEnv_FrequencyCounter
ENV_104_StdEnv_OperationCycleCounter
ENV_112_Data_Record_2_CommonEnvData
ENV_120_CommonEnv_StorageSequence
ENV_128_Data_Record_3_First_Occurrence
ENV_136_First_SIGNALS_xSet1
ENV_136_First_SIGNALS_xSet2
*/