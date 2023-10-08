using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;

namespace Caesar
{
    public class ECUVariantPattern
    {

        public int? UnkBufferSize;

        public byte[]? UnkBuffer;
        public int? Unk3;
        public int? Unk4;
        public int? Unk5;
        public string? VendorName;

        public int? KwpVendorID;
        public int? Unk8;
        public int? Unk9;
        public int? Unk10;

        public int? Unk11;
        public int? Unk12;
        public int? Unk13;
        public int? Unk14;
        public int? Unk15;

        public byte[]? EcuId;

        public int? Unk17;
        public int? Unk18;
        public int? Unk19;
        public int? Unk20;

        public string? Unk21;

        public int? Unk22;
        public int? Unk23;
        public int? UdsVendorID;
        public int? PatternType;

        public int? VariantID;

        private readonly long BaseAddress;

        public void Restore() 
        {
        
        }

        public ECUVariantPattern() { }

        public ECUVariantPattern(CaesarReader reader, long baseAddress) 
        {
            BaseAddress = baseAddress;
            reader.BaseStream.Seek(baseAddress, SeekOrigin.Begin);
            ulong bitflags = reader.ReadUInt32();

            UnkBufferSize = reader.ReadBitflagInt32(ref bitflags);

            UnkBuffer = reader.ReadBitflagDumpWithReader(ref bitflags, UnkBufferSize, baseAddress);
            Unk3 = reader.ReadBitflagInt32(ref bitflags);
            Unk4 = reader.ReadBitflagInt32(ref bitflags);
            Unk5 = reader.ReadBitflagInt32(ref bitflags);
            VendorName = reader.ReadBitflagStringWithReader(ref bitflags, baseAddress);

            KwpVendorID = reader.ReadBitflagUInt16(ref bitflags);
            Unk8 = reader.ReadBitflagInt16(ref bitflags);
            Unk9 = reader.ReadBitflagInt16(ref bitflags);
            Unk10 = reader.ReadBitflagInt16(ref bitflags);

            Unk11 = reader.ReadBitflagUInt8(ref bitflags);
            Unk12 = reader.ReadBitflagUInt8(ref bitflags);
            Unk13 = reader.ReadBitflagUInt8(ref bitflags);
            Unk14 = reader.ReadBitflagUInt8(ref bitflags);
            Unk15 = reader.ReadBitflagUInt8(ref bitflags);

            EcuId = reader.ReadBitflagRawBytes(ref bitflags, 4);

            Unk17 = reader.ReadBitflagUInt8(ref bitflags);
            Unk18 = reader.ReadBitflagUInt8(ref bitflags);
            Unk19 = reader.ReadBitflagUInt8(ref bitflags);
            Unk20 = reader.ReadBitflagUInt8(ref bitflags);

            Unk21 = reader.ReadBitflagStringWithReader(ref bitflags, baseAddress);

            Unk22 = reader.ReadBitflagInt32(ref bitflags);
            Unk23 = reader.ReadBitflagInt32(ref bitflags);
            UdsVendorID = reader.ReadBitflagInt32(ref bitflags);
            PatternType = reader.ReadBitflagInt32(ref bitflags);

            VariantID = UdsVendorID == 0 ? KwpVendorID : UdsVendorID;
            // type 3 contains a vendor name

        }

        public void PrintDebug() 
        {
            Console.WriteLine($"UnkBufferSize : {UnkBufferSize}");
            Console.WriteLine($"UnkBuffer : {UnkBuffer}");
            Console.WriteLine($"Unk3 : {Unk3}");
            Console.WriteLine($"Unk4 : {Unk4}");
            Console.WriteLine($"Unk5 : {Unk5}");
            Console.WriteLine($"VendorName : {VendorName}");
            Console.WriteLine($"Unk7 : {KwpVendorID}");
            Console.WriteLine($"Unk8 : {Unk8}");
            Console.WriteLine($"Unk9 : {Unk9}");
            Console.WriteLine($"Unk10 : {Unk10}");
            Console.WriteLine($"Unk11 : {Unk11}");
            Console.WriteLine($"Unk12 : {Unk12}");
            Console.WriteLine($"Unk13 : {Unk13}");
            Console.WriteLine($"Unk14 : {Unk14}");
            Console.WriteLine($"Unk15 : {Unk15}");
            Console.WriteLine($"EcuId : {EcuId}");
            Console.WriteLine($"Unk17 : {Unk17}");
            Console.WriteLine($"Unk18 : {Unk18}");
            Console.WriteLine($"Unk19 : {Unk19}");
            Console.WriteLine($"Unk20 : {Unk20}");
            Console.WriteLine($"Unk21 : {Unk21}");
            Console.WriteLine($"Unk22 : {Unk22}");
            Console.WriteLine($"Unk23 : {Unk23}");
            Console.WriteLine($"VariantID : {VariantID}");
            Console.WriteLine($"PatternType : {PatternType}");
        }
    }
}
