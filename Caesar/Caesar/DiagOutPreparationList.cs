using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Caesar
{
    public class DiagOutPreparationList : CaesarTable<DiagPreparation>
    {
        protected override bool ReadHeader(CaesarReader reader)
        {
            EntryCount = reader.ReadInt32();

            RelativeAddress = reader.ReadInt32();

            return true;
        }
    }
}
