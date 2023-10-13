using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;

namespace Caesar.DSC
{
    public class DscList<T> : CaesarTable<T> where T : CaesarObject, new()
    {
        public List<GlobalVariable> GlobalVariables = new List<GlobalVariable>();

        protected override bool ReadHeader(CaesarReader reader)
        {
            RelativeAddress = reader.ReadInt32(); // ?? @ 22
            EntryCount = reader.ReadInt16(); // ?? @ 26
            return true;
        }
    }
}
