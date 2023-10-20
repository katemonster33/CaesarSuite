using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Caesar.Enums
{
    [Flags]
    public enum ServiceTypes
    {
        None =              0x00000000,
        Actuator =          0x00000001,
        Adjustment =        0x00000002,
        Data =              0x00000010,
        Download =          0x00000040,
        Environment =       0x00000080,
        Function =          0x00000200,
        Static =            0x00000400,
        DiagJob =           0x00040000,
        Security =          0x00080000,
        Global =            0x00010000,
        IOControl =         0x00800000,
        Session =           0x00100000,
        StoredData =        0x00200000,
        Routine =           0x00400000,
        WriteVarCode =      0x02000000,
        ReadVarCode =       0x04000000,
    }
}
