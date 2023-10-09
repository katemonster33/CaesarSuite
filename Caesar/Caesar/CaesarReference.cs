using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Caesar
{
    public class CaesarReference<T> where T : CaesarObject, new()
    {
        [Newtonsoft.Json.JsonIgnore]
        public CaesarLargeTable<T>? ReferenceTable { get; set; }
        public int Index { get; set; }
        public int? Count { get; set; }

        T? objRef;
        public virtual T? Value
        {
            get
            {
                if(objRef == null && ReferenceTable != null)
                {
                    objRef = ReferenceTable.GetSingle(Index);
                }
                return objRef;
            }
            set
            {
                objRef = value;
            }
        }

        public CaesarReference()
        {
            Index = 0;
        }

        public CaesarReference(int id, CaesarLargeTable<T> table)
        {
            Index = id;
            ReferenceTable = table;
        }
    }
}
