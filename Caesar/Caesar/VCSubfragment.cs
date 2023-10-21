using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Caesar
{
    public class VCSubfragment : CaesarObject
    {
        public CaesarStringReference? Name;
        public byte[]? Dump;
        public CaesarStringReference? Description;
        public string? QualifierUsuallyDisabled;
        public int? Unk3;
        public int? Unk4;
        public string? SupplementKey;

        [System.Text.Json.Serialization.JsonIgnore]
        CTFLanguage Language;
        [System.Text.Json.Serialization.JsonIgnore]
        public VCFragment ParentFragment { get; set; }

        public void Restore(CTFLanguage language) 
        {
            Language = language;
        }

        public VCSubfragment() 
        {
            Language = new CTFLanguage();
            ParentFragment = new VCFragment();
        }

        protected override void ReadData(CaesarReader reader, CaesarContainer container)
        {
            ulong bitflags = reader.ReadUInt16();

            Name = reader.ReadBitflagStringRef(ref bitflags, container);
            if (ParentFragment.CCFHandle == 5) 
            {
                // fragment should be parsed as PBSGetDumpAsStringFn, though internally we perceive this as the same
            }
            Dump = reader.ReadBitflagDumpWithReader(ref bitflags, ParentFragment.VarcodeDumpSize, AbsoluteAddress);
            Description = reader.ReadBitflagStringRef(ref bitflags, container);
            QualifierUsuallyDisabled = reader.ReadBitflagStringWithReader(ref bitflags, AbsoluteAddress);
            Unk3 = reader.ReadBitflagInt32(ref bitflags);
            Unk4 = reader.ReadBitflagInt16(ref bitflags);
            SupplementKey = reader.ReadBitflagStringWithReader(ref bitflags, AbsoluteAddress);

            //int subfragmentIdk2 = reader.ReadInt32();
            //int subfragmentName = reader.ReadInt32();
            //int subfragmentIdkIncremented = reader.ReadInt32();
            //Console.WriteLine($"Subfragment: {subfragmentIdk1:X} {subfragmentIdk2:X} {language.GetString(subfragmentName)} {subfragmentIdkIncremented:X}");
            //PrintDebug();
        }

        private void PrintDebug(bool verbose = false) 
        {
            if (verbose)
            {
                Console.WriteLine("------------- subfragment ------------- ");
                Console.WriteLine($"{nameof(Name)}, {Name}");
                Console.WriteLine($"{nameof(Dump)}, {BitUtility.BytesToHex(Dump)}");
                Console.WriteLine($"{nameof(Description)}, {Description}");
                Console.WriteLine($"{nameof(QualifierUsuallyDisabled)}, {QualifierUsuallyDisabled}");
                Console.WriteLine($"{nameof(Unk3)}, {Unk3}");
                Console.WriteLine($"{nameof(Unk4)}, {Unk4}");
                Console.WriteLine($"{nameof(SupplementKey)}, {SupplementKey}");
            }
            else
            {
                Console.WriteLine($">> {BitUtility.BytesToHex(Dump)} : {Name}");
            }
        }
    }
}