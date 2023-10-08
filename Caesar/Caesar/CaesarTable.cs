using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Caesar
{
    public class CaesarTable<T>
        where T : CaesarObject, new()
    {
        public int BlockOffset { get; set; }

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
                return output;
            }
            return output;
        }

        public void Populate(CaesarReader reader, CTFLanguage language, ECU? currentEcu)
        {
            Objects.Clear();
            long originalOffset = reader.BaseStream.Position;
            reader.BaseStream.Seek((long)BlockOffset, System.IO.SeekOrigin.Begin);
            int offset = BlockOffset;
            for (int index = 0; index < EntryCount; index++, offset += BlockSize)
            {
                T obj = new T();
                obj.PoolIndex = index;
                obj.Read(reader, BlockOffset, language, currentEcu);
                Objects.Add(obj);
            }
            reader.BaseStream.Seek(originalOffset, System.IO.SeekOrigin.Begin);
        }

        public CaesarTable()
        {
            BlockOffset = 0;
            EntryCount = 0;
            EntrySize = 0;
            BlockSize = 0;
        }

        public CaesarTable(int blockOffset, int entryCount, int? entrySize, int? blockSize)
        {
            BlockOffset = blockOffset;
            EntryCount = entryCount;
            EntrySize = entrySize;
            BlockSize = blockSize;
        }
    }
}
