using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Caesar
{
    /// <summary>
    /// Here I define a 'basic' table in Caesar as a table referenced only by offset and entry count.
    /// </summary>
    public class CaesarTable<T> : CaesarObject
        where T : CaesarObject, new ()
    {
        public int EntryCount { get; set; }

        public int? EntrySize { get; set; }

        public int? BlockSize { get; set; }

        public List<T> Objects { get; set; } = new List<T>();

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
            return RelativeAddress != -1;
        }

        protected override void ReadData(CaesarReader reader, CaesarContainer container)
        {
            Objects.Clear();
            for (int index = 0; index < EntryCount; index++)
            {
                T obj = new T();
                obj.PoolIndex = index;
                obj.Read(reader, this, container);
                Objects.Add(obj);
            }
        }

        public CaesarTable()
        {
            RelativeAddress = -1;
            EntryCount = 0;
        }
    }
}
