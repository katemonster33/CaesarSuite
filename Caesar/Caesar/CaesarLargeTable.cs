using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Caesar
{
    /// <summary>
    /// A 'large table in Caesar has address, entry count, entry size, and block size.
    /// </summary>
    public class CaesarLargeTable<T> : CaesarBasicTable<T> 
        where T : CaesarObject, new()
    {
        public int EntrySize { get; set; }

        public int BlockSize { get; set; }


        protected override bool ReadHeader(CaesarReader reader)
        {
            if(ParentObject == null) return false;

            bool baseSuccess = base.ReadHeader(reader);

            int? entrySize = reader.ReadBitflagInt32(ref ParentObject.Bitflags);
            EntrySize = entrySize ?? 0;

            int? blockSize = reader.ReadBitflagInt32(ref ParentObject.Bitflags);
            BlockSize = blockSize ?? EntrySize * EntryCount;

            return baseSuccess && entrySize != null;
        }

        public CaesarLargeTable()
        {
            RelativeAddress = 0;
            EntryCount = 0;
            EntrySize = 0;
            BlockSize = 0;
        }
    }
}
