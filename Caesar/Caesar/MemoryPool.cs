using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;

namespace Caesar
{
    public class MemoryPool
    {
        int blockOffset; // 1
        int entryCount;
        int entrySize;
        int blockSize;

        public MemoryPool(int blockOffset, int entryCount, int entrySize, int blockSize)
        {
            this.blockOffset = blockOffset;
            this.entryCount = entryCount;
            this.entrySize = entrySize;
            this.blockSize = blockSize;
        }

        public static MemoryPool? TryCreateMemPool(CaesarReader reader, ref ulong ecuBitFlags, int startOffset)
        {
            int? blockOffset = reader.ReadBitflagInt32(ref ecuBitFlags);
            int? entryCount = reader.ReadBitflagInt32(ref ecuBitFlags);
            int? entrySize = reader.ReadBitflagInt32(ref ecuBitFlags); // 10
            int? blockSize = reader.ReadBitflagInt32(ref ecuBitFlags);

            if(blockOffset != null && entryCount != null && entrySize != null && blockSize != null)
            {
                return new MemoryPool((int)blockSize, (int)entryCount, (int)entrySize, (int)blockSize);
            }
            return null;
        }

        public byte[] ReadPool(BinaryReader reader)
        {
            reader.BaseStream.Seek(blockOffset, SeekOrigin.Begin);
            return reader.ReadBytes(entryCount * entrySize);
        }
    }
}
