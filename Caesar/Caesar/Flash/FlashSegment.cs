using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Caesar
{
    public class FlashSegment
    {
        public int? FromAddress;
        public int? SegmentLength;
        public int? Unk3;
        public string? SegmentName;

        public int? Unk5;
        public int? Unk6;
        public int? Unk7;

        /*
            0x1b [2,  4,4,4,4,  4,4,4],
         
         */
        public long BaseAddress;

        public FlashSegment(CaesarReader reader, long baseAddress)
        {
            BaseAddress = baseAddress;
            reader.BaseStream.Seek(baseAddress, SeekOrigin.Begin);

            ulong bitFlags = reader.ReadUInt16();

            // start reading

            FromAddress = reader.ReadBitflagInt32(ref bitFlags);
            SegmentLength = reader.ReadBitflagInt32(ref bitFlags);
            Unk3 = reader.ReadBitflagInt32(ref bitFlags);
            SegmentName = reader.ReadBitflagStringWithReader(ref bitFlags, BaseAddress);

            Unk5 = reader.ReadBitflagInt32(ref bitFlags);
            Unk6 = reader.ReadBitflagInt32(ref bitFlags);
            Unk7 = reader.ReadBitflagInt32(ref bitFlags);
        }

        public long GetMappedAddressFileOffset(CaesarReader reader)
        {
            reader.BaseStream.Seek(BaseAddress, SeekOrigin.Begin);

            ulong bitFlags = reader.ReadUInt16();

            if (reader.CheckAndAdvanceBitflag(ref bitFlags))
            {
                return reader.BaseStream.Position;
            }
            else
            {
                return -1;
            }
        }
        public long GetSegmentLengthFileOffset(CaesarReader reader)
        {
            reader.BaseStream.Seek(BaseAddress, SeekOrigin.Begin);

            ulong bitFlags = reader.ReadUInt16();

            reader.ReadBitflagInt32(ref bitFlags); // skip FromAddress
            if (reader.CheckAndAdvanceBitflag(ref bitFlags))
            {
                return reader.BaseStream.Position;
            }
            else
            {
                return -1;
            }
        }

        public void PrintDebug()
        {
            Console.WriteLine($"{nameof(FromAddress)} : 0x{FromAddress:X}");
            Console.WriteLine($"{nameof(SegmentLength)} : 0x{SegmentLength:X}");
            Console.WriteLine($"{nameof(Unk3)} : {Unk3}");
            Console.WriteLine($"{nameof(SegmentName)} : {SegmentName}");


            Console.WriteLine($"{nameof(Unk5)} : {Unk5}");
            Console.WriteLine($"{nameof(Unk6)} : {Unk6}");
            Console.WriteLine($"{nameof(Unk7)} : {Unk7}");
        }
    }
}
