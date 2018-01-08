﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace DwarfCorp.Rail
{
    public class RailCombination
    {
        public String Overlay;
        public Orientation OverlayRelativeOrientation;

        public String Result;
        public Orientation ResultRelativeOrientation;
    }

    public class RailPiece
    {
        public String Name = "";
        public Point Tile = Point.Zero;
        public List<RailCombination> CombinationTable = new List<RailCombination>();

    }
}
