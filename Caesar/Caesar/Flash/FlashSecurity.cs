using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Caesar
{
    public class FlashSecurity
    {
        public short? MethodValueType;
        public int? MethodSize;
        public byte[]? MethodValue;

        public short? SignatureValueType;
        public int? SignatureSize;
        public byte[]? SignatureValue;

        public short? ChecksumValueType;
        public int? ChecksumSize;
        public byte[]? ChecksumValue;

        public short? EcuKeyValueType;
        public int? EcuKeySize;
        public byte[]? EcuKeyValue;

        public long BaseAddress;

        public FlashSecurity(CaesarReader reader, long baseAddress)
        {
            BaseAddress = baseAddress;
            reader.BaseStream.Seek(baseAddress, SeekOrigin.Begin);

            ulong bitFlags = reader.ReadUInt16();

            MethodValueType = reader.ReadBitflagInt16(ref bitFlags);
            MethodSize = reader.ReadBitflagInt32(ref bitFlags);
            MethodValue = reader.ReadBitflagDumpWithReader(ref bitFlags, MethodSize, baseAddress);

            SignatureValueType = reader.ReadBitflagInt16(ref bitFlags);
            SignatureSize = reader.ReadBitflagInt32(ref bitFlags);
            SignatureValue = reader.ReadBitflagDumpWithReader(ref bitFlags, SignatureSize, baseAddress);

            ChecksumValueType = reader.ReadBitflagInt16(ref bitFlags);
            ChecksumSize = reader.ReadBitflagInt32(ref bitFlags);
            ChecksumValue = reader.ReadBitflagDumpWithReader(ref bitFlags, ChecksumSize, baseAddress);

            EcuKeyValueType = reader.ReadBitflagInt16(ref bitFlags);
            EcuKeySize = reader.ReadBitflagInt32(ref bitFlags);
            EcuKeyValue = reader.ReadBitflagDumpWithReader(ref bitFlags, EcuKeySize, baseAddress);
        }

        public void PrintDebug()
        {
            Console.WriteLine($"{nameof(MethodValueType)} : 0x{MethodValueType:X}");
            Console.WriteLine($"{nameof(MethodSize)} : 0x{MethodSize:X}");
            Console.WriteLine($"{nameof(MethodValue)} : {BitUtility.BytesToHex(MethodValue)}");

            Console.WriteLine($"{nameof(SignatureValueType)} : 0x{SignatureValueType:X}");
            Console.WriteLine($"{nameof(SignatureSize)} : 0x{SignatureSize:X}");
            Console.WriteLine($"{nameof(SignatureValue)} : {BitUtility.BytesToHex(SignatureValue)}");

            Console.WriteLine($"{nameof(ChecksumValueType)} : 0x{ChecksumValueType:X}");
            Console.WriteLine($"{nameof(ChecksumSize)} : 0x{ChecksumSize:X}");
            Console.WriteLine($"{nameof(ChecksumValue)} : {BitUtility.BytesToHex(ChecksumValue)}");

            Console.WriteLine($"{nameof(EcuKeyValueType)} : 0x{EcuKeyValueType:X}");
            Console.WriteLine($"{nameof(EcuKeySize)} : 0x{EcuKeySize:X}");
            Console.WriteLine($"{nameof(EcuKeyValue)} : {BitUtility.BytesToHex(EcuKeyValue)}");
        }
    }
}
