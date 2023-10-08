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
        public int Address { get; set; }

        public int HeaderSize { get; set; }

        public int DataSize { get; set; }

        public int PoolIndex { get; set; } = -1;

        protected virtual void ReadHeader(CaesarReader reader, int baseOffset)
        {
            if(HeaderSize == 0)
            {
                HeaderSize = 8;
            }
            Address = reader.ReadInt32() + baseOffset;
            DataSize = reader.ReadInt32();
        }

        public void Read(CaesarReader reader, int baseOffset, CTFLanguage language, ECU? currentEcu)
        {
            long nextHeaderOffset = reader.BaseStream.Position;
            ReadHeader(reader, baseOffset);
            nextHeaderOffset += HeaderSize; // HeaderSize is guaranteed to be set after first call to ReadHeader
            if(Address != 0 && DataSize > 0)
            {
                reader.BaseStream.Seek(Address, SeekOrigin.Begin);
                ReadData(reader, language, currentEcu);
                reader.BaseStream.Seek(nextHeaderOffset, SeekOrigin.Begin);
            }
        }

        protected abstract void ReadData(CaesarReader reader, CTFLanguage language, ECU? currentEcu);
    }
}
