using Newtonsoft.Json;
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
        public int? InterfaceBlockCount;
        public int? InterfaceTableOffset;
        public int? SubinterfacesCount;
        public int? SubinterfacesOffset;
        public string? EcuClassName;
        public string? UnkStr7;
        public string? UnkStr8;

        public int? IgnitionRequired;
        public int? Unk2;
        public int? UnkBlockCount;
        public int? UnkBlockOffset;
        public int? EcuSgmlSource;
        public int? Unk6RelativeOffset;

        private int? EcuVariant_BlockOffset; // 1
        private int? EcuVariant_EntryCount;
        private int? EcuVariant_EntrySize;
        private int? EcuVariant_BlockSize;

        public CaesarLargeTable<DiagService>? GlobalDiagServices;

        public CaesarLargeTable<DTC>? GlobalDTCs;

        public CaesarLargeTable<EnvironmentContext>? GlobalEnvironmentContexts;

        private int? VcDomain_BlockOffset; // 5 , 0x15716
        private int? VcDomain_EntryCount; // [1], 43 0x2B
        private int? VcDomain_EntrySize; // [2], 12 0xC (multiply with [1] for size), 43*12=516 = 0x204
        private int? VcDomain_BlockSize; // [3] unused

        public CaesarLargeTable<DiagPresentation>? GlobalPresentations;

        public CaesarLargeTable<DiagPresentation>? GlobalInternalPresentations;

        private int? Unk_BlockOffset;
        private int? Unk_EntryCount;
        private int? Unk_EntrySize;
        private int? Unk_BlockSize;

        public int? Unk39;

        public List<ECUInterface> ECUInterfaces = new List<ECUInterface>();
        public List<ECUInterfaceSubtype> ECUInterfaceSubtypes = new List<ECUInterfaceSubtype>();
        public List<ECUVariant> ECUVariants = new List<ECUVariant>();

        public List<VCDomain> GlobalVCDs = new List<VCDomain>();

        private long BaseAddress;
        [Newtonsoft.Json.JsonIgnore]
        public CTFLanguage Language;

        [Newtonsoft.Json.JsonIgnore]
        public CaesarContainer ParentContainer;

        byte[] cachedVarcodingPool = new byte[] { };
        byte[] cachedVariantPool = new byte[] { };
        byte[] cachedDiagjobPool = new byte[] { };
        byte[] cachedEcuInfoPool = new byte[] { };
        byte[] cachedPresentationsPool = new byte[] { };
        byte[] cachedInternalPresentationsPool = new byte[] { };
        byte[] cachedUnkPool = new byte[] { };

        public void Restore(CTFLanguage language, CaesarContainer parentContainer)
        {
            Language = language;
            ParentContainer = parentContainer;
            foreach (VCDomain vc in GlobalVCDs)
            {
                vc.Restore(language, this);
            }
            foreach (ECUInterface iface in ECUInterfaces)
            {
                iface.Restore(language);
            }
            foreach (ECUInterfaceSubtype iface in ECUInterfaceSubtypes)
            {
                iface.Restore(language);
            }
            foreach (ECUVariant variant in ECUVariants)
            {
                variant.Restore(language, this);
            }
        }

        public byte[] ReadVariantPool(BinaryReader reader)
        {
            if (cachedVariantPool.Length == 0 && EcuVariant_BlockOffset != null && EcuVariant_EntryCount != null && EcuVariant_EntrySize != null)
            {
                cachedVariantPool = ReadEcuPool(reader, (int)EcuVariant_BlockOffset, (int)EcuVariant_EntryCount, (int)EcuVariant_EntrySize);
            }
            return cachedVariantPool;
        }

        public byte[] ReadVarcodingPool(BinaryReader reader)
        {
            if (cachedVarcodingPool.Length == 0 && VcDomain_BlockOffset != null && VcDomain_EntryCount != null && VcDomain_EntrySize != null)
            {
                cachedVarcodingPool = ReadEcuPool(reader, (int)VcDomain_BlockOffset, (int)VcDomain_EntryCount, (int)VcDomain_EntrySize);
            }
            return cachedVarcodingPool;
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
            GlobalDTCs = new CaesarLargeTable<DTC>();
            GlobalEnvironmentContexts = new CaesarLargeTable<EnvironmentContext>();
            Language = new CTFLanguage();
            ParentContainer = new CaesarContainer();
        }

        public ECU(CaesarReader reader, CTFLanguage language, CFFHeader header, long baseAddress, CaesarContainer parentContainer)
        {
            ParentContainer = parentContainer;
            BaseAddress = baseAddress;
            RelativeAddress = (int)BaseAddress;
            Language = language;
            // Read 32+16 bits
            Bitflags = reader.ReadUInt32();
            Bitflags |= (ulong)reader.ReadUInt16() << 32;

            // Console.WriteLine($"ECU bitflags: {ecuBitFlags:X}");

            // advancing forward to ecuBase + 10
            int ecuHdrIdk1 = reader.ReadInt32(); // no idea
            // Console.WriteLine($"Skipping: {ecuHdrIdk1:X8}");

            Qualifier = reader.ReadBitflagStringWithReader(ref Bitflags, BaseAddress);
            EcuName = reader.ReadBitflagStringRef(ref Bitflags, language);
            EcuDescription = reader.ReadBitflagStringRef(ref Bitflags, language);
            EcuXmlVersion = reader.ReadBitflagStringWithReader(ref Bitflags, BaseAddress);
            InterfaceBlockCount = reader.ReadBitflagInt32(ref Bitflags);
            InterfaceTableOffset = reader.ReadBitflagInt32(ref Bitflags);
            SubinterfacesCount = reader.ReadBitflagInt32(ref Bitflags);
            SubinterfacesOffset = reader.ReadBitflagInt32(ref Bitflags);
            EcuClassName = reader.ReadBitflagStringWithReader(ref Bitflags, BaseAddress);
            UnkStr7 = reader.ReadBitflagStringWithReader(ref Bitflags, BaseAddress);
            UnkStr8 = reader.ReadBitflagStringWithReader(ref Bitflags, BaseAddress);

            // Console.WriteLine($"{nameof(dataBufferOffsetRelativeToFile)} : 0x{dataBufferOffsetRelativeToFile:X}");

            IgnitionRequired = reader.ReadBitflagInt16(ref Bitflags);
            Unk2 = reader.ReadBitflagInt16(ref Bitflags);
            UnkBlockCount = reader.ReadBitflagInt16(ref Bitflags);
            UnkBlockOffset = reader.ReadBitflagInt32(ref Bitflags);
            EcuSgmlSource = reader.ReadBitflagInt16(ref Bitflags);
            Unk6RelativeOffset = reader.ReadBitflagInt32(ref Bitflags);

            int dataBufferOffsetRelativeToFile = (header.StringPoolSize ?? 0) + StubHeader.StubHeaderSize + header.CffHeaderSize + 4;
            AbsoluteAddress = (int)dataBufferOffsetRelativeToFile;

            EcuVariant_BlockOffset = reader.ReadBitflagInt32(ref Bitflags) + dataBufferOffsetRelativeToFile;
            EcuVariant_EntryCount = reader.ReadBitflagInt32(ref Bitflags);
            EcuVariant_EntrySize = reader.ReadBitflagInt32(ref Bitflags); // 10
            EcuVariant_BlockSize = reader.ReadBitflagInt32(ref Bitflags);

            GlobalDiagServices = reader.ReadBitflagTable<DiagService>(this, language, this);

            GlobalDTCs = reader.ReadBitflagTable<DTC>(this, language, this);

            GlobalEnvironmentContexts = reader.ReadBitflagTable<EnvironmentContext>(this, language, this);

            VcDomain_BlockOffset = reader.ReadBitflagInt32(ref Bitflags) + dataBufferOffsetRelativeToFile;
            VcDomain_EntryCount = reader.ReadBitflagInt32(ref Bitflags);
            VcDomain_EntrySize = reader.ReadBitflagInt32(ref Bitflags); // 12
            VcDomain_BlockSize = reader.ReadBitflagInt32(ref Bitflags);

            GlobalPresentations = reader.ReadBitflagTable<DiagPresentation>(this, language, this);

            GlobalInternalPresentations = reader.ReadBitflagTable<DiagPresentation>(this, language, this);

            Unk_BlockOffset = reader.ReadBitflagInt32(ref Bitflags) + dataBufferOffsetRelativeToFile;
            Unk_EntryCount = reader.ReadBitflagInt32(ref Bitflags);
            Unk_EntrySize = reader.ReadBitflagInt32(ref Bitflags);
            Unk_BlockSize = reader.ReadBitflagInt32(ref Bitflags);

            Unk39 = reader.ReadBitflagInt32(ref Bitflags);

            // read ecu's supported interfaces and subtypes

            ECUInterfaces = new List<ECUInterface>();
            if (InterfaceTableOffset != null && InterfaceBlockCount != null)
            {
                // try to read interface block from the interface buffer table
                // this address is relative to the definitions block
                long interfaceTableAddress = BaseAddress + (int)InterfaceTableOffset;
                // Console.WriteLine($"Interface table address: {interfaceTableAddress:X}, given offset: {interfaceTableOffset:X}");

                for (int interfaceBufferIndex = 0; interfaceBufferIndex < InterfaceBlockCount; interfaceBufferIndex++)
                {
                    // Console.WriteLine($"Parsing interface {interfaceBufferIndex + 1}/{interfaceBlockCount}");

                    // find our interface block offset
                    reader.BaseStream.Seek(interfaceTableAddress + (interfaceBufferIndex * 4), SeekOrigin.Begin);
                    // seek to the actual block (ambiguity: is this relative to the interface table or the current array?)
                    int interfaceBlockOffset = reader.ReadInt32();

                    long ecuInterfaceBaseAddress = interfaceTableAddress + interfaceBlockOffset;

                    ECUInterface ecuInterface = new ECUInterface(reader, ecuInterfaceBaseAddress);
                    ECUInterfaces.Add(ecuInterface);
                }
            }
            // try to read interface subtype block from the interface buffer table
            // this address is relative to the definitions block
            ECUInterfaceSubtypes = new List<ECUInterfaceSubtype>();
            if (SubinterfacesOffset != null && SubinterfacesCount != null)
            {
                long ctTableAddress = BaseAddress + (int)SubinterfacesOffset;
                // Console.WriteLine($"Interface subtype table address: {ctTableAddress:X}, given offset: {ecuChildTypesOffset:X}");
                for (int ctBufferIndex = 0; ctBufferIndex < SubinterfacesCount; ctBufferIndex++)
                {
                    // Console.WriteLine($"Parsing interface subtype {ctBufferIndex + 1}/{ecuNumberOfEcuChildTypes}");
                    // find our ct block offset
                    reader.BaseStream.Seek(ctTableAddress + (ctBufferIndex * 4), SeekOrigin.Begin);
                    // seek to the actual block (ambiguity: is this relative to the ct table or the current array?)
                    int actualBlockOffset = reader.ReadInt32();
                    long ctBaseAddress = ctTableAddress + actualBlockOffset;

                    ECUInterfaceSubtype ecuInterfaceSubtype = new ECUInterfaceSubtype(reader, ctBaseAddress, ctBufferIndex, language);
                    ECUInterfaceSubtypes.Add(ecuInterfaceSubtype);
                }
            }
            // dependency of variants
            // requires presentations
            // dtc has xrefs to envs
            CreateVCDomains(reader, language);

            CreateEcuVariants(reader, language);
            //PrintDebug();
        }

        public void CreateVCDomains(CaesarReader reader, CTFLanguage language)
        {
            if (VcDomain_BlockOffset != null && VcDomain_EntryCount != null)
            {
                byte[] vcPool = ReadVarcodingPool(reader);
                VCDomain[] globalVCDs = new VCDomain[(int)VcDomain_EntryCount];
                using (BinaryReader poolReader = new BinaryReader(new MemoryStream(vcPool)))
                {
                    for (int vcdIndex = 0; vcdIndex < VcDomain_EntryCount; vcdIndex++)
                    {
                        int entryOffset = poolReader.ReadInt32();
                        int entrySize = poolReader.ReadInt32();
                        uint entryCrc = poolReader.ReadUInt32();
                        long vcdBlockAddress = entryOffset + (long)VcDomain_BlockOffset;
                        VCDomain vcd = new VCDomain(reader, language, vcdBlockAddress, vcdIndex, this);
                        globalVCDs[vcdIndex] = vcd;
                    }
                }
                GlobalVCDs = new List<VCDomain>(globalVCDs);
            }
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
        public void CreateEcuVariants(CaesarReader reader, CTFLanguage language)
        {
            ECUVariants.Clear();
            if (EcuVariant_BlockOffset != null && EcuVariant_EntryCount != null && EcuVariant_EntrySize != null)
            {
                byte[] ecuVariantPool = ReadVariantPool(reader);

                using (CaesarReader poolReader = new CaesarReader(new MemoryStream(ecuVariantPool)))
                {
                    for (int ecuVariantIndex = 0; ecuVariantIndex < EcuVariant_EntryCount; ecuVariantIndex++)
                    {
                        poolReader.BaseStream.Seek(ecuVariantIndex * (int)EcuVariant_EntrySize, SeekOrigin.Begin);

                        int entryOffset = poolReader.ReadInt32();
                        int entrySize = poolReader.ReadInt32();
                        ushort poolEntryAttributes = poolReader.ReadUInt16();
                        long variantBlockAddress = entryOffset + (long)EcuVariant_BlockOffset;

                        ECUVariant variant = new ECUVariant(reader, this, language, variantBlockAddress, entrySize);
                        ECUVariants.Add(variant);
                        // Console.WriteLine($"Variant Entry @ 0x{entryOffset:X} with size 0x{entrySize:X} and CRC {poolEntryAttributes:X8}, abs addr {variantBlockAddress:X8}");

#if DEBUG
                        int resultLimit = 1999;
                        if (ecuVariantIndex >= resultLimit)
                        {
                            Console.WriteLine($"Breaking prematurely to create only {resultLimit} variant(s) (debug)");
                            break;
                        }
#endif
                    }
                }
            }
        }

        public override string ToString()
        {
            return $"ECU: Name={Qualifier}, " +
                $"{nameof(EcuName)}={EcuName}, " +
                $"{nameof(EcuDescription)}={EcuDescription}, " +
                $"ecuXmlVersion={EcuXmlVersion}, " +
                $"{nameof(InterfaceBlockCount)}={InterfaceBlockCount}, " +
                $"{nameof(InterfaceTableOffset)}=0x{InterfaceTableOffset:X}, " +
                $"{nameof(SubinterfacesCount)}={SubinterfacesCount}, " +
                $"{nameof(SubinterfacesOffset)}={SubinterfacesOffset}, " +
                $"ecuClassName: {EcuClassName}, " +
                $"euIdk7={UnkStr7}, " +
                $"ecuIdk8={UnkStr8}, " +
                $"{nameof(IgnitionRequired)}={IgnitionRequired}, " +
                $"{nameof(Unk2)}={Unk2}, " +
                $"{nameof(UnkBlockCount)}={UnkBlockCount}, " +
                $"{nameof(UnkBlockOffset)}={UnkBlockOffset}, " +
                $"{nameof(EcuSgmlSource)}={EcuSgmlSource}, " +
                $"{nameof(Unk6RelativeOffset)}=0x{Unk6RelativeOffset:X}, " +

                $"{nameof(EcuVariant_BlockOffset)}=0x{EcuVariant_BlockOffset:X}, " +
                $"{nameof(EcuVariant_EntryCount)}={EcuVariant_EntryCount}, " +
                $"{nameof(EcuVariant_EntrySize)}={EcuVariant_EntrySize}, " +
                $"{nameof(EcuVariant_BlockSize)}=0x{EcuVariant_BlockSize:X}, " +
                $"{nameof(GlobalDiagServices)}={GlobalDiagServices}, " +
                $"{nameof(GlobalDTCs)}={GlobalDTCs}, " +
                $"{nameof(GlobalEnvironmentContexts)}={GlobalEnvironmentContexts}, " +

                // Console.WriteLine("--- bitflag load 2 ---");

                //$"{nameof(Env_BlockSize)}=0x{Env_BlockSize:X}, " +
                $"{nameof(VcDomain_BlockOffset)}=0x{VcDomain_BlockOffset:X}, " +
                $"{nameof(VcDomain_EntryCount)}={VcDomain_EntryCount}, " +
                $"{nameof(VcDomain_EntrySize)}={VcDomain_EntrySize}, " +
                $"{nameof(VcDomain_BlockSize)}=0x{VcDomain_BlockSize:X}, " +
                $"{nameof(GlobalPresentations)}={GlobalPresentations}, " +
                $"{nameof(GlobalInternalPresentations)}={GlobalInternalPresentations}, " +
                $"{nameof(Unk_BlockOffset)}=0x{Unk_BlockOffset:X}, " +
                $"{nameof(Unk_EntryCount)}={Unk_EntryCount}, " +
                $"{nameof(Unk_EntrySize)}={Unk_EntrySize}, " +
                $"{nameof(Unk_BlockSize)}={Unk_BlockSize}, " +
                $"{nameof(Unk39)}={Unk39}";
        }

        protected override void ReadData(CaesarReader reader, CTFLanguage language, ECU? currentEcu)
        {
            throw new NotImplementedException();
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
