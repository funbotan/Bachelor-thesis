using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PSI_GIS
{
    struct Vertex // потому что в .net нет точек с координатами в double
    {
        public double X;
        public double Y;

        public Vertex(double x, double y)
        {
            X = x;
            Y = y;
        }

        public static bool operator ==(Vertex a, Vertex b)
        {
            return a % b < Constants.EPS;
        }

        public static bool operator !=(Vertex a, Vertex b)
        {
            return !(a==b);
        }

        public static double operator %(Vertex a, Vertex b) // Расстояние между точками
        {
            return Math.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y));
        }
    }
}
