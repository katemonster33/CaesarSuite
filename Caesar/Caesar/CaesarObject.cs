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
        protected CaesarContainer? Container;
        [System.Text.Json.Serialization.JsonIgnore]
        protected CaesarObject? ParentObject { get; set; }

        // object's address relative to the last parent CaesarObject, this is what we read
        public int RelativeAddress { get; set; } = -1;

        // actual offset in the file to be read from
        [System.Text.Json.Serialization.JsonIgnore]
        public int AbsoluteAddress { get; protected set; }

        public int PoolIndex { get; set; } = 0;

        internal ulong Bitflags = 0;

        protected virtual bool ReadHeader(CaesarReader reader)
        {
            RelativeAddress = reader.ReadInt32();
            return true;
        }

        public T? GetParentByType<T>() where T : CaesarObject
        {
            CaesarObject? parent = ParentObject;
            while(parent != null)
            {
                if(parent.GetType() == typeof(T))
                {
                    return (T)parent;
                }
                parent = parent.ParentObject;
            }
            return null;
        }

        public bool Read(CaesarReader reader, CaesarObject? parentObject, CaesarContainer container)
        {
            ParentObject = parentObject;
            Container = container;
            if(!ReadHeader(reader))
            {
                return false;
            }
            if(RelativeAddress != -1)
            {
                AbsoluteAddress = (parentObject != null ? parentObject.AbsoluteAddress : 0) + RelativeAddress;
                long nextHeaderOffset = reader.BaseStream.Position;
                reader.BaseStream.Seek(AbsoluteAddress, SeekOrigin.Begin);
                ReadData(reader, container);
                reader.BaseStream.Seek(nextHeaderOffset, SeekOrigin.Begin);
            }
            return true;
        }
        
        protected abstract void ReadData(CaesarReader reader, CaesarContainer container);
    }
}
