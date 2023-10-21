using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Caesar
{
    public class ComParameter : CaesarObject
    {
        public short? ComParamIndex;
        // this takes precedence over SubinterfaceIndex for KW2C3PE
        public short? ParentInterfaceIndex;
        public short? SubinterfaceIndex;
        public short? Unk5;
        public int? Unk_CTF;
        public short? Phrase;
        private int? DumpSize;
        public byte[]? Dump;
       
        public int ComParamValue;

        string? comParamName;
        public string ComParamName
        {
            get
            {
                if(comParamName == null)
                {
                    comParamName = GetComParamName();
                }
                return comParamName;
            }
            set => comParamName = value;
        }

        string GetComParamName()
        {
            ECU? parentEcu = GetParentByType<ECU>();
            if(parentEcu != null && parentEcu.ECUInterfaces != null && ParentInterfaceIndex != null && ParentInterfaceIndex < parentEcu.ECUInterfaces.Count)
            {
                var ecuInt = parentEcu.ECUInterfaces.GetObjects()[(int)ParentInterfaceIndex];
                if(ComParamIndex != null && ComParamIndex < ecuInt.ComParameterNames.Count)
                {
                    return ecuInt.ComParameterNames[(int)ComParamIndex];
                }
            }
			throw new Exception("Invalid communication parameter : parent interface has no matching key");
            // return "CP_UNKNOWN_PARAM";
        }

        public void Restore(CTFLanguage language) 
        {

        }

        public void PrintDebug() 
        {
            Console.WriteLine($"ComParam: id {ComParamIndex} name {ComParamName}, v {ComParamValue} 0x{ComParamValue:X8} SI_Index:{SubinterfaceIndex} | parentIndex:{ParentInterfaceIndex} 5:{Unk5} DumpSize:{DumpSize} D: {BitUtility.BytesToHex(Dump)}");
            Console.WriteLine($"Pos 0x{AbsoluteAddress:X}");
        }

        protected override void ReadData(CaesarReader reader, CaesarContainer container)
        {
            Bitflags = reader.ReadUInt16();
            ComParamIndex = reader.ReadBitflagInt16(ref Bitflags);
            ParentInterfaceIndex = reader.ReadBitflagInt16(ref Bitflags);
            SubinterfaceIndex = reader.ReadBitflagInt16(ref Bitflags);
            Unk5 = reader.ReadBitflagInt16(ref Bitflags);
            Unk_CTF = reader.ReadBitflagInt32(ref Bitflags); // no -1? ctf strings should have -1
            Phrase = reader.ReadBitflagInt16(ref Bitflags);
            DumpSize = reader.ReadBitflagInt32(ref Bitflags);
            Dump = reader.ReadBitflagDumpWithReader(ref Bitflags, DumpSize, AbsoluteAddress);
            ComParamValue = 0;
            if (Dump != null && DumpSize == 4)
            {
                ComParamValue = BitConverter.ToInt32(Dump, 0);
            }
            else 
            {
                throw new Exception("Unhandled dump in comparam");
            }
        }
		
		public void InsertIntoEcu(ECU parentEcu)
        {
            // KW2C3PE uses a different parent addressing style
            int parentIndex = -1;
            if (ParentInterfaceIndex != null)
            {
                parentIndex = (int)ParentInterfaceIndex;
            }
            else if (SubinterfaceIndex != null)
            {
                parentIndex = (int)SubinterfaceIndex;
            }
            else return;
            if (parentEcu.ECUInterfaceSubtypes != null)
            {
                if (ParentInterfaceIndex >= parentEcu.ECUInterfaceSubtypes.Count)
                {
                    throw new Exception("ComParam: tried to assign to nonexistent interface");
                }
                else
                {
                    // apparently it is possible to insert multiple of the same comparams..?
                    var parentList = parentEcu.ECUInterfaceSubtypes.GetObjects()[parentIndex].CommunicationParameters;
                    if (parentList.Count(x => x.ComParamName == ComParamName) > 0)
                    {
                        Console.WriteLine($"ComParam with existing key already exists, skipping insertion: {ComParamName} new: {ComParamValue}");
                    }
                    else
                    {
                        parentList.Add(this);
                    }
                }
            }
        }
    }
}
