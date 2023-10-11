﻿using System;
using System.IO;
using System.Linq;
using System.Text;

namespace Caesar
{
    public class CaesarReader : BinaryReader
    {
        public Encoding DefaultEncoding = Encoding.UTF8;

        public CaesarReader(Stream input) : base(input, Encoding.UTF8, false)
        {
        }

        public CaesarReader(Stream input, Encoding encoding) : base(input, encoding, false)
        {
            DefaultEncoding = encoding;
        }

        public CaesarReader(Stream input, Encoding encoding, bool leaveOpen) : base(input, encoding, leaveOpen)
        {
            DefaultEncoding = encoding;
        }

        // slightly more complex because it jumps to the string position for reading
        public string? ReadBitflagStringWithReader(ref ulong bitFlags, long virtualBase = 0)
        {
            if (CheckAndAdvanceBitflag(ref bitFlags))
            {
                // read the string's offset relative to our current block
                int stringOffset = ReadInt32();
                // save our reading cursor
                long readerPosition = BaseStream.Position;
                // seek to the specified offset, then read out the string
                BaseStream.Seek(stringOffset + virtualBase, SeekOrigin.Begin);
                string result = ReadString(Encoding.UTF8);
                // restore our reading cursor
                BaseStream.Seek(readerPosition, SeekOrigin.Begin);
                return result;
            }
            else
            {
                // Console.WriteLine("Bitflag was off for string");
                return null;
            }
        }
        public byte[]? ReadBitflagDumpWithReader(ref ulong bitFlags, int? dumpSize, long virtualBase = 0)
        {
            if (CheckAndAdvanceBitflag(ref bitFlags) && dumpSize != null)
            {

                // read the dump's offset relative to our current block
                int dumpOffset = ReadInt32();
                // save our reading cursor
                long readerPosition = BaseStream.Position;
                // seek to the specified offset, then read out the dump
                BaseStream.Seek(dumpOffset + virtualBase, SeekOrigin.Begin);
                byte[] result = ReadBytes((int)dumpSize);
                // restore our reading cursor
                BaseStream.Seek(readerPosition, SeekOrigin.Begin);
                return result;
            }
            else
            {
                return null;
            }
        }

        public string? ReadBitflagDumpWithReaderAsString(ref ulong bitFlags, int dumpSize, long virtualBase = 0)
        {
            byte[]? stringBytes = ReadBitflagDumpWithReader(ref bitFlags, dumpSize, virtualBase);
            return stringBytes != null ? DefaultEncoding.GetString(stringBytes) : null; // lazy: no encoding is specified
        }

        public string ReadString(Encoding? encoding = null)
        {
            if (encoding is null) 
            {
                encoding = DefaultEncoding;
            }

            // slightly better performance than the original below at the expense of compiling with unsafe
            long stringStartPosition = BaseStream.Position;
            byte[] underlyingBuffer = ((MemoryStream)BaseStream).GetBuffer();
            long cursor = stringStartPosition;
            while (underlyingBuffer[cursor++] != 0) 
            {
            }
            int difference = (int)(cursor - stringStartPosition) - 1;
            byte[] stringBytes = new byte[difference];
            Buffer.BlockCopy(underlyingBuffer, (int)stringStartPosition, stringBytes, 0, difference);
            return encoding.GetString(stringBytes);

            /*
            // significant performance bottleneck: (original)
            // read out a string, stopping at the first null terminator
            using (BinaryWriter writer = new BinaryWriter(new MemoryStream()))
            {
                while (true)
                {
                    byte nextByte = reader.ReadByte();
                    if (nextByte == 0)
                    {
                        byte[] stringRaw = ((MemoryStream)writer.BaseStream).ToArray();
                        return encoding.GetString(stringRaw);
                    }
                    else
                    {
                        writer.Write(nextByte);
                    }
                }
            }
            */
        }

        public bool CheckAndAdvanceBitflag(ref ulong bitFlag)
        {
            bool flagIsSet = (bitFlag & 1) > 0;
            bitFlag >>= 1;
            return flagIsSet;
        }

        public byte[]? ReadBitflagRawBytes(ref ulong bitFlags, int bytes) {
            if (CheckAndAdvanceBitflag(ref bitFlags)) {
                return ReadBytes(bytes);
            }
            return null;
        }

        public float? ReadBitflagFloat(ref ulong bitFlags)
        {
            if (CheckAndAdvanceBitflag(ref bitFlags))
            {
                byte[] floatBytes = ReadBytes(4);
                return BitConverter.ToSingle(floatBytes, 0);
            }
            return null;
        }

