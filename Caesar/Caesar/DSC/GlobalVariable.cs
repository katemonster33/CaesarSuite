using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Caesar.DSC
{
    public class GlobalVariable : CaesarObject
    {
        public string Name;
        public BasicType BasicType;
        public DerivedType DerivedType;
        public int ArraySize;
        public int GlobalBytePosition;
        public byte[] DataBuffer = new byte[0];
        public GlobalVariable()
        {
            Name = string.Empty;
            BasicType = BasicType.Undefined;
            DerivedType = DerivedType.Undefined;
            ArraySize = 0;
            GlobalBytePosition = 0;
        }

        protected override bool ReadHeader(CaesarReader reader)
        {
            var parentDsc = GetParentByType<DSCContext>();
            Name = reader.ReadStringByAddress(parentDsc != null ? parentDsc.AbsoluteAddress : 0);
            BasicType = (BasicType)reader.ReadInt16();
            DerivedType = (DerivedType)reader.ReadInt16();
            ArraySize = reader.ReadInt16();
            GlobalBytePosition = reader.ReadInt16();
            return true;
        }

        protected override void ReadData(CaesarReader reader, CaesarContainer container)
        {
            throw new NotImplementedException();
        }

        int GetBasicByteSize()
        {
            switch (BasicType)
            {
                case BasicType.Undefined:
                default:
                    throw new Exception("Unrecognized DSC Type: basic type is out of bounds");
                case BasicType.Unk_1Byte:
                case BasicType.Char:
                    return 1;
                case BasicType.Unk_2Byte:
                case BasicType.Word:
                    return 2;
                case BasicType.Unk_4Byte:
                case BasicType.Unk_4Byte_2:
                case BasicType.DWord:
                    return 4;
            }
        }

        public int GetByteSize()
        {
            // MISizeofVarDataType
            // char, word, dword, ??, ??, ??, ??
            switch(DerivedType)
            {
                case DerivedType.Pointer:
                    return 4;
                default:
                    {
                        int output = GetBasicByteSize();
                        if(DerivedType == DerivedType.Array)
                        {
                            output *= ArraySize;
                        }
                        return output;
                    }
            }
        }

        public void InitializeData(byte[] globalBuffer)
        {
            DataBuffer = new byte[GetByteSize()];
            Buffer.BlockCopy(globalBuffer, GlobalBytePosition, DataBuffer, 0, DataBuffer.Length);
        }
    }
}
