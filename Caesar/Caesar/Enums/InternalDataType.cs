using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Caesar.Enums
{
    public enum InternalDataType
    {
        Unknown,
        Invalid,
        Hex,
        Numeric,
        Enumerated,
        Raw, // unsure how to scale this
        Ascii,
        Unicode,
        Float
    }
}