        public CaesarTable<C>? ReadBitflagSubTable<C>(CaesarObject parentObject, CTFLanguage language, ECU? currentEcu = null, bool autoRead = true) where C : CaesarObject, new()
        {
            var output = new CaesarTable<C>();

            int? blockOffset = ReadBitflagInt32(ref parentObject.Bitflags);
            int? entryCount = ReadBitflagInt32(ref parentObject.Bitflags);

            if (blockOffset != null && entryCount != null)
            {
                output.RelativeAddress = (int)blockOffset;
                output.EntryCount = (int)entryCount;
                output.Read(this, parentObject, language, currentEcu);
                return output;
            }
            return null;
        }

        public CaesarTable<C>? ReadBitflagSubTableAlt<C>(CaesarObject parentObject, CTFLanguage language, ECU? currentEcu = null, bool autoRead = true) where C : CaesarObject, new()
        {
            var output = new CaesarTable<C>();

            int? entryCount = ReadBitflagInt32(ref parentObject.Bitflags);
            int? blockOffset = ReadBitflagInt32(ref parentObject.Bitflags);

            if (blockOffset != null && entryCount != null)
            {
                output.RelativeAddress = (int)blockOffset;
                output.EntryCount = (int)entryCount;
                if (autoRead)
                {
                    output.Read(this, parentObject, language, currentEcu);
                }
                return output;
            }
            return null;
        }

        public CaesarPool? ReadBitflagPool(CaesarObject parentObject, CTFLanguage language, ECU? currentEcu = null)
        {
            int? entryCount = ReadBitflagInt32(ref parentObject.Bitflags);
            int? blockOffset = ReadBitflagInt32(ref parentObject.Bitflags);

            if (blockOffset != null && entryCount != null)
            {
                var output = new CaesarPool()
                {
                    RelativeAddress = (int)blockOffset,
                    EntryCount = (int)entryCount
                };
                output.Read(this, parentObject, language, currentEcu);
                return output;
            }
            return null;
        }

        public CaesarTable<C>? ReadBitflagTable<C>(CaesarObject parentObject, CTFLanguage language, ECU? currentEcu = null, bool autoRead = true) where C : CaesarObject, new()
        {
            int? blockOffset = ReadBitflagInt32(ref parentObject.Bitflags);
            int? entryCount = ReadBitflagInt32(ref parentObject.Bitflags);
            int? entrySize = ReadBitflagInt32(ref parentObject.Bitflags);
            int? blockSize = ReadBitflagInt32(ref parentObject.Bitflags);

            if (blockOffset != null && entryCount != null && entrySize != null)
            {
                var output = new CaesarTable<C>()
                {
                    RelativeAddress = (int)blockOffset,
                    EntryCount = (int)entryCount,
                    EntrySize = entrySize,
                    BlockSize = blockSize ?? (entryCount * entrySize)
                };
                if (autoRead)
                {
                    output.Read(this, parentObject, language, currentEcu);
                }
                return output;
            }
            return null;
        }

        public CaesarStringReference? ReadBitflagStringRef(ref ulong bitFlags, CTFLanguage language)
        {
            if (CheckAndAdvanceBitflag(ref bitFlags))
            {
                int id = ReadInt32();
                return new CaesarStringReference(language, id);
            }
            return null;
        }
        public int? ReadBitflagInt32(ref ulong bitFlags)
        {
            if (CheckAndAdvanceBitflag(ref bitFlags))
            {
                return ReadInt32();
            }
            return null;
        }
        public uint? ReadBitflagUInt32(ref ulong bitFlags)
        {
            if (CheckAndAdvanceBitflag(ref bitFlags))
            {
                return ReadUInt32();
            }
            return null;
        }
        public short? ReadBitflagInt16(ref ulong bitFlags)
        {
            if (CheckAndAdvanceBitflag(ref bitFlags))
            {
                return ReadInt16();
            }
            return null;
        }
        public ushort? ReadBitflagUInt16(ref ulong bitFlags)
        {
            if (CheckAndAdvanceBitflag(ref bitFlags))
            {
                return ReadUInt16();
            }
            return null;
        }
        public char? ReadBitflagInt8(ref ulong bitFlags)
        {
            if (CheckAndAdvanceBitflag(ref bitFlags))
            {
                return ReadChar();
            }
            return null;
        }
        public byte? ReadBitflagUInt8(ref ulong bitFlags)
        {
            if (CheckAndAdvanceBitflag(ref bitFlags))
            {
                return ReadByte();
            }
            return null;
        }

