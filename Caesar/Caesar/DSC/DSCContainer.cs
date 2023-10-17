using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Caesar.DSC
{
    public class DSCContainer : CaesarObject
    {
        public DSCFile DscFile = new DSCFile();
        public int DataSize = 0;

        protected override bool ReadHeader(CaesarReader reader)
        {
            base.ReadHeader(reader);
            DataSize = reader.ReadInt32();
            return true;
        }

        protected override void ReadData(CaesarReader reader, CaesarContainer container)
        {
            byte[] dscBytes = reader.ReadBytes(DataSize);
            CaesarReader dscReader = new CaesarReader(new MemoryStream(dscBytes, 0, dscBytes.Length, false, true));
            DscFile.Read(dscReader);
        }

        
    }
}
