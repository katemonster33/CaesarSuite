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
        private CaesarPool? Xrefs; // H
        public CaesarPool? VCDomainPoolOffsets;

        public string? NegativeResponseName;
        public int? UnkByte;

        public List<Tuple<int, int, int>> DTCsPoolOffsetsWithBounds = new List<Tuple<int, int, int>>();

        // these should be manually deserialized by creating references back to the parent ECU

        [System.Text.Json.Serialization.JsonIgnore]
        public List<VCDomain> VCDomains = new List<VCDomain>();
        [System.Text.Json.Serialization.JsonIgnore]
        public DiagService[] DiagServices = new DiagService[] { };
        [System.Text.Json.Serialization.JsonIgnore]
        public DTC[] DTCs = new DTC[] { };
        [System.Text.Json.Serialization.JsonIgnore]
        public EnvironmentContext[] EnvironmentContexts = new EnvironmentContext[] { };

        [System.Text.Json.Serialization.JsonIgnore]
        public ECU ParentECU;

        [System.Text.Json.Serialization.JsonIgnore]
        private CTFLanguage Language;

        public void Restore(CTFLanguage language, ECU parentEcu)
        {
            Language = language;
            ParentECU = parentEcu;

            CreateVCDomains(parentEcu);
            CreateDiagServices(parentEcu);
            CreateDTCs(parentEcu);
            CreateEnvironmentContexts(parentEcu);
            CreateComParameters(parentEcu);

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
            if (inDtc.XrefStart != null && inDtc.XrefCount != null && Xrefs != null)
            {
                var xrefsCpy = Xrefs.GetPoolIndices();
                for (int i = (int)inDtc.XrefStart; i < (inDtc.XrefStart + inDtc.XrefCount); i++)
                {
                    foreach (DiagService envToTest in EnvironmentContexts)
                    {
                        int xref = xrefsCpy[i];
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

        void CreateComParameters(ECU parentEcu)
        {
            if (ComParameters != null && parentEcu.ECUInterfaceSubtypes != null)
            {
                var subTypes = parentEcu.ECUInterfaceSubtypes.GetObjects();
                foreach (var subType in subTypes)
                {
                    subType.CommunicationParameters.Clear();
                }
                foreach (var comParam in ComParameters.GetObjects())
                {
					comParam.InsertIntoEcu(parentEcu);
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

        private void CreateVCDomains(ECU parentEcu)
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
        private void CreateDiagServices(ECU parentEcu)
        {
            // unlike variant domains, storing references to the parent objects in the ecu is preferable since this is relatively larger
            //DiagServices = new List<DiagService>();
            if (DiagServicesPoolOffsets != null)
            {
                var diagServicesOffsets = DiagServicesPoolOffsets.GetPoolIndices();
                DiagServices = new DiagService[diagServicesOffsets.Count];
                List<DiagService> dsTmp = new List<DiagService>(diagServicesOffsets.Count);
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
                    //for (int i = 0; i < poolSize; i++)
                    //{
                    //    if (i == diagServicesOffsets[i])
                    //    {
                    //        DiagServices[i] = globalDiagServices[i];
                    //    }
                    //}
                    //diagServicesOffsets.Sort();
                    //int lowestIndex = 0;
                    //int loopMax = parentEcu.GlobalDiagServices.Count;
                    //for (int i = 0; i < poolSize; i++)
                    //{
                    //    if (DiagServices[i] != null)
                    //    {
                    //        continue;
                    //    }
                    //    for (int globalIndex = lowestIndex; globalIndex < loopMax; globalIndex++)
                    //    {
                    //        if (globalDiagServices[globalIndex].PoolIndex == diagServicesOffsets[i])
                    //        {
                    //            DiagServices[i] = globalDiagServices[globalIndex];
                    //            lowestIndex = globalIndex;
                    //            break;
                    //        }
                    //    }
                    //}
                    foreach(var idx in diagServicesOffsets)
                    {
                        dsTmp.Add(globalDiagServices[idx]);
                    }
                    DiagServices = dsTmp.ToArray();
                }
            }
        }

        private void CreateDTCs(ECU parentEcu)
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

        private void CreateEnvironmentContexts(ECU parentEcu)
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
            Console.WriteLine($"{nameof(Xrefs)} : {Xrefs}");
            Console.WriteLine($"{nameof(VCDomainPoolOffsets)} : {VCDomainPoolOffsets}");

        }

        protected override bool ReadHeader(CaesarReader reader)
        {
            base.ReadHeader(reader);

            EntrySize = reader.ReadInt32();
            ushort poolEntryAttributes = reader.ReadUInt16();

            return true;
        }

        protected override void ReadData(CaesarReader reader, CaesarContainer container)
        {
            byte[] variantBytes = reader.ReadBytes(EntrySize);

            using (CaesarReader variantReader = new CaesarReader(new MemoryStream(variantBytes, 0, variantBytes.Length, false, true)))
            {
                int oldAddress = AbsoluteAddress;
                AbsoluteAddress = 0;
                Bitflags = variantReader.ReadUInt32();
                int skip = variantReader.ReadInt32();

                Qualifier = variantReader.ReadBitflagStringWithReader(ref Bitflags, 0);
                Name = variantReader.ReadBitflagStringRef(ref Bitflags, container);
                Description = variantReader.ReadBitflagStringRef(ref Bitflags, container);
                UnkStr1 = variantReader.ReadBitflagStringWithReader(ref Bitflags, 0);
                UnkStr2 = variantReader.ReadBitflagStringWithReader(ref Bitflags, 0);

                Unk1 = variantReader.ReadBitflagInt32(ref Bitflags);  // 1 
                VariantPatterns = variantReader.ReadBitflagSubTableAlt<ECUVariantPattern>(this, container);
                SubsectionB_Count = variantReader.ReadBitflagInt32(ref Bitflags);  // 4 
                SubsectionB_Offset = variantReader.ReadBitflagInt32(ref Bitflags);  // 5 
                ComParameters = variantReader.ReadBitflagSubTableAlt<ComParameter>(this, container);
                DiagServiceCode_Count = variantReader.ReadBitflagInt32(ref Bitflags);  // 8 
                DiagServiceCode_Offset = variantReader.ReadBitflagInt32(ref Bitflags);  // 9 
                DiagServicesPoolOffsets = variantReader.ReadBitflagPool(this, container);
                DTC_Count = variantReader.ReadBitflagInt32(ref Bitflags);  // 12 
                DTC_Offset = variantReader.ReadBitflagInt32(ref Bitflags);  // 13 
                EnvironmentContextsPoolOffsets = variantReader.ReadBitflagPool(this, container);
                Xrefs = variantReader.ReadBitflagPool(this, container);

                VCDomainPoolOffsets = variantReader.ReadBitflagPool(this, container);

                NegativeResponseName = variantReader.ReadBitflagStringWithReader(ref Bitflags, 0);
                UnkByte = variantReader.ReadBitflagInt8(ref Bitflags);  // 20 byte
                AbsoluteAddress = oldAddress;

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
        }
    }
}