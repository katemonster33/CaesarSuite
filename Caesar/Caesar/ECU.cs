using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Caesar
{
    public class ECU
    {
        public string? Qualifier;
        public int? EcuName_CTF;
        public int? EcuDescription_CTF;
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

        private int? DiagJob_BlockOffset; // 2
        private int? DiagJob_EntryCount;
        private int? DiagJob_EntrySize;
        private int? DiagJob_BlockSize;

        public CaesarTable<DTC> GlobalDTCs;

        public CaesarTable<DiagService> GlobalEnvironmentContexts;

        private int? VcDomain_BlockOffset; // 5 , 0x15716
        private int? VcDomain_EntryCount; // [1], 43 0x2B
        private int? VcDomain_EntrySize; // [2], 12 0xC (multiply with [1] for size), 43*12=516 = 0x204
        private int? VcDomain_BlockSize; // [3] unused

        private int? Presentations_BlockOffset;
        private int? Presentations_EntryCount;
        private int? Presentations_EntrySize;
        private int? Presentations_BlockSize;

        private int? InternalPresentations_BlockOffset; // 31 (formerly InfoPool)
        private int? InternalPresentations_EntryCount; // 32
        private int? InternalPresentations_EntrySize; // 33
        private int? InternalPresentations_BlockSize; // 34

        private int? Unk_BlockOffset;
        private int? Unk_EntryCount;
        private int? Unk_EntrySize;
        private int? Unk_BlockSize;

        public int? Unk39;

        public List<ECUInterface> ECUInterfaces = new List<ECUInterface>();
        public List<ECUInterfaceSubtype> ECUInterfaceSubtypes = new List<ECUInterfaceSubtype>();
        public List<ECUVariant> ECUVariants = new List<ECUVariant>();

        public List<VCDomain> GlobalVCDs = new List<VCDomain>();
        public List<DiagService> GlobalDiagServices = new List<DiagService>();
        public List<DiagPresentation> GlobalPresentations = new List<DiagPresentation>();
        public List<DiagPresentation> GlobalInternalPresentations = new List<DiagPresentation>();

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

        [Newtonsoft.Json.JsonIgnore]
        public string? ECUDescription { get { return Language.GetString(EcuDescription_CTF); } }


        public void Restore(CTFLanguage language, CaesarContainer parentContainer)
        {
            Language = language;
            ParentContainer = parentContainer;
            foreach (VCDomain vc in GlobalVCDs)
            {
                vc.Restore(language, this);
            }
            foreach (DiagPresentation pres in GlobalPresentations)
            {
                pres.Restore(language);
            }
            foreach (DiagPresentation pres in GlobalInternalPresentations)
            {
                pres.Restore(language);
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

        public byte[] ReadDiagjobPool(BinaryReader reader)
        {
            if (cachedDiagjobPool.Length == 0 && DiagJob_BlockOffset != null && DiagJob_EntryCount != null && DiagJob_EntrySize != null)
            {
                cachedDiagjobPool = ReadEcuPool(reader, (int)DiagJob_BlockOffset, (int)DiagJob_EntryCount, (int)DiagJob_EntrySize);
            }
            return cachedDiagjobPool;
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
        // don't actually know what the proper name is, using "ECUInfo" for now
        public byte[] ReadECUInfoPool(BinaryReader reader)
        {
            if (cachedEcuInfoPool.Length == 0 && InternalPresentations_BlockOffset != null && InternalPresentations_EntryCount != null && InternalPresentations_EntrySize != null)
            {
                cachedEcuInfoPool = ReadEcuPool(reader, (int)InternalPresentations_BlockOffset, (int)InternalPresentations_EntryCount, (int)InternalPresentations_EntrySize);
            }
            return cachedEcuInfoPool;
        }
        public byte[] ReadECUPresentationsPool(BinaryReader reader)
        {
            if (cachedPresentationsPool.Length == 0 && Presentations_BlockOffset != null && Presentations_EntryCount != null && Presentations_EntrySize != null)
            {
                cachedPresentationsPool = ReadEcuPool(reader, (int)Presentations_BlockOffset, (int)Presentations_EntryCount, (int)Presentations_EntrySize);
            }
            return cachedPresentationsPool;
        }
        public byte[] ReadECUInternalPresentationsPool(BinaryReader reader)
        {
            if (cachedInternalPresentationsPool.Length == 0 && InternalPresentations_BlockOffset != null && InternalPresentations_EntryCount != null && InternalPresentations_EntrySize != null)
            {
                cachedInternalPresentationsPool = ReadEcuPool(reader, (int)InternalPresentations_BlockOffset, (int)InternalPresentations_EntryCount, (int)InternalPresentations_EntrySize);
            }
            return cachedInternalPresentationsPool;
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
            GlobalEnvironmentContexts = new CaesarTable<DiagService>();
            Language = new CTFLanguage();
            ParentContainer = new CaesarContainer();
        }

        public ECU(CaesarReader reader, CTFLanguage language, CFFHeader header, long baseAddress, CaesarContainer parentContainer)
        {
            ParentContainer = parentContainer;
            BaseAddress = baseAddress;
            Language = language;
            // Read 32+16 bits
            ulong ecuBitFlags = (ulong)reader.ReadUInt32();
            ecuBitFlags |= (ulong)reader.ReadUInt16() << 32;

            // Console.WriteLine($"ECU bitflags: {ecuBitFlags:X}");

            // advancing forward to ecuBase + 10
            int ecuHdrIdk1 = reader.ReadInt32(); // no idea
            // Console.WriteLine($"Skipping: {ecuHdrIdk1:X8}");

            Qualifier = reader.ReadBitflagStringWithReader(ref ecuBitFlags, BaseAddress);
            EcuName_CTF = reader.ReadBitflagInt32(ref ecuBitFlags);
            EcuDescription_CTF = reader.ReadBitflagInt32(ref ecuBitFlags);
            EcuXmlVersion = reader.ReadBitflagStringWithReader(ref ecuBitFlags, BaseAddress);
            InterfaceBlockCount = reader.ReadBitflagInt32(ref ecuBitFlags);
            InterfaceTableOffset = reader.ReadBitflagInt32(ref ecuBitFlags);
            SubinterfacesCount = reader.ReadBitflagInt32(ref ecuBitFlags);
            SubinterfacesOffset = reader.ReadBitflagInt32(ref ecuBitFlags);
            EcuClassName = reader.ReadBitflagStringWithReader(ref ecuBitFlags, BaseAddress);
            UnkStr7 = reader.ReadBitflagStringWithReader(ref ecuBitFlags, BaseAddress);
            UnkStr8 = reader.ReadBitflagStringWithReader(ref ecuBitFlags, BaseAddress);

            int dataBufferOffsetRelativeToFile = (header.StringPoolSize ?? 0) + StubHeader.StubHeaderSize + header.CffHeaderSize + 4;
            // Console.WriteLine($"{nameof(dataBufferOffsetRelativeToFile)} : 0x{dataBufferOffsetRelativeToFile:X}");

            IgnitionRequired = reader.ReadBitflagInt16(ref ecuBitFlags);
            Unk2 = reader.ReadBitflagInt16(ref ecuBitFlags);
            UnkBlockCount = reader.ReadBitflagInt16(ref ecuBitFlags);
            UnkBlockOffset = reader.ReadBitflagInt32(ref ecuBitFlags);
            EcuSgmlSource = reader.ReadBitflagInt16(ref ecuBitFlags);
            Unk6RelativeOffset = reader.ReadBitflagInt32(ref ecuBitFlags);

            EcuVariant_BlockOffset = reader.ReadBitflagInt32(ref ecuBitFlags) + dataBufferOffsetRelativeToFile;
            EcuVariant_EntryCount = reader.ReadBitflagInt32(ref ecuBitFlags);
            EcuVariant_EntrySize = reader.ReadBitflagInt32(ref ecuBitFlags); // 10
            EcuVariant_BlockSize = reader.ReadBitflagInt32(ref ecuBitFlags);

            DiagJob_BlockOffset = reader.ReadBitflagInt32(ref ecuBitFlags) + dataBufferOffsetRelativeToFile;
            DiagJob_EntryCount = reader.ReadBitflagInt32(ref ecuBitFlags);
            DiagJob_EntrySize = reader.ReadBitflagInt32(ref ecuBitFlags); // 14
            DiagJob_BlockSize = reader.ReadBitflagInt32(ref ecuBitFlags);

            GlobalDTCs = reader.ReadBitflagTable<DTC>(ref ecuBitFlags, dataBufferOffsetRelativeToFile, language, this);

            GlobalEnvironmentContexts = reader.ReadBitflagTable<DiagService>(ref ecuBitFlags, dataBufferOffsetRelativeToFile, language, this);

            VcDomain_BlockOffset = reader.ReadBitflagInt32(ref ecuBitFlags) + dataBufferOffsetRelativeToFile;
            VcDomain_EntryCount = reader.ReadBitflagInt32(ref ecuBitFlags);
            VcDomain_EntrySize = reader.ReadBitflagInt32(ref ecuBitFlags); // 12
            VcDomain_BlockSize = reader.ReadBitflagInt32(ref ecuBitFlags);

            Presentations_BlockOffset = reader.ReadBitflagInt32(ref ecuBitFlags) + dataBufferOffsetRelativeToFile;
            Presentations_EntryCount = reader.ReadBitflagInt32(ref ecuBitFlags);
            Presentations_EntrySize = reader.ReadBitflagInt32(ref ecuBitFlags); // 8
            Presentations_BlockSize = reader.ReadBitflagInt32(ref ecuBitFlags);

            InternalPresentations_BlockOffset = reader.ReadBitflagInt32(ref ecuBitFlags) + dataBufferOffsetRelativeToFile;
            InternalPresentations_EntryCount = reader.ReadBitflagInt32(ref ecuBitFlags);
            InternalPresentations_EntrySize = reader.ReadBitflagInt32(ref ecuBitFlags); // 8
            InternalPresentations_BlockSize = reader.ReadBitflagInt32(ref ecuBitFlags);

            Unk_BlockOffset = reader.ReadBitflagInt32(ref ecuBitFlags) + dataBufferOffsetRelativeToFile;
            Unk_EntryCount = reader.ReadBitflagInt32(ref ecuBitFlags);
            Unk_EntrySize = reader.ReadBitflagInt32(ref ecuBitFlags);
            Unk_BlockSize = reader.ReadBitflagInt32(ref ecuBitFlags);

            Unk39 = reader.ReadBitflagInt32(ref ecuBitFlags);

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
            CreatePresentations(reader, language);
            CreateInternalPresentations(reader, language);
            // requires presentations
            CreateDiagServices(reader, language);
            // dtc has xrefs to envs
            CreateVCDomains(reader, language);

            CreateEcuVariants(reader, language);
            //PrintDebug();
        }

        public void CreateDiagServices(CaesarReader reader, CTFLanguage language)
        {
            if (DiagJob_EntryCount != null && DiagJob_BlockOffset != null)
            {
                byte[] diagjobPool = ReadDiagjobPool(reader);
                // arrays since list has become too expensive
                DiagService[] globalDiagServices = new DiagService[(int)DiagJob_EntryCount];


                using (CaesarReader poolReader = new CaesarReader(new MemoryStream(diagjobPool)))
                {
                    for (int diagjobIndex = 0; diagjobIndex < DiagJob_EntryCount; diagjobIndex++)
                    {
                        int offset = poolReader.ReadInt32();
                        int size = poolReader.ReadInt32();
                        uint crc = poolReader.ReadUInt32();
                        uint config = poolReader.ReadUInt16();
                        long diagjobBaseAddress = offset + (long)DiagJob_BlockOffset;
                        // Console.WriteLine($"DJ @ {offset:X} with size {size:X}");

                        DiagService dj = new DiagService(reader, language, diagjobBaseAddress, diagjobIndex, this);
                        // GlobalDiagServices.Add(dj);
                        globalDiagServices[diagjobIndex] = dj;
                    }
                }

                GlobalDiagServices = new List<DiagService>(globalDiagServices);
            }
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
        public void CreatePresentations(CaesarReader reader, CTFLanguage language)
        {
            if (Presentations_BlockOffset != null && Presentations_EntryCount != null)
            {
                byte[] presentationsPool = ReadECUPresentationsPool(reader);
                // arrays since list has become too expensive
                // DiagService[] globalDiagServices = new DiagService[DiagJob_EntryCount];
                DiagPresentation[] globalPresentations = new DiagPresentation[(int)Presentations_EntryCount];

                using (CaesarReader poolReader = new CaesarReader(new MemoryStream(presentationsPool)))
                {
                    for (int presentationsIndex = 0; presentationsIndex < Presentations_EntryCount; presentationsIndex++)
                    {

                        int offset = poolReader.ReadInt32();
                        int size = poolReader.ReadInt32();

                        long presentationsBaseAddress = offset + (long)Presentations_BlockOffset;
                        // string offsetLog = $"Pres @ 0x{offset:X} with size 0x{size:X} base 0x{presentationsBaseAddress:X}";

                        DiagPresentation pres = new DiagPresentation(reader, presentationsBaseAddress, presentationsIndex, language);
                        globalPresentations[presentationsIndex] = pres;
                    }
                    // Console.WriteLine($"Entry count/size for presentations : {Presentations_EntryCount}, {Presentations_EntrySize}");
                }
                GlobalPresentations = new List<DiagPresentation>(globalPresentations);
            }
        }
        public void CreateInternalPresentations(CaesarReader reader, CTFLanguage language)
        {
            if (InternalPresentations_EntryCount != null && InternalPresentations_BlockOffset != null)
            {
                byte[] internalPresentationsPool = ReadECUInternalPresentationsPool(reader);
                DiagPresentation[] globalInternalPresentations = new DiagPresentation[(int)InternalPresentations_EntryCount];

                using (BinaryReader poolReader = new BinaryReader(new MemoryStream(internalPresentationsPool)))
                {
                    for (int internalPresentationsIndex = 0; internalPresentationsIndex < InternalPresentations_EntryCount; internalPresentationsIndex++)
                    {
                        int offset = poolReader.ReadInt32();
                        int size = poolReader.ReadInt32();

                        long internalPresentationsBaseAddress = offset + (long)InternalPresentations_BlockOffset;
                        DiagPresentation pres = new DiagPresentation(reader, internalPresentationsBaseAddress, internalPresentationsIndex, language);
                        globalInternalPresentations[internalPresentationsIndex] = pres;
                    }
                }
                GlobalInternalPresentations = new List<DiagPresentation>(globalInternalPresentations);
            }
        }
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
                $"{nameof(EcuName_CTF)}={EcuName_CTF}, " +
                $"{nameof(EcuDescription_CTF)}={EcuDescription_CTF}, " +
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
                $"{nameof(DiagJob_BlockOffset)}=0x{DiagJob_BlockOffset:X}, " +
                $"{nameof(DiagJob_EntryCount)}={DiagJob_EntryCount}, " +
                $"{nameof(DiagJob_EntrySize)}={DiagJob_EntrySize}, " +
                $"{nameof(DiagJob_BlockSize)}=0x{DiagJob_BlockSize:X}, " +
                $"{nameof(GlobalDTCs)}={GlobalDTCs}, " +
                $"{nameof(GlobalEnvironmentContexts)}={GlobalEnvironmentContexts}, " +

                // Console.WriteLine("--- bitflag load 2 ---");

                //$"{nameof(Env_BlockSize)}=0x{Env_BlockSize:X}, " +
                $"{nameof(VcDomain_BlockOffset)}=0x{VcDomain_BlockOffset:X}, " +
                $"{nameof(VcDomain_EntryCount)}={VcDomain_EntryCount}, " +
                $"{nameof(VcDomain_EntrySize)}={VcDomain_EntrySize}, " +
                $"{nameof(VcDomain_BlockSize)}=0x{VcDomain_BlockSize:X}, " +
                $"{nameof(Presentations_BlockOffset)}=0x{Presentations_BlockOffset:X}, " +
                $"{nameof(Presentations_EntryCount)}={Presentations_EntryCount}, " +
                $"{nameof(Presentations_EntrySize)}={Presentations_EntrySize}, " +
                $"{nameof(Presentations_BlockSize)}=0x{Presentations_BlockSize:X}, " +
                $"{nameof(InternalPresentations_BlockOffset)}=0x{InternalPresentations_BlockOffset:X}, " +
                $"{nameof(InternalPresentations_EntryCount)}={InternalPresentations_EntryCount}, " +
                $"{nameof(InternalPresentations_EntrySize)}={InternalPresentations_EntrySize}, " +
                $"{nameof(InternalPresentations_BlockSize)}=0x{InternalPresentations_BlockSize:X}, " +
                $"{nameof(Unk_BlockOffset)}=0x{Unk_BlockOffset:X}, " +
                $"{nameof(Unk_EntryCount)}={Unk_EntryCount}, " +
                $"{nameof(Unk_EntrySize)}={Unk_EntrySize}, " +
                $"{nameof(Unk_BlockSize)}={Unk_BlockSize}, " +
                $"{nameof(Unk39)}={Unk39}";
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