        public int ReadIntWithSize(int size, long offset)
        {
            BaseStream.Seek(offset, SeekOrigin.Begin);
            if (size == 1)
            {
                return ReadChar();
            }
            else if (size == 2)
            {
                return ReadInt16();
            }
            else if (size == 4)
            {
                return ReadInt32();
            }
            else
            {
                throw new NotImplementedException($"Requested an unknown integer size to read: {size}");
            }
        }
        public uint ReadUIntWithSize(int size, long offset)
        {
            BaseStream.Seek(offset, SeekOrigin.Begin);
            if (size == 1)
            {
                return ReadByte();
            }
            else if (size == 2)
            {
                return ReadUInt16();
            }
            else if (size == 4)
            {
                return ReadUInt32();
            }
            else
            {
                throw new NotImplementedException($"Requested an unknown integer size to read: {size}");
            }
        }

        private static uint[] CrcTable = { 
            0x00000000, 0x77073096, 0x0EE0E612C, 0x990951BA, 0x76DC419, 0x706AF48F, 0x0E963A535, 0x9E6495A3, 
            0x0EDB8832, 0x79DCB8A4, 0x0E0D5E91E, 0x97D2D988, 0x9B64C2B, 0x7EB17CBD, 0x0E7B82D07, 0x90BF1D91, 
            0x1DB71064, 0x6AB020F2, 0x0F3B97148, 0x84BE41DE, 0x1ADAD47D, 0x6DDDE4EB, 0x0F4D4B551, 0x83D385C7, 
            0x136C9856, 0x646BA8C0, 0x0FD62F97A, 0x8A65C9EC, 0x14015C4F, 0x63066CD9, 0x0FA0F3D63, 0x8D080DF5, 
            0x3B6E20C8, 0x4C69105E, 0x0D56041E4, 0x0A2677172, 0x3C03E4D1, 0x4B04D447, 0x0D20D85FD, 0x0A50AB56B, 
            0x35B5A8FA, 0x42B2986C, 0x0DBBBC9D6, 0x0ACBCF940, 0x32D86CE3, 0x45DF5C75, 0x0DCD60DCF, 0x0ABD13D59, 
            0x26D930AC, 0x51DE003A, 0x0C8D75180, 0x0BFD06116, 0x21B4F4B5, 0x56B3C423, 0x0CFBA9599, 0x0B8BDA50F, 
            0x2802B89E, 0x5F058808, 0x0C60CD9B2, 0x0B10BE924, 0x2F6F7C87, 0x58684C11, 0x0C1611DAB, 0x0B6662D3D, 
            0x76DC4190, 0x1DB7106, 0x98D220BC, 0x0EFD5102A, 0x71B18589, 0x6B6B51F, 0x9FBFE4A5, 0x0E8B8D433, 
            0x7807C9A2, 0x0F00F934, 0x9609A88E, 0x0E10E9818, 0x7F6A0DBB, 0x86D3D2D, 0x91646C97, 0x0E6635C01, 
            0x6B6B51F4, 0x1C6C6162, 0x856530D8, 0x0F262004E, 0x6C0695ED, 0x1B01A57B, 0x8208F4C1, 0x0F50FC457, 
            0x65B0D9C6, 0x12B7E950, 0x8BBEB8EA, 0x0FCB9887C, 0x62DD1DDF, 0x15DA2D49, 0x8CD37CF3, 0x0FBD44C65, 
            0x4DB26158, 0x3AB551CE, 0x0A3BC0074, 0x0D4BB30E2, 0x4ADFA541, 0x3DD895D7, 0x0A4D1C46D, 0x0D3D6F4FB, 
            0x4369E96A, 0x346ED9FC, 0x0AD678846, 0x0DA60B8D0, 0x44042D73, 0x33031DE5, 0x0AA0A4C5F, 0x0DD0D7CC9, 
            0x5005713C, 0x270241AA, 0x0BE0B1010, 0x0C90C2086, 0x5768B525, 0x206F85B3, 0x0B966D409, 0x0CE61E49F, 
            0x5EDEF90E, 0x29D9C998, 0x0B0D09822, 0x0C7D7A8B4, 0x59B33D17, 0x2EB40D81, 0x0B7BD5C3B, 0x0C0BA6CAD, 
            0x0EDB88320, 0x9ABFB3B6, 0x3B6E20C, 0x74B1D29A, 0x0EAD54739, 0x9DD277AF, 0x4DB2615, 0x73DC1683, 
            0x0E3630B12, 0x94643B84, 0x0D6D6A3E, 0x7A6A5AA8, 0x0E40ECF0B, 0x9309FF9D, 0x0A00AE27, 0x7D079EB1, 
            0x0F00F9344, 0x8708A3D2, 0x1E01F268, 0x6906C2FE, 0x0F762575D, 0x806567CB, 0x196C3671, 0x6E6B06E7, 
            0x0FED41B76, 0x89D32BE0, 0x10DA7A5A, 0x67DD4ACC, 0x0F9B9DF6F, 0x8EBEEFF9, 0x17B7BE43, 0x60B08ED5, 
            0x0D6D6A3E8, 0x0A1D1937E, 0x38D8C2C4, 0x4FDFF252, 0x0D1BB67F1, 0x0A6BC5767, 0x3FB506DD, 0x48B2364B, 
            0x0D80D2BDA, 0x0AF0A1B4C, 0x36034AF6, 0x41047A60, 0x0DF60EFC3, 0x0A867DF55, 0x316E8EEF, 0x4669BE79, 
            0x0CB61B38C, 0x0BC66831A, 0x256FD2A0, 0x5268E236, 0x0CC0C7795, 0x0BB0B4703, 0x220216B9, 0x5505262F, 
            0x0C5BA3BBE, 0x0B2BD0B28, 0x2BB45A92, 0x5CB36A04, 0x0C2D7FFA7, 0x0B5D0CF31, 0x2CD99E8B, 0x5BDEAE1D, 
            0x9B64C2B0, 0x0EC63F226, 0x756AA39C, 0x26D930A, 0x9C0906A9, 0x0EB0E363F, 0x72076785, 0x5005713, 
            0x95BF4A82, 0x0E2B87A14, 0x7BB12BAE, 0x0CB61B38, 0x92D28E9B, 0x0E5D5BE0D, 0x7CDCEFB7, 0x0BDBDF21, 
            0x86D3D2D4, 0x0F1D4E242, 0x68DDB3F8, 0x1FDA836E, 0x81BE16CD, 0x0F6B9265B, 0x6FB077E1, 0x18B74777, 
            0x88085AE6, 0x0FF0F6A70, 0x66063BCA, 0x11010B5C, 0x8F659EFF, 0x0F862AE69, 0x616BFFD3, 0x166CCF45, 
            0x0A00AE278, 0x0D70DD2EE, 0x4E048354, 0x3903B3C2, 0x0A7672661, 0x0D06016F7, 0x4969474D, 0x3E6E77DB, 
            0x0AED16A4A, 0x0D9D65ADC, 0x40DF0B66, 0x37D83BF0, 0x0A9BCAE53, 0x0DEBB9EC5, 0x47B2CF7F, 0x30B5FFE9, 
            0x0BDBDF21C, 0x0CABAC28A, 0x53B39330, 0x24B4A3A6, 0x0BAD03605, 0x0CDD70693, 0x54DE5729, 0x23D967BF, 
            0x0B3667A2E, 0x0C4614AB8, 0x5D681B02, 0x2A6F2B94, 0x0B40BBE37, 0x0C30C8EA1, 0x5A05DF1B, 0x2D02EF8D 
        };

