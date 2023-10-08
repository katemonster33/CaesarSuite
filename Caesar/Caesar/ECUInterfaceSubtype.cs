using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;


namespace Caesar
{
    public class ECUInterfaceSubtype
    {
        public enum ParamName 
        {
            CP_BAUDRATE,
            CP_GLOBAL_REQUEST_CANIDENTIFIER,
            CP_FUNCTIONAL_REQUEST_CANIDENTIFIER,
            CP_REQUEST_CANIDENTIFIER,
            CP_RESPONSE_CANIDENTIFIER,
            CP_PARTNUMBERID,
            CP_PARTBLOCK,
            CP_HWVERSIONID,
            CP_SWVERSIONID,
            CP_SWVERSIONBLOCK,
            CP_SUPPLIERID,
            CP_SWSUPPLIERBLOCK,
            CP_ADDRESSMODE,
            CP_ADDRESSEXTENSION,
            CP_ROE_RESPONSE_CANIDENTIFIER,
            CP_USE_TIMING_RECEIVED_FROM_ECU,
            CP_STMIN_SUG,
            CP_BLOCKSIZE_SUG,
            CP_P2_TIMEOUT,
            CP_S3_TP_PHYS_TIMER,
            CP_S3_TP_FUNC_TIMER,
            CP_BR_SUG,
            CP_CAN_TRANSMIT,
            CP_BS_MAX,
            CP_CS_MAX,
            CPI_ROUTINECOUNTER,
            CP_REQREPCOUNT,
            // looks like outliers?
            CP_P2_EXT_TIMEOUT_7F_78,
            CP_P2_EXT_TIMEOUT_7F_21,
        }

        public string? Qualifier;
        public int? Name_CTF;
        public int? Description_CTF;

        public short? Unk3;
        public short? Unk4;

        public int? Unk5;
        public int? Unk6;
        public int? Unk7;

        public byte? Unk8;
        public byte? Unk9;
        public char? Unk10; // might be signed

        private long BaseAddress;
        private int Index;

        public List<ComParameter> CommunicationParameters = new List<ComParameter>();
        private CTFLanguage Language;

        public void Restore(CTFLanguage language) 
        {
            Language = language;
            foreach (ComParameter cp in CommunicationParameters) 
            {
                cp.Restore(language);
            }
        }

        public ECUInterfaceSubtype() 
        {
            Index = -1;
            BaseAddress = -1;
            Language = new CTFLanguage();
        }

        public ECUInterfaceSubtype(CaesarReader reader, long baseAddress, int index, CTFLanguage language)
        {
            Index = index;
            BaseAddress = baseAddress;
            Language = language;
            reader.BaseStream.Seek(baseAddress, SeekOrigin.Begin);
            // we can now properly operate on the interface block
            ulong ctBitflags = reader.ReadUInt32();

            Qualifier = reader.ReadBitflagStringWithReader(ref ctBitflags, BaseAddress);
            Name_CTF = reader.ReadBitflagInt32(ref ctBitflags);
            Description_CTF = reader.ReadBitflagInt32(ref ctBitflags);

            Unk3 = reader.ReadBitflagInt16(ref ctBitflags);
            Unk4 = reader.ReadBitflagInt16(ref ctBitflags);

            Unk5 = reader.ReadBitflagInt32(ref ctBitflags);
            Unk6 = reader.ReadBitflagInt32(ref ctBitflags);
            Unk7 = reader.ReadBitflagInt32(ref ctBitflags);

            Unk8 = reader.ReadBitflagUInt8(ref ctBitflags);
            Unk9 = reader.ReadBitflagUInt8(ref ctBitflags);
            Unk10 = reader.ReadBitflagInt8(ref ctBitflags); // might be signed
        }

        public ComParameter? GetComParameterByName(string paramName) 
        {
            return CommunicationParameters.Find(x => x.ParamName == paramName);
        }

        public int? GetComParameterValue(ParamName name)
        {
            var comParam = GetComParameterByName(name.ToString());
            if (comParam == null)
            {
                return null;
            }
            else
            {
                return comParam.ComParamValue;
            }
        }

        public void PrintDebug()
        {
            Console.WriteLine($"iface subtype: @ 0x{BaseAddress:X}");
            Console.WriteLine($"{nameof(Name_CTF)} : {Name_CTF}");
            Console.WriteLine($"{nameof(Description_CTF)} : {Description_CTF}");
            Console.WriteLine($"{nameof(Unk3)} : {Unk3}");
            Console.WriteLine($"{nameof(Unk4)} : {Unk4}");
            Console.WriteLine($"{nameof(Unk5)} : {Unk5}");
            Console.WriteLine($"{nameof(Unk6)} : {Unk6}");
            Console.WriteLine($"{nameof(Unk7)} : {Unk7}");
            Console.WriteLine($"{nameof(Unk8)} : {Unk8}");
            Console.WriteLine($"{nameof(Unk9)} : {Unk9}");
            Console.WriteLine($"{nameof(Unk10)} : {Unk10}");
            Console.WriteLine($"CT: {Qualifier}");
        }
    }
}
