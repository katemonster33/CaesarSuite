using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Caesar
{
    public class CaesarListReference<T> where T : CaesarObject, new()
    {
        [Newtonsoft.Json.JsonIgnore]
        public CaesarLargeTable<T>? ReferenceTable { get; set; }
        public int Index { get; set; }
        public int Count { get; set; }

        List<T>? multiRef;
        public virtual List<T>? Value
        {
            get
            {
                if (multiRef == null && ReferenceTable != null)
                {
                    multiRef = ReferenceTable.GetMultiple(Index, Count);
                }
                return multiRef;
            }
            set
            {
                multiRef = value;
            }
        }

        public CaesarListReference()
        {
            Index = 0;
            Count = 0;
        }

        public CaesarListReference(int index, int count, CaesarLargeTable<T> table)
        {
            Index = index;
            Count = count;
            ReferenceTable = table;
        }
    }
}
