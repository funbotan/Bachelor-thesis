using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PSI_GIS
{
    class LineD
    {
        public Vertex a;
        public Vertex b;

        public LineD(Vertex A, Vertex B)
        {
            a = A;
            b = B;
        }

        public LineD(double x0, double y0, double x1, double y1)
        {
            a = new Vertex(x0, y0);
            b = new Vertex(x1, y1);
        }

        public double length()
        {
            return a % b;
        }

        public bool includes(Vertex v) // проверяет, лежит ли точка v на этом отрезке
        {
            if (b.X <= Math.Max(a.X, v.X) && b.X >= Math.Min(a.X, v.X) &&
                b.Y <= Math.Max(a.Y, v.Y) && b.Y >= Math.Min(a.Y, v.Y))
                return true;
            return false;
        }

        public int orientation(Vertex v) // проверяет, с какой стороны от отрезка лежит точка v
        {
            double val = (b.Y - a.Y) * (v.X - b.X) - (b.X - a.X) * (v.Y - b.Y);
            if (Math.Abs(val) < Constants.EPS) return 0;
            return (val > 0) ? 1 : 2;
        }

        public bool intersects(LineD l) // проверяет, пересекаются ли два отрезка, концы не считаются
        {
            int o1 = orientation(l.a);
            int o2 = orientation(l.b);
            int o3 = l.orientation(a);
            int o4 = l.orientation(b);
            //if (o1 == 0 && includes(l.a)) return true;
            //if (o2 == 0 && includes(l.b)) return true;
            //if (o3 == 0 && l.includes(a)) return true;
            //if (o4 == 0 && l.includes(b)) return true;
            if (o1 == 0 || o2 == 0 || o3 == 0 || o4 == 0) return false;
            if (o1 != o2 && o3 != o4) return true;
            return false;
        }

        public bool lineXpoly(Poly poly) // то же для произвольного полигона
        {
            if (poly.getSides().Any(side => intersects(side)))
                return true;
            else return false;
        }
    }
}
