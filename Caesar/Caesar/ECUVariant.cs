using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Caesar
{
    public class ECUVariant : CaesarObject
    {
        public int EntrySize;
        public string? Qualifier;
        public CaesarStringReference? Name;
        public CaesarStringReference? Description;
        public string? UnkStr1;
        public string? UnkStr2;
        public int? Unk1;

        public CaesarTable<ECUVariantPattern>? VariantPatterns;
        private int? SubsectionB_Count; // B
        private int? SubsectionB_Offset;
        public CaesarTable<ComParameter>? ComParameters;
        private int? DiagServiceCode_Count; // D : DSC
        private int? DiagServiceCode_Offset;
        public CaesarPool? DiagServicesPoolOffsets;
        private int? DTC_Count; // F
        private int? DTC_Offset;
        public CaesarPool? EnvironmentContextsPoolOffsets;
        private int? Xref_Count; // H
        private int? Xref_Offset;
        public CaesarPool? VCDomainPoolOffsets;

        public string? NegativeResponseName;
        public int? UnkByte;

        public List<Tuple<int, int, int>> DTCsPoolOffsetsWithBounds = new List<Tuple<int, int, int>>();

        public int[] Xrefs = new int[] { };

        // these should be manually deserialized by creating references back to the parent ECU

        [Newtonsoft.Json.JsonIgnore]
        public List<VCDomain> VCDomains = new List<VCDomain>();
        [Newtonsoft.Json.JsonIgnore]
        public DiagService[] DiagServices = new DiagService[] { };
        [Newtonsoft.Json.JsonIgnore]
        public DTC[] DTCs = new DTC[] { };
        [Newtonsoft.Json.JsonIgnore]
        public EnvironmentContext[] EnvironmentContexts = new EnvironmentContext[] { };

        [Newtonsoft.Json.JsonIgnore]
        public ECU ParentECU;

        [Newtonsoft.Json.JsonIgnore]
        private CTFLanguage Language;

        public void Restore(CTFLanguage language, ECU parentEcu)
        {
            Language = language;
            ParentECU = parentEcu;

            CreateVCDomains(parentEcu, language);
            CreateDiagServices(parentEcu, language);
            CreateDTCs(parentEcu, language);
            CreateEnvironmentContexts(parentEcu, language);

            /*
            // no restoring required
            foreach (ECUVariantPattern vp in VariantPatterns) 
            {
                vp.Restore();
            }
            */
            // CreateComParameters(reader, parentEcu); // already serialized in json
        }

        public ECUVariant()
        {
            ParentECU = new ECU();
            Language = new CTFLanguage();
        }

        public ECUVariant(CaesarReader reader, ECU parentEcu, CTFLanguage language, long baseAddress, int blockSize)
        {
            // int __usercall DIIFindVariantByECUID@<eax>(ECU_VARIANT *a1@<ebx>, _DWORD *a2, int a3, __int16 a4, int a5)

            AbsoluteAddress = (int)baseAddress;
            ParentECU = parentEcu;
            Language = language;
            reader.BaseStream.Seek(baseAddress, SeekOrigin.Begin);

            //PrintDebug();
        }

        // this function is parked here since the values are drawn from EnvironmentContexts // Xref_Count and Xref_Offset;
        public List<DiagService> GetEnvironmentContextsForDTC(DTC inDtc)
        {
            List<DiagService> ctxList = new List<DiagService>();
            if (inDtc.XrefStart != null && inDtc.XrefCount != null)
            {
                for (int i = (int)inDtc.XrefStart; i < (inDtc.XrefStart + inDtc.XrefCount); i++)
                {
                    foreach (DiagService envToTest in EnvironmentContexts)
                    {
                        int xref = Xrefs[i];
                        if (envToTest.PoolIndex == xref)
                        {
                            ctxList.Add(envToTest);
                            break;
                        }
                    }
                }
            }
            return ctxList;
        }

        public void CreateComParameters(CaesarReader reader, ECU parentEcu)
        {
            if (ComParameters != null)
            {
                foreach (var comParam in ComParameters.GetObjects())
                {
                    if (comParam.ParentInterfaceIndex != null && comParam.SubinterfaceIndex != null)
                    {
                        // KW2C3PE uses a different parent addressing style
                        int parentIndex = (int)(comParam.ParentInterfaceIndex > 0 ? comParam.ParentInterfaceIndex : comParam.SubinterfaceIndex);

                        if (comParam.ParentInterfaceIndex >= parentEcu.ECUInterfaceSubtypes.Count)
                        {
                            throw new Exception("ComParam: tried to assign to nonexistent interface");
                        }
                        else
                        {
                            parentEcu.ECUInterfaceSubtypes[parentIndex].CommunicationParameters.Add(comParam);
                        }
                    }
                }
            }
        }

        public VCDomain? GetVCDomainByName(string name)
        {
            foreach (VCDomain domain in VCDomains)
            {
                if (domain.Qualifier == name)
                {
                    return domain;
                }
            }
            return null;
        }
        public DiagService? GetDiagServiceByName(string name)
        {
            foreach (DiagService diag in DiagServices)
            {
                if (diag.Qualifier == name)
                {
                    return diag;
                }
            }
            return null;
        }
        public string[] GetVCDomainNames()
        {
            List<string> result = new List<string>();
            foreach (VCDomain domain in VCDomains)
            {
                if (domain.Qualifier != null)
                {
                    result.Add(domain.Qualifier);
                }
            }
            return result.ToArray();
        }

        private void CreateVCDomains(ECU parentEcu, CTFLanguage language)
        {
            VCDomains = new List<VCDomain>();
            if (VCDomainPoolOffsets != null)
            {
                List<VCDomain> globalVcds = parentEcu.GlobalVCDs != null ? parentEcu.GlobalVCDs.GetObjects() : new List<VCDomain>();
                foreach (int variantCodingDomainEntry in VCDomainPoolOffsets.GetPoolIndices())
                {
                    /*
                    VCDomain vcDomain = new VCDomain(reader, parentEcu, language, variantCodingDomainEntry);
                    VCDomains.Add(vcDomain);
                    */
                    VCDomains.Add(globalVcds[variantCodingDomainEntry]);
                }
            }
        }
        private void CreateDiagServices(ECU parentEcu, CTFLanguage language)
        {
            // unlike variant domains, storing references to the parent objects in the ecu is preferable since this is relatively larger
            //DiagServices = new List<DiagService>();
            if (DiagServicesPoolOffsets != null)
            {
                var diagServicesOffsets = DiagServicesPoolOffsets.GetPoolIndices();
                DiagServices = new DiagService[diagServicesOffsets.Count];

                /*
                // computationally expensive, 40ish % runtime is spent here
                // easier to read, below optimization essentially accomplishes this in a shorter period

                foreach (DiagService diagSvc in parentEcu.GlobalDiagServices)
                {
                    for (int i = 0; i < DiagServicesPoolOffsets.Count; i++)
                    {
                        if (diagSvc.PoolIndex == DiagServicesPoolOffsets[i])
                        {
                            DiagServices[i] = diagSvc;
                        }
                    }
                }
                */
                // optimization hack
                int poolSize = diagServicesOffsets.Count;
                if (parentEcu.GlobalDiagServices != null)
                {
                    List<DiagService> globalDiagServices = parentEcu.GlobalDiagServices.GetObjects();
                    for (int i = 0; i < poolSize; i++)
                    {
                        if (i == diagServicesOffsets[i])
                        {
                            DiagServices[i] = globalDiagServices[i];
                        }
                    }
                    diagServicesOffsets.Sort();
                    int lowestIndex = 0;
                    int loopMax = parentEcu.GlobalDiagServices.Count;
                    for (int i = 0; i < poolSize; i++)
                    {
                        if (DiagServices[i] != null)
                        {
                            continue;
                        }
                        for (int globalIndex = lowestIndex; globalIndex < loopMax; globalIndex++)
                        {
                            if (globalDiagServices[globalIndex].PoolIndex == diagServicesOffsets[i])
                            {
                                DiagServices[i] = globalDiagServices[globalIndex];
                                lowestIndex = globalIndex;
                                break;
                            }
                        }
                    }
                }
            }
        }

        private void CreateDTCs(ECU parentEcu, CTFLanguage language)
        {
            if (parentEcu.GlobalDTCs == null)
            {
                return;
            }
            int dtcPoolSize = DTCsPoolOffsetsWithBounds.Count;
            DTCs = new DTC[dtcPoolSize];
            List<DTC> globalDtcsCopy = parentEcu.GlobalDTCs.GetObjects();
            for (int i = 0; i < dtcPoolSize; i++)
            {
                if (i == DTCsPoolOffsetsWithBounds[i].Item1)
                {
                    DTCs[i] = globalDtcsCopy[i];
                    DTCs[i].XrefStart = DTCsPoolOffsetsWithBounds[i].Item2;
                    DTCs[i].XrefCount = DTCsPoolOffsetsWithBounds[i].Item3;
                }
            }
            DTCsPoolOffsetsWithBounds.Sort((x, y) => x.Item1.CompareTo(y.Item1));
            int lowestIndex = 0;
            int loopMax = globalDtcsCopy.Count;
            for (int i = 0; i < dtcPoolSize; i++)
            {
                if (DTCs[i] != null)
                {
                    continue;
                }
                for (int globalIndex = lowestIndex; globalIndex < loopMax; globalIndex++)
                {
                    if (globalDtcsCopy[globalIndex].PoolIndex == DTCsPoolOffsetsWithBounds[i].Item1)
                    {
                        DTCs[i] = globalDtcsCopy[globalIndex];
                        DTCs[i].XrefStart = DTCsPoolOffsetsWithBounds[i].Item2;
                        DTCs[i].XrefCount = DTCsPoolOffsetsWithBounds[i].Item3;
                        lowestIndex = globalIndex;
                        break;
                    }
                }
            }

            /*
            // same thing as above, just more readable and slower
            foreach (DTC dtc in parentEcu.GlobalDTCs)
            {
                for (int i = 0; i < DTCsPoolOffsetsWithBounds.Count; i++)
                {
                    if (dtc.PoolIndex == DTCsPoolOffsetsWithBounds[i].Item1)
                    {
                        // this is only valid on the assumption that DTC instances are unique (e.g. not shared from a base variant)
                        dtc.XrefStart = DTCsPoolOffsetsWithBounds[i].Item2;
                        dtc.XrefCount = DTCsPoolOffsetsWithBounds[i].Item3;
                        DTCs[i] = dtc;
                    }
                }
            }
            */
        }

        private void CreateXrefs(BinaryReader reader, ECU parentEcu, CTFLanguage language)
        {
            Xrefs = new int[Xref_Count ?? 0];
            if (Xref_Count != null && Xref_Offset != null)
            {
                reader.BaseStream.Seek(AbsoluteAddress + (long)Xref_Offset, SeekOrigin.Begin);
                for (int i = 0; i < Xref_Count; i++)
                {
                    Xrefs[i] = reader.ReadInt32();
                }
            }
        }

        private void CreateEnvironmentContexts(ECU parentEcu, CTFLanguage language)
        {
            if (EnvironmentContextsPoolOffsets != null)
            {
                var envCtxIndices = EnvironmentContextsPoolOffsets.GetPoolIndices();
                int envPoolSize = envCtxIndices.Count;
                EnvironmentContexts = new EnvironmentContext[envPoolSize];
                List<EnvironmentContext> globalEnvCache = parentEcu.GlobalEnvironmentContexts != null ? parentEcu.GlobalEnvironmentContexts.GetObjects() : new List<EnvironmentContext>();
                for (int i = 0; i < envPoolSize; i++)
                {
                    if (i == envCtxIndices[i])
                    {
                        EnvironmentContexts[i] = globalEnvCache[i];
                    }
                }
                envCtxIndices.Sort();
                int lowestIndex = 0;
                int loopMax = globalEnvCache.Count;
                for (int i = 0; i < envPoolSize; i++)
                {
                    if (EnvironmentContexts[i] != null)
                    {
                        continue;
                    }
                    for (int globalIndex = lowestIndex; globalIndex < loopMax; globalIndex++)
                    {
                        if (globalEnvCache[globalIndex].PoolIndex == envCtxIndices[i])
                        {
                            EnvironmentContexts[i] = globalEnvCache[globalIndex];
                            lowestIndex = globalIndex;
                            break;
                        }
                    }
                }
                /*
                // same thing, more readable, much slower
                foreach (DiagService env in parentEcu.GlobalEnvironmentContexts)
                {
                    for (int i = 0; i < EnvironmentContextsPoolOffsets.Count; i++)
                    {
                        if (env.PoolIndex == EnvironmentContextsPoolOffsets[i])
                        {
                            EnvironmentContexts[i] = env;
                        }
                    }
                }
                */
            }
        }

        public void PrintDebug()
        {
            Console.WriteLine($"---------------- {AbsoluteAddress:X} ----------------");
            Console.WriteLine($"{nameof(Qualifier)} : {Qualifier}");
            Console.WriteLine($"{nameof(Name)} : {Name?.Text}");
            Console.WriteLine($"{nameof(Description)} : {Description?.Text}");
            Console.WriteLine($"{nameof(UnkStr1)} : {UnkStr1}");
            Console.WriteLine($"{nameof(UnkStr2)} : {UnkStr2}");
            Console.WriteLine($"{nameof(NegativeResponseName)} : {NegativeResponseName}");

            Console.WriteLine($"{nameof(Unk1)} : {Unk1}");
            Console.WriteLine($"{nameof(VariantPatterns)} : {VariantPatterns}");
            Console.WriteLine($"{nameof(SubsectionB_Count)} : {SubsectionB_Count}");
            Console.WriteLine($"{nameof(SubsectionB_Offset)} : {SubsectionB_Offset}");
            Console.WriteLine($"{nameof(ComParameters)} : {ComParameters}");
            Console.WriteLine($"{nameof(DiagServiceCode_Count)} : {DiagServiceCode_Count}");
            Console.WriteLine($"{nameof(DiagServiceCode_Offset)} : {DiagServiceCode_Offset}");
            Console.WriteLine($"{nameof(DiagServicesPoolOffsets)} : {DiagServicesPoolOffsets}");
            Console.WriteLine($"{nameof(DTC_Count)} : {DTC_Count}");
            Console.WriteLine($"{nameof(DTC_Offset)} : {DTC_Offset}");
            Console.WriteLine($"{nameof(EnvironmentContextsPoolOffsets)} : {EnvironmentContextsPoolOffsets}");
            Console.WriteLine($"{nameof(Xref_Count)} : {Xref_Count}");
            Console.WriteLine($"{nameof(Xref_Offset)} : {Xref_Offset}");
            Console.WriteLine($"{nameof(VCDomainPoolOffsets)} : {VCDomainPoolOffsets}");

        }

        protected override bool ReadHeader(CaesarReader reader)
        {
            base.ReadHeader(reader);

            EntrySize = reader.ReadInt32();
            ushort poolEntryAttributes = reader.ReadUInt16();

            return true;
        }

        protected override void ReadData(CaesarReader reader, CTFLanguage language, ECU? currentEcu)
        {
            int blockSize = 0;
            if (ParentObject is CaesarTable<ECUVariant> varTable && varTable.EntrySize != null)
            {
                blockSize = (int)varTable.EntrySize;
            }
            byte[] variantBytes = reader.ReadBytes(blockSize);

            using (CaesarReader variantReader = new CaesarReader(new MemoryStream(variantBytes, 0, variantBytes.Length, false, true)))
            {
                Bitflags = variantReader.ReadUInt32();
                int skip = variantReader.ReadInt32();

                Qualifier = variantReader.ReadBitflagStringWithReader(ref Bitflags);
                Name = variantReader.ReadBitflagStringRef(ref Bitflags, language);
                Description = variantReader.ReadBitflagStringRef(ref Bitflags, language);
                UnkStr1 = variantReader.ReadBitflagStringWithReader(ref Bitflags);
                UnkStr2 = variantReader.ReadBitflagStringWithReader(ref Bitflags);

                Unk1 = variantReader.ReadBitflagInt32(ref Bitflags);  // 1 
                int oldAddress = AbsoluteAddress;
                AbsoluteAddress = 0;
                VariantPatterns = variantReader.ReadBitflagSubTableAlt<ECUVariantPattern>(this, language, currentEcu);
                AbsoluteAddress = oldAddress;
                SubsectionB_Count = variantReader.ReadBitflagInt32(ref Bitflags);  // 4 
                SubsectionB_Offset = variantReader.ReadBitflagInt32(ref Bitflags);  // 5 
                ComParameters = variantReader.ReadBitflagSubTableAlt<ComParameter>(this, language, currentEcu);
                DiagServiceCode_Count = variantReader.ReadBitflagInt32(ref Bitflags);  // 8 
                DiagServiceCode_Offset = variantReader.ReadBitflagInt32(ref Bitflags);  // 9 
                DiagServicesPoolOffsets = variantReader.ReadBitflagPool(this, language, currentEcu);
                DTC_Count = variantReader.ReadBitflagInt32(ref Bitflags);  // 12 
                DTC_Offset = variantReader.ReadBitflagInt32(ref Bitflags);  // 13 
                EnvironmentContextsPoolOffsets = variantReader.ReadBitflagPool(this, language, currentEcu);
                Xref_Count = variantReader.ReadBitflagInt32(ref Bitflags);  // 16
                Xref_Offset = variantReader.ReadBitflagInt32(ref Bitflags);  // 17 

                VCDomainPoolOffsets = variantReader.ReadBitflagPool(this, language, currentEcu);

                NegativeResponseName = variantReader.ReadBitflagStringWithReader(ref Bitflags);
                UnkByte = variantReader.ReadBitflagInt8(ref Bitflags);  // 20 byte

                // DTCs
                //DTCsPoolOffsets = new List<int>();
                DTCsPoolOffsetsWithBounds = new List<Tuple<int, int, int>>();
                if (DTC_Offset != null)
                {
                    variantReader.BaseStream.Seek((long)DTC_Offset, SeekOrigin.Begin);
                    for (int dtcIndex = 0; dtcIndex < DTC_Count; dtcIndex++)
                    {
                        int actualIndex = variantReader.ReadInt32();
                        int xrefStart = variantReader.ReadInt32();
                        int xrefCount = variantReader.ReadInt32(); // stitch with table H : int __cdecl DIECUGetNumberOfEnvForAllErrors(DI_ECUINFO *ecuh, int a2, int a3)
                                                                   //DTCsPoolOffsets.Add(actualIndex); // todo: depreciate this
                        DTCsPoolOffsetsWithBounds.Add(new Tuple<int, int, int>(actualIndex, xrefStart, xrefCount));
                    }
                }
            }
            if (currentEcu != null)
            {
                CreateVCDomains(currentEcu, language);
                CreateDiagServices(currentEcu, language);
                CreateComParameters(reader, currentEcu);
                CreateDTCs(currentEcu, language);
                CreateEnvironmentContexts(currentEcu, language);
                CreateXrefs(reader, currentEcu, language);
            }
        }
    }
}
