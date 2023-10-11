using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Caesar
{
    public class ECUInterface
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

        private long BaseAddress;

        public ECUInterface() 
        {
            BaseAddress = -1;
        }

        public ECUInterface(CaesarReader reader, CaesarContainer container, long baseAddress)
        {
            BaseAddress = baseAddress;
            reader.BaseStream.Seek(BaseAddress, SeekOrigin.Begin);

            // we can now properly operate on the interface block
            ulong interfaceBitflags = reader.ReadUInt32();

            Qualifier = reader.ReadBitflagStringWithReader(ref interfaceBitflags, BaseAddress);
            Name = reader.ReadBitflagStringRef(ref interfaceBitflags, container);
            Description = reader.ReadBitflagStringRef(ref interfaceBitflags, container);
            VersionString = reader.ReadBitflagStringWithReader(ref interfaceBitflags, BaseAddress);
            Version = reader.ReadBitflagInt32(ref interfaceBitflags);
            ComParamCount = reader.ReadBitflagInt32(ref interfaceBitflags);
            ComParamListOffset = reader.ReadBitflagInt32(ref interfaceBitflags);
            Unk6 = reader.ReadBitflagInt16(ref interfaceBitflags);


            if (ComParamListOffset != null && ComParamCount != null)
            {
                long comparamFileOffset = (long)ComParamListOffset + BaseAddress;
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
