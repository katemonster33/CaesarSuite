using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Caesar
{
    public class Choice
    {
        public int Value { get; set; }
        public CaesarStringReference Text { get; set; }

        public Choice()
        {
            Value = -1;
            Text = new CaesarStringReference()
            {
                Index = 0,
                Text = string.Empty
            };
        }

        public Choice(int value, CaesarStringReference text)
        {
            Value = value;
            Text = text;
        }
    }
}