        public static UInt32 CrcAccumulate(byte[] inputBuffer, uint currentChecksum = 0, int length = 0) 
        {
            length = length == 0 ? inputBuffer.Length : length;
            for (int i = 0; i < length; i++) 
            {
                uint tableIndex = (currentChecksum ^ inputBuffer[i]) & 0xFF;
                uint tableValue = CrcTable[tableIndex];
                // mix in the loaded byte into the crc on the most significant byte
                currentChecksum >>= 8;
                currentChecksum &= 0xFFFFFF;
                currentChecksum ^= tableValue;
            }

            return currentChecksum;
        }

        public static uint ComputeFileChecksum(byte[] fileBytes)
        {
            // caesar uses a 0x8000 block size
            // const int blockSize = 0x8000;
            const int blockSize = int.MaxValue;

            // skip the appended checksum
            fileBytes = fileBytes.Take(fileBytes.Length - 4).ToArray();

            int fileCursor = 0;
            uint currentChecksum = 0xFFFFFFFF;
            while (fileCursor < fileBytes.Length)
            {
                byte[] blockToRead = fileBytes.Skip(fileCursor).Take(blockSize).ToArray();
                fileCursor += blockSize;
                currentChecksum = CrcAccumulate(blockToRead, currentChecksum);
            }
            return currentChecksum;
        }
        public static uint ComputeFileChecksumLazy(byte[] fileBytes)
        {
            return CrcAccumulate(fileBytes, 0xFFFFFFFF, fileBytes.Length - 4);
        }
    }
}
