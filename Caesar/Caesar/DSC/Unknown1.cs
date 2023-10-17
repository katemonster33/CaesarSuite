using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Caesar.DSC
{
    public class Unknown1 : DscObject
    {
        public ushort UnknownIndex = 0;


        public void Read(CaesarReader reader)
        {
            this.UnknownIndex = reader.ReadUInt16();
        }
    }
}
