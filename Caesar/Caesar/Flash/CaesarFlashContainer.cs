using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Caesar
{
    public class CaesarFlashContainer
    {
        public FlashHeader CaesarFlashHeader;
        public CTFHeader CaesarCTFHeader;

        public byte[] FileBytes = new byte[] { };
        public uint ProvidedChecksum = 0;
		
        public CaesarFlashContainer(byte[] fileBytes)
        {
            CaesarCTFHeader = new CTFHeader();
            FileBytes = fileBytes;
            // from DIOpenCFF
            using (CaesarReader reader = new CaesarReader(new MemoryStream(fileBytes, 0, FileBytes.Length, false, true)))
            {
                byte[] header = reader.ReadBytes(StubHeader.StubHeaderSize);

                int cffHeaderSize = reader.ReadInt32();
                byte[] cffHeaderData = reader.ReadBytes(cffHeaderSize);

                uint computedChecksum = CaesarReader.ComputeFileChecksumLazy(fileBytes);
                uint providedChecksum = ReadFileChecksum(fileBytes);

                if (computedChecksum != providedChecksum)
                {
                    Console.WriteLine($"WARNING: Checksum mismatch : computed/provided: {computedChecksum:X8}/{providedChecksum:X8}");
                }
				ProvidedChecksum = providedChecksum;
				
                CaesarFlashHeader = new FlashHeader(reader);
				
                ReadCTF(reader);
            }
        }
        void ReadCTF(CaesarReader fileReader)
        {
            Debug.Assert(CaesarFlashHeader.LanguageHeaderTable != null, "No idea how to handle nonexistent ctf header");
            
            long ctfOffset = CaesarFlashHeader.BaseAddress + (long)CaesarFlashHeader.LanguageHeaderTable;
            ///CaesarCTFHeader = new CTFHeader(fileReader, ctfOffset, CaesarFlashHeader.CffHeaderSize);
        }


        public uint ReadFileChecksum(byte[] fileBytes)
        {
            return BitConverter.ToUInt32(fileBytes, fileBytes.Length - 4);
        }

        public static void ExportCFFMemorySegments(string filePath) 
        {
            string? directory = Path.GetDirectoryName(filePath);

            Console.WriteLine($"Starting CFF segment export. Assumes that segments are embedded and unprotected.");
            byte[] flashContainer = File.ReadAllBytes(filePath);
            CaesarFlashContainer container = new CaesarFlashContainer(flashContainer);

            // mini-feature: if a binary with enough space exists, the image's data will be written to it (for some CFFs with many segments, this saves time)
            string mergePath = @"merge.bin";
            bool useMergeFeature = File.Exists(mergePath);
            byte[] mergeBytes = new byte[] { };
            if (useMergeFeature) 
            {
                mergeBytes = File.ReadAllBytes(mergePath);
            }

            using (BinaryReader reader = new BinaryReader(new MemoryStream(flashContainer)))
            {
                foreach (FlashDataBlock db in container.CaesarFlashHeader.DataBlocks)
                {
                    Console.WriteLine($"FlashDataBlock: {db.Qualifier}");
                    long fileCursor = 0;
                    foreach (FlashSegment seg in db.FlashSegments)
                    {
                        long offset =
                            (long)(db.FlashData ?? 0) +
                            container.CaesarFlashHeader.CffHeaderSize +
                            container.CaesarFlashHeader.LanguageBlockLength ?? 0 +
                            fileCursor +
                            0x414;

                        fileCursor += seg.SegmentLength ?? 0;
                        Console.WriteLine($"Segment: {seg.SegmentName} mapped to 0x{seg.FromAddress:X} with size 0x{seg.SegmentLength:X}");
                        reader.BaseStream.Seek(offset, SeekOrigin.Begin);
                        if (seg.SegmentLength != null)
                        {
                            byte[] fileBytes = reader.ReadBytes((int)seg.SegmentLength);

                            File.WriteAllBytes($"{directory}\\{db.Qualifier}_{seg.FromAddress:X}.bin", fileBytes);

                            if (useMergeFeature)
                            {
                                if (seg.FromAddress != null && mergeBytes.Length >= (seg.FromAddress + fileBytes.Length))
                                {
                                    for (int i = 0; i < fileBytes.Length; i++)
                                    {
                                        mergeBytes[i + (int)seg.FromAddress] |= fileBytes[i];
                                    }
                                }
                                else
                                {
                                    Console.WriteLine($"Out-of-bound merge, skipping {seg.SegmentName} mapped to 0x{seg.FromAddress:X} with size 0x{seg.SegmentLength:X}");
                                }
                            }
                        }
                    }
                }

            }
            Console.WriteLine($"Exported segments can be found at {directory}");
            if (useMergeFeature) 
            {
                File.WriteAllBytes(mergePath, mergeBytes);
            }
        }

    }
}