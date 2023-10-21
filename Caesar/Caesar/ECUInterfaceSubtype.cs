using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;


namespace Caesar
{
    public class ECUInterfaceSubtype : CaesarObject
    {
        public enum PhysicalProtocolType : int
        {
            // one or more of these are probably D2B and Most
            Unknown = 0,
            KLINE = 1,
            LSCAN = 2,
            HOMING_PIGEONS = 3,
            HSCAN = 4,
        }
		
        public string? Qualifier;
        public int? Name_CTF;
        public int? Description_CTF;

        public short? ParentInterfaceIndex;
        public short? Unk4AlmostAlways1;

        public int? PhysicalProtocolRaw;
        public int? Unk6;
        public int? Unk7;

		// these 2 below params appear specific o kline/kw2000pe
        public byte? Unk8;
        public byte? Unk9;
        public char? Unk10; // might be signed, almost always -3?

        public List<ComParameter> CommunicationParameters = new List<ComParameter>();
        public PhysicalProtocolType PhysicalProtocol { get { return (PhysicalProtocolType)(PhysicalProtocolRaw ?? 0); } }


        public void Restore(CTFLanguage language) 
        {
            foreach (ComParameter cp in CommunicationParameters) 
            {
                cp.Restore(language);
            }
        }

        protected override void ReadData(CaesarReader reader, CaesarContainer container)
        {
            // we can now properly operate on the interface block
            Bitflags = reader.ReadUInt32();

            Qualifier = reader.ReadBitflagStringWithReader(ref Bitflags, AbsoluteAddress);
            Name_CTF = reader.ReadBitflagInt32(ref Bitflags);
            Description_CTF = reader.ReadBitflagInt32(ref Bitflags);

            ParentInterfaceIndex = reader.ReadBitflagInt16(ref Bitflags);
            Unk4AlmostAlways1 = reader.ReadBitflagInt16(ref Bitflags);

            PhysicalProtocolRaw = reader.ReadBitflagInt32(ref Bitflags);
            Unk6 = reader.ReadBitflagInt32(ref Bitflags);
            Unk7 = reader.ReadBitflagInt32(ref Bitflags);

            Unk8 = reader.ReadBitflagUInt8(ref Bitflags);
            Unk9 = reader.ReadBitflagUInt8(ref Bitflags);
            Unk10 = reader.ReadBitflagInt8(ref Bitflags); // might be signed
        }

        public void PrintDebug()
        {
            Console.WriteLine($"iface subtype: @ 0x{AbsoluteAddress:X}");
            Console.WriteLine($"{nameof(Name_CTF)} : {Name_CTF}");
            Console.WriteLine($"{nameof(Description_CTF)} : {Description_CTF}");
            Console.WriteLine($"{nameof(ParentInterfaceIndex)} : {ParentInterfaceIndex}");
            Console.WriteLine($"{nameof(Unk4AlmostAlways1)} : {Unk4AlmostAlways1}");
            Console.WriteLine($"{nameof(PhysicalProtocolRaw)} : {PhysicalProtocolRaw}");
            Console.WriteLine($"{nameof(Unk6)} : {Unk6}");
            Console.WriteLine($"{nameof(Unk7)} : {Unk7}");
            Console.WriteLine($"{nameof(Unk8)} : {Unk8}");
            Console.WriteLine($"{nameof(Unk9)} : {Unk9}");
            Console.WriteLine($"{nameof(Unk10)} : {Unk10}");
            Console.WriteLine($"CT: {Qualifier}");
        }
    }
}