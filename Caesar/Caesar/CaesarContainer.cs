using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace Caesar
{
    public class CaesarContainer
    {
        public CFFHeader CaesarCFFHeader;
        public CTFHeader CaesarCTFHeader;
        public List<ECU> CaesarECUs = new List<ECU>();
        [Newtonsoft.Json.JsonIgnore]
        public byte[] FileBytes = new byte[] { };

        public uint FileChecksum;

        public CaesarContainer() 
        {
            CaesarCFFHeader = new CFFHeader();
            CaesarCTFHeader = new CTFHeader();
        }

        // fixup serialization/deserialization:
        // language strings should be properties; resolve to actual string only when called
        public CaesarContainer(byte[] fileBytes)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();

            FileBytes = fileBytes;
            // work from int __cdecl DIIAddCBFFile(char *fileName)
            using (CaesarReader reader = new CaesarReader(new MemoryStream(fileBytes, 0, fileBytes.Length, false, true)))
            {
                byte[] header = reader.ReadBytes(StubHeader.StubHeaderSize);
                StubHeader.ReadHeader(header);

                int cffHeaderSize = reader.ReadInt32();
                byte[] cffHeaderData = reader.ReadBytes(cffHeaderSize);

                // expensive, probably an impediment for modders
                // VerifyChecksum(fileBytes, out uint checksum);
                FileChecksum = ReadFileChecksum(fileBytes);

                CaesarCFFHeader = new CFFHeader(reader, this);
                CaesarCTFHeader = CaesarCFFHeader.CaesarCTFHeader;
                // CaesarCFFHeader.PrintDebug();

                if (CaesarCFFHeader.CaesarVersion < 400)
                {
                    throw new NotImplementedException($"Unhandled Caesar version: {CaesarCFFHeader.CaesarVersion}");
                }
                // language is the highest priority since all our strings come from it
                ReadECU(reader);

                //Restore now resolves references and performs post-read operations so we must call it here.
                CaesarECUs.ForEach(ecu => ecu.Restore(ecu.Language, this));
            }

            sw.Stop();
//#if DEBUG
            Console.WriteLine($"Loaded {CaesarECUs[0].Qualifier} in {sw.ElapsedMilliseconds}ms");
//#endif
        }

        public static string SerializeContainer(CaesarContainer container) 
        {
            return JsonConvert.SerializeObject(container, typeof(CaesarContainer), new JsonSerializerSettings() { Formatting = Formatting.Indented, DefaultValueHandling = DefaultValueHandling.Ignore });
        }

        public static CaesarContainer? DeserializeContainer(string json) 
        {
            CaesarContainer? container = JsonConvert.DeserializeObject<CaesarContainer>(json);
            // at this point, the container needs to restore its internal object references before it is fully usable
            if (container != null)
            {
                CTFLanguage language = container.CaesarCTFHeader.CtfLanguages.GetObjects()[0];
                foreach (ECU ecu in container.CaesarECUs)
                {
                    ecu.Restore(language, container);
                }
            }

            return container;
        }

        public static CaesarContainer? DeserializeCompressedContainer(byte[] containerBytes)
        {
            string json = Encoding.UTF8.GetString(Inflate(containerBytes));
            return DeserializeContainer(json);
        }
        public static byte[] SerializeCompressedContainer(CaesarContainer container)
        {
            return Deflate(Encoding.UTF8.GetBytes(SerializeContainer(container)));
        }


        private static byte[] Inflate(byte[] input)
        {
            using (MemoryStream ms = new MemoryStream(input))
            {
                using (MemoryStream msInner = new MemoryStream())
                {
                    using (DeflateStream z = new DeflateStream(ms, CompressionMode.Decompress))
                    {
                        z.CopyTo(msInner);
                    }
                    return msInner.ToArray();
                }
            }
        }
        private static byte[] Deflate(byte[] input)
        {
            using (MemoryStream compressedStream = new MemoryStream())
            {
                DeflateStream deflateStream = new DeflateStream(compressedStream, CompressionLevel.Optimal, true);
                deflateStream.Write(input, 0, input.Length);
                deflateStream.Close();
                return compressedStream.ToArray();
            }
        }


        public static bool VerifyChecksum(byte[] fileBytes, out uint checksum) 
        {
            uint computedChecksum = CaesarReader.ComputeFileChecksumLazy(fileBytes);
            uint providedChecksum = ReadFileChecksum(fileBytes);
            checksum = providedChecksum;
            if (computedChecksum != providedChecksum)
            {
                Console.WriteLine($"WARNING: Checksum mismatch : computed/provided: {computedChecksum:X8}/{providedChecksum:X8}");
                return false;
            }
            return true;
        }

        public static string GetCaesarVersionString()
        {
            System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
            System.Diagnostics.FileVersionInfo fvi = System.Diagnostics.FileVersionInfo.GetVersionInfo(assembly.Location);
            return fvi.FileVersion ?? string.Empty;
        }

        public static uint ReadFileChecksum(byte[] fileBytes) 
        {
            return BitConverter.ToUInt32(fileBytes, fileBytes.Length - 4);
        }

        public ECUVariant? GetECUVariantByName(string name)
        {
            foreach (ECU ecu in CaesarECUs)
            {
                if (ecu.ECUVariants != null)
                {
                    foreach (ECUVariant variant in ecu.ECUVariants.GetObjects())
                    {
                        if (variant.Qualifier == name)
                        {
                            return variant;
                        }
                    }
                }
            }
            return null;
        }
        public ECU? GetECUByName(string name)
        {
            foreach (ECU ecu in CaesarECUs)
            {
                if (ecu.Qualifier == name)
                {
                    return ecu;
                }
            }
            return null;
        }

        public string[] GetECUVariantNames() 
        {
            List<string> result = new List<string>();

            foreach (ECU ecu in CaesarECUs)
            {
                if (ecu.ECUVariants != null)
                {
                    foreach (ECUVariant variant in ecu.ECUVariants.GetObjects())
                    {
                        if (variant.Qualifier != null)
                        {
                            result.Add(variant.Qualifier);
                        }
                    }
                }
            }
            return result.ToArray();
        }

        CTFLanguage? language;
        public CTFLanguage Language
        {
            get => language ?? throw new Exception();
            set => language = value;
        }

        void ReadECU(CaesarReader fileReader) 
        {
            CaesarECUs = new List<ECU>(CaesarCFFHeader.CaesarECUs.GetObjects());
            // read all ecu definitions
            //if (CaesarCFFHeader.EcuOffset != null && CaesarCFFHeader.EcuCount != null)
            //{
            //    long ecuTableOffset = (long)CaesarCFFHeader.EcuOffset + CaesarCFFHeader.BaseAddress;

            //    for (int ecuIndex = 0; ecuIndex < CaesarCFFHeader.EcuCount; ecuIndex++)
            //    {
            //        // seek to an entry the ecu offsets table
            //        fileReader.BaseStream.Seek(ecuTableOffset + (ecuIndex * 4), SeekOrigin.Begin);
            //        // read the offset to the ecu entry, then seek to the actual address
            //        int offsetToActualEcuEntry = fileReader.ReadInt32();
            //        CaesarECUs.Add(new ECU(fileReader, GetLanguage(), CaesarCFFHeader, ecuTableOffset + offsetToActualEcuEntry, this));
            //    }
            //}
        }

        public string GetFileSize() 
        {
            return BytesToString(FileBytes.Length);
        }

        private static string BytesToString(long byteCount)
        {
            string[] suf = { " B", " KB", " MB", " GB", " TB", " PB", " EB" }; //Longs run out around EB
            if (byteCount == 0)
            {
                return "0" + suf[0];
            }
            long bytes = Math.Abs(byteCount);
            int place = Convert.ToInt32(Math.Floor(Math.Log(bytes, 1024)));
            double num = Math.Round(bytes / Math.Pow(1024, place), 3);
            return (Math.Sign(byteCount) * num).ToString() + suf[place];
        }

        public override bool Equals(object? obj)
        {
            var container = obj as CaesarContainer;

            if (container == null) 
            {
                return false;
            }
            return this.FileChecksum == container.FileChecksum;
        }

        public override int GetHashCode()
        {
            return (int)FileChecksum;
        }

    }
}
