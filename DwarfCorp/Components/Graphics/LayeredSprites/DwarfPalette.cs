using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
using System.Runtime.Serialization;

namespace DwarfCorp.DwarfSprites
{
    public class Palette
    {
        [JsonIgnore]
        public DwarfCorp.Palette CachedPalette = null;

        public String Name;
        public String Asset;
        public int Row;
        public String Type;
    }
}
