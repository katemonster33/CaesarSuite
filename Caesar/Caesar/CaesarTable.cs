using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Caesar
{
    public class CaesarTable<T> : CaesarObject
        where T : CaesarObject, new()
    {
        public int EntryCount { get; set; }

        public int? EntrySize { get; set; }

        public int? BlockSize { get; set; }

        protected List<T> Objects { get; set; } = new List<T>();

        public List<T> GetObjects()
        {
            return new List<T>(Objects);
        }

        [JsonIgnore]
        public int Count => Objects.Count;

        public T? GetSingle(int index)
        {
            if (index < Objects.Count)
            {
                return Objects[index];
            }
            return default(T?);
        }

        public List<T> GetMultiple(int index, int count)
        {
            List<T> output = new List<T>(count);
            if ((index + count) < Objects.Count)
            {
                for (int i = 0; i < count; i++)
                {
                    output.Add(Objects[i + index]);
                }
            }
            return output;
        }

        protected override bool ReadHeader(CaesarReader reader)
        {
            if(ParentObject == null) return false;

            bool baseSuccess = base.ReadHeader(reader);

            int? entryCount = reader.ReadBitflagInt32(ref ParentObject.Bitflags);
            EntryCount = entryCount ?? 0;

            EntrySize = reader.ReadBitflagInt32(ref ParentObject.Bitflags);

            BlockSize = reader.ReadBitflagInt32(ref ParentObject.Bitflags);

            return baseSuccess && entryCount != null;
        }

        protected override void ReadData(CaesarReader reader,  CTFLanguage language, ECU? currentEcu)
        {
            Objects.Clear();
            for (int index = 0; index < EntryCount; index++)
            {
                T obj = new T();
                obj.PoolIndex = index;
                obj.Read(reader, this, language, currentEcu);
                Objects.Add(obj);
            }
        }

        public CaesarTable()
        {
            Address = 0;
            EntryCount = 0;
        }
    }
}
