using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Globalization;

namespace PSI_GIS
{
    class Poly
    {
        public Vertex[] vert;

        public Poly() { }

        public Poly(string str) // парсинг строки в массив точек
        {
            string[] tmp = str.Split(' ');
            vert = new Vertex[tmp.Length / 2];
            for (int i = 0; i < tmp.Length; i++)
            {
                if (i % 2 == 0) vert[i / 2].X = double.Parse(tmp[i], CultureInfo.InvariantCulture);
                else vert[i / 2].Y = double.Parse(tmp[i], CultureInfo.InvariantCulture);
            }
        }

        public string toString() // наоборот
        {
            string ret = "";
            for (int i = 0; i < vert.Length; i++)
            {
                ret += vert[i].X.ToString(CultureInfo.InvariantCulture);
                ret += " ";
                ret += vert[i].Y.ToString(CultureInfo.InvariantCulture);
                ret += " ";
            }
            return ret.Trim();
        }

        // следующая и предыдущая вершины в цикле
        public int next(int n)
        {
            return (n + 1) % vert.Length;
        }
        public int prev(int n)
        {
            if (n == 0) return vert.Length - 1;
            else return n - 1;
        }

        // Определяет порядок нумерации вершин. 1 = по часовой, 0 = против
        bool orientation()
        {
            double buff = 0;
            for (int i = 1; i < vert.Length; i++)
                buff += (vert[i].X - vert[i - 1].X) * (vert[i].Y + vert[i - 1].Y);
            if (buff < 0) return false;
            else return true;
        }

        // Создает массив линий, составляющих полигон
        public LineD[] getSides()
        {
            LineD[] sides = new LineD[vert.Length];
            for (int i = 0; i < vert.Length; i++)
                sides[i] = new LineD(vert[i], vert[next(i)]);
            return sides;
        }

        // Поиск ближайшей к заданной точке вершины полигона. Если понадобится, можно оптимизировать до O(log(N))
        public int closestVertex(Vertex point)
        {
            int closestVertex = 0;
            double newDist, bestDist = double.PositiveInfinity;
            LineD connection;
            var sides = getSides();
            for (int i = 0; i < vert.Length; i++)
            {
                newDist = vert[i] % point;
                connection = new LineD(point, vert[i]);
                if (newDist < bestDist && !sides.Any(s => connection.intersects(s)))
                {
                    bestDist = newDist;
                    closestVertex = i;
                }
            }
            return closestVertex;
        }

        // Проверяет, что линия, соединяющая две вершины полигона, не пересекает его стороны
        bool clearPath(int v0, int v1)
        {
            int clockwise, anticlockwise, t0, t1;
            if (orientation())
            {
                clockwise = 2;
                anticlockwise = 1;
            }
            else
            {
                clockwise = 1;
                anticlockwise = 2;
            }
            t0 = v0;
            t1 = v1;
            v0 = Math.Min(t0, t1);
            v1 = Math.Max(t0, t1);
            LineD line = new LineD(vert[v0], vert[v1]);
            var test = from v in Enumerable.Range(0, vert.Length) where (v > v0 && v < v1) select line.orientation(vert[v]);
            for (int v = 0; v < vert.Length; v++)
            {
                if (vert[v].X > Math.Min(vert[v0].X, vert[v1].X) &&
                    vert[v].Y > Math.Min(vert[v0].Y, vert[v1].Y) &&
                    vert[v].X < Math.Max(vert[v0].X, vert[v1].X) &&
                    vert[v].Y < Math.Max(vert[v0].Y, vert[v1].Y) &&
                    ((v < v0 && line.orientation(vert[v]) == clockwise) ||
                     (v > v0 && v < v1 && line.orientation(vert[v]) == anticlockwise) ||
                     (v > v1 && line.orientation(vert[v]) == clockwise)))
                    return false;
            }
            return true;
        }

        // Рекурсивное разбиение
        public static Poly[] reduce(Poly poly)
        {
            if (poly.vert.Length > Constants.MAXV)
            {
                Poly[] reduced = new Poly[2];
                int v0 = 0;
                int v1 = Constants.MAXV - 2;
                while (true)
                {
                    do
                    {
                        if (poly.clearPath(v0, v1))
                            goto found;
                        v0 = poly.next(v0);
                        v1 = poly.next(v1);
                    }
                    while (v0 != 0);
                    v1 = poly.prev(v1);
                }
                throw new Exception("Impossible to reduce a polygon");
            found:
                int t0 = v0;
                int t1 = v1;
                v0 = Math.Min(t0, t1);
                v1 = Math.Max(t0, t1);
                reduced[0] = new Poly();
                reduced[0].vert = poly.vert.Skip(v0).Take(v1 - v0 + 1).ToArray();
                reduced[1] = new Poly();
                reduced[1].vert = poly.vert.Take(v0 + 1).Concat(poly.vert.Skip(v1 - 1)).ToArray();
                return reduce(reduced[0]).Concat(reduce(reduced[1])).ToArray();
            }
            else return new[] { poly };
        }

        // Вывод до уровня patch
        public XElement toXML()
        {
            return new XElement(Constants.gml + "patches",
                   new XElement(Constants.gml + "PolygonPatch",
                   new XElement(Constants.gml + "exterior",
                   new XElement(Constants.gml + "LinearRing",
                   new XElement(Constants.gml + "posList",
                   toString())))));
        }

        // Проверка
        public bool consistent()
        {
            LineD[] sides = getSides();
            for (int i = 0; i < sides.Length; i++)
            {
                for (int j = 0; j < sides.Length; j++)
                {
                    if (i != j &&
                        Math.Abs(i - j) > 1 &&
                        sides[i].a != sides[j].a &&
                        sides[i].b != sides[j].b &&
                        sides[i].a != sides[j].b &&
                        sides[i].b != sides[j].a &&
                        sides[i].intersects(sides[j]))
                        return false;
                }
            }
            return true;
        }
    }   
}
