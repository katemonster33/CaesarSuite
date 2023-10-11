using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;

namespace Caesar
{
    public class ECUVariantPattern : CaesarObject
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

        public void Restore() 
        {
        
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

        protected override void ReadData(CaesarReader reader, CaesarContainer container)
        {
            Bitflags = reader.ReadUInt32();

            UnkBufferSize = reader.ReadBitflagInt32(ref Bitflags);

            UnkBuffer = reader.ReadBitflagDumpWithReader(ref Bitflags, UnkBufferSize, AbsoluteAddress);
            Unk3 = reader.ReadBitflagInt32(ref Bitflags);
            Unk4 = reader.ReadBitflagInt32(ref Bitflags);
            Unk5 = reader.ReadBitflagInt32(ref Bitflags);
            VendorName = reader.ReadBitflagStringWithReader(ref Bitflags, AbsoluteAddress);

            KwpVendorID = reader.ReadBitflagUInt16(ref Bitflags);
            Unk8 = reader.ReadBitflagInt16(ref Bitflags);
            Unk9 = reader.ReadBitflagInt16(ref Bitflags);
            Unk10 = reader.ReadBitflagInt16(ref Bitflags);

            Unk11 = reader.ReadBitflagUInt8(ref Bitflags);
            Unk12 = reader.ReadBitflagUInt8(ref Bitflags);
            Unk13 = reader.ReadBitflagUInt8(ref Bitflags);
            Unk14 = reader.ReadBitflagUInt8(ref Bitflags);
            Unk15 = reader.ReadBitflagUInt8(ref Bitflags);

            EcuId = reader.ReadBitflagRawBytes(ref Bitflags, 4);

            Unk17 = reader.ReadBitflagUInt8(ref Bitflags);
            Unk18 = reader.ReadBitflagUInt8(ref Bitflags);
            Unk19 = reader.ReadBitflagUInt8(ref Bitflags);
            Unk20 = reader.ReadBitflagUInt8(ref Bitflags);

            Unk21 = reader.ReadBitflagStringWithReader(ref Bitflags, AbsoluteAddress);

            Unk22 = reader.ReadBitflagInt32(ref Bitflags);
            Unk23 = reader.ReadBitflagInt32(ref Bitflags);
            UdsVendorID = reader.ReadBitflagInt32(ref Bitflags);
            PatternType = reader.ReadBitflagInt32(ref Bitflags);

            VariantID = UdsVendorID == 0 ? KwpVendorID : UdsVendorID;
        }
    }
}
