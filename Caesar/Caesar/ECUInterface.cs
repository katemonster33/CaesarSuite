using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Caesar
{
    public class ECUInterface : CaesarObject
    {
        public string? Qualifier;
        public CaesarStringReference? Name;
        public CaesarStringReference? Description;
        public string? VersionString;
        public int? Version;
        private int? ComParamCount;
        private int? ComParamListOffset;
        public int? Unk6;
        

        public List<string> ComParameterNames = new List<string>();

        protected override void ReadData(CaesarReader reader, CaesarContainer container)
        {
            // we can now properly operate on the interface block
            Bitflags = reader.ReadUInt32();

            Qualifier = reader.ReadBitflagStringWithReader(ref Bitflags, AbsoluteAddress);
            Name = reader.ReadBitflagStringRef(ref Bitflags, container);
            Description = reader.ReadBitflagStringRef(ref Bitflags, container);
            VersionString = reader.ReadBitflagStringWithReader(ref Bitflags, AbsoluteAddress);
            Version = reader.ReadBitflagInt32(ref Bitflags);
            ComParamCount = reader.ReadBitflagInt32(ref Bitflags);
            ComParamListOffset = reader.ReadBitflagInt32(ref Bitflags);
            Unk6 = reader.ReadBitflagInt16(ref Bitflags);
			
            // absolute file offset to the comparam string table
            // this points to an array of integers
            // each of these ints is added to the fileoffset to get to the actual string

            if (ComParamListOffset != null && ComParamCount != null)
            {
                long comparamFileOffset = (long)ComParamListOffset + AbsoluteAddress;
                // Console.WriteLine($"interface string table offset from definition block : {interfaceStringTableOffset_fromDefinitionBlock:X}");

                for (int interfaceStringIndex = 0; interfaceStringIndex < ComParamCount; interfaceStringIndex++)
                {
                    // seek to string pointer
                    reader.BaseStream.Seek(comparamFileOffset + (interfaceStringIndex * 4), SeekOrigin.Begin);
                    // from pointer, seek to string
                    long interfaceStringReadoutPtr = reader.ReadInt32() + comparamFileOffset;
                    reader.BaseStream.Seek(interfaceStringReadoutPtr, SeekOrigin.Begin);
                    string comParameter = reader.ReadString();
                    ComParameterNames.Add(comParameter);
                }
            }
        }

        public void PrintDebug() 
        {
            Console.WriteLine($"{nameof(Qualifier)} : {Qualifier}");
            Console.WriteLine($"{nameof(Name)} : {Name?.Text}");
            Console.WriteLine($"{nameof(Description)} : {Description?.Text}");
            Console.WriteLine($"{nameof(VersionString)} : {VersionString}");
            Console.WriteLine($"{nameof(Version)} : {Version}");
            Console.WriteLine($"{nameof(ComParamCount)} : {ComParamCount}");
            Console.WriteLine($"{nameof(ComParamListOffset)} : 0x{ComParamListOffset:X}");
            Console.WriteLine($"{nameof(Unk6)} : {Unk6}");

            foreach (string comParameter in ComParameterNames)
            {
                Console.WriteLine($"InterfaceComParameter: {comParameter}");
            }
        }
    }
}