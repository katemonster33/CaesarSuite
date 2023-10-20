using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Caesar.Enums
{
    public enum ParamType
    {
        UByte,
        UWord,
        ULong,
        SByte,
        SWord,
        SLong,
        Float,
        UByteP,
        UWordP,
        ULongP,
        SByteP,
        SWordP,
        SLongP,
        FloatP,
        Bool,
        Dump,
        APtr,
        String,
        Choice,
        Special,
        Unknown, // Seems to imply a generic scaled value
        StringId,
        Unicode,
        Constant
    }
}
