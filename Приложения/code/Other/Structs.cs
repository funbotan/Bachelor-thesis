using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PSI_GIS
{
    struct Jump
    {
        public int fromPoly;
        public int toPoly;
        public int returnFrom;
        public int returnTo;
    }

    struct Connection
    {
        public int v0;
        public int v1;
        public double len;
    }
}
