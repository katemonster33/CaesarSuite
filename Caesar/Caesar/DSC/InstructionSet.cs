using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Caesar.DSC
{
    public class InstructionSet : DscObject
    {
        public int DataSize = 0;
        public byte[] Dump = new byte[0];
        public void Read(CaesarReader reader)
        {
            Dump = reader.ReadBytes(DataSize);
        }
    }
}
