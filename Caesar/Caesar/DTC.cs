﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Caesar
{
    public class DTC : CaesarObject
    {
        public enum DTCStatusByte : uint
        {
            TestFailedAtRequestTime = 0x01,
            TestFailedAtCurrentCycle = 0x02,
            PendingDTC = 0x04,
            ConfirmedDTC = 0x08,
            TestIncompleteSinceLastClear = 0x10,
            TestFailedSinceLastClear = 0x20,
            TestIncompleteAtCurrentCycle = 0x40,
            WarningIndicatorActive = 0x80,
        }
        public int DataSize;
        public int CRC;

        // see : const char *__cdecl DIGetComfortErrorCode(DI_ECUINFO *ecuh, unsigned int dtcIndex)
        public string? Qualifier;

        public CaesarStringReference? Description;
        public CaesarStringReference? Reference;

        public int? XrefStart = -1;
        public int? XrefCount = -1;

        public DTC()
        {
            CRC = -1;
        }

        public static DTC? FindDTCById(string id, ECUVariant variant)
        {
            foreach (DTC dtc in variant.DTCs)
            {
                if (dtc.Qualifier != null && dtc.Qualifier.EndsWith(id))
                {
                    return dtc;
                }
            }
            return null;
        }

        public override string ToString() 
        {
            return $"DTC: {Qualifier}: {Description} : {Reference}";
        }

        protected override bool ReadHeader(CaesarReader reader)
        {
            RelativeAddress = reader.ReadInt32();
            DataSize = reader.ReadInt32();
            CRC = reader.ReadInt32();
            return true;
        }

        protected override void ReadData(CaesarReader reader, CTFLanguage language, ECU? currentEcu)
        {
            ulong bitflags = reader.ReadUInt16();

            Qualifier = reader.ReadBitflagStringWithReader(ref bitflags, AbsoluteAddress);

            Description = reader.ReadBitflagStringRef(ref bitflags, language);
            Reference = reader.ReadBitflagStringRef(ref bitflags, language);
#if DEBUG
            if (bitflags > 0)
            {
                Console.WriteLine($"DTC {Qualifier} has additional unparsed fields : 0x{bitflags:X}");
            }
#endif
        }
    }
}
