using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Caesar
{
    public abstract class CaesarObject
    {
        [JsonIgnore]
        protected CaesarObject? ParentObject { get; set; }

        // object's address relative to the last parent CaesarObject, this is what we read
        public int RelativeAddress { get; set; }

        // actual offset in the file to be read from
        [JsonIgnore]
        public int AbsoluteAddress { get; protected set; }

        public int PoolIndex { get; set; } = -1;

        internal ulong Bitflags = 0;

        protected virtual bool ReadHeader(CaesarReader reader)
        {
            if(ParentObject == null) return false;

            int? address = reader.ReadBitflagInt32(ref ParentObject.Bitflags);

            RelativeAddress = address ?? 0;

            return address != null;
        }

        public bool Read(CaesarReader reader, CaesarObject parentObject, CTFLanguage language, ECU? currentEcu)
        {
            ParentObject = parentObject;
            if(!ReadHeader(reader))
            {
                return false;
            }
            if(RelativeAddress != 0)
            {
                AbsoluteAddress = parentObject.AbsoluteAddress + RelativeAddress;
                long nextHeaderOffset = reader.BaseStream.Position;
                reader.BaseStream.Seek(AbsoluteAddress, SeekOrigin.Begin);
                ReadData(reader, language, currentEcu);
                reader.BaseStream.Seek(nextHeaderOffset, SeekOrigin.Begin);
            }
            return true;
        }

        protected abstract void ReadData(CaesarReader reader, CTFLanguage language, ECU? currentEcu);
    }
}
