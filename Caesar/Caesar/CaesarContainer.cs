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
        [System.Text.Json.Serialization.JsonIgnore]
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
				
				int caesarStringTableOffset = CaesarCFFHeader.CffHeaderSize + 0x410 + 4;
				int formEntryTable = caesarStringTableOffset + (CaesarCFFHeader.StringPoolSize ?? 0);
					
				// fixme: relook at this, might be the correct way to load fm?

				//Console.WriteLine($"{nameof(caesarStringTableOffset)} : 0x{caesarStringTableOffset:X}");
				//Console.WriteLine($"{nameof(afterStringTableOffset)} : 0x{afterStringTableOffset:X}");

				/*
				if (CaesarCFFHeader.FormEntries > 0)
				{
					int formOffsetTable = CaesarCFFHeader.unk2RelativeOffset + formEntryTable;
					int formOffsetTableSize = CaesarCFFHeader.FormEntrySize * CaesarCFFHeader.FormEntries;
					Console.WriteLine($"after string table block (*.fm) is present: {nameof(formEntryTable)} : 0x{formEntryTable:X}\n\n");
					Console.WriteLine($"{nameof(formOffsetTable)} : 0x{formOffsetTable:X}\n\n");
					Console.WriteLine($"{nameof(formOffsetTableSize)} : 0x{formOffsetTableSize:X}\n\n");
				}
				*/
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
    }
}