using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Caesar
{
    public class CaesarStringReference
    {
        [JsonIgnore]
        public CaesarContainer? Container { get; set; }
        public int Index { get; set; }

        string? text = null;
        public string? Text 
        { 
            get
            {
                if (text == null && Container != null)
                {
                    text = Container.Language.GetString(Index);
                }
                return text;
            }
            set
            {
                text = value;
            }
        }

        public CaesarStringReference()
        {
            Index = -1;
        }

        public CaesarStringReference(CaesarContainer container, int index)
        {
            Container = container;
            Index = index;
        }
    }
}
