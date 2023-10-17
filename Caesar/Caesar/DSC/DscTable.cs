using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;

namespace Caesar.DSC
{
    public class DscTable<T> : DscObject  where T : DscObject, new()
    {
        public int RelativeAddress;
        public int EntryCount;
        public List<T> Values = new List<T>();

        public virtual void Read(CaesarReader reader)
        {
            RelativeAddress = reader.ReadInt32();
            EntryCount = reader.ReadInt16();
            Values.Clear();
            long oldPos = reader.BaseStream.Position;
            reader.BaseStream.Seek(RelativeAddress, SeekOrigin.Begin);
            for(int i = 0; i < EntryCount; i++)
            {
                T newT = new T();
                newT.Read(reader);
                Values.Add(newT);
            }
            reader.BaseStream.Seek(oldPos, SeekOrigin.Begin);
        }
    }
}
