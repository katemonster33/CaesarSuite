using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Caesar
{
    public class CaesarPool : CaesarObject
    {
        public int EntryCount { get; set; }

        protected List<int> Indices { get; set; } = new List<int>();

        public List<int> GetPoolIndices()
        {
            return new List<int>(Indices);
        }

        [JsonIgnore]
        public int Count => Indices.Count;

        protected override bool ReadHeader(CaesarReader reader)
        {
            return true;
        }

        protected override void ReadData(CaesarReader reader, CaesarContainer container)
        {
            Indices.Clear();
            for (int index = 0; index < EntryCount; index++)
            {
                Indices.Add(reader.ReadInt32());
            }
        }

        public CaesarPool()
        {
            RelativeAddress = -1;
            EntryCount = 0;
        }
    }
}
