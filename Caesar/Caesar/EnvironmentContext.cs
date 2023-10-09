using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Caesar
{
    public class EnvironmentContext : DiagService
    {
        protected override bool ReadHeader(CaesarReader reader)
        {
            RelativeAddress = reader.ReadInt32();
            DataSize = reader.ReadInt32();
            return true;
        }
    }
}
