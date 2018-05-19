using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Globalization;
using System.Windows.Forms;

namespace PSI_GIS
{
    static class Constants
    {
        public const double EPS = double.Epsilon;
        public const int MAXV = 8000; // максимальное количество вершин полигона
        public static XNamespace gml = "http://www.opengis.net/gml";
        public static XNamespace fme = "http://www.safe.com/gml/fme";
    }

    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new FileDialog());
        }

        public static void Work(string[] files)
        {
            foreach (string file in files)
            {
                // Загрузка карты
                Console.WriteLine(file);
                XElement root = XElement.Load(file);

                // Парсинг файла
                FeatureMember[] map = (from el in root.Elements(Constants.gml + "featureMember")
                                       select new FeatureMember(el.Descendants().First())).ToArray();

                // Обработка
                //try
                //{
                    for (int MS = 0; MS < map.Length; MS++)
                    {
                        List<Poly> newMap = new List<Poly>();
                        foreach (OuterPoly OP in map[MS].pols)
                        {
                            OP.unite();
                            newMap.AddRange(Poly.reduce(OP));
                        }
                        map[MS].pols = new List<Poly>(newMap);
                    }
                    // Проверка
                    for (int MS = 0; MS < map.Length; MS++)
                    {
                        foreach (Poly poly in map[MS].pols)
                        {
                            LineD[] sides = poly.getSides();
                            for (int i = 0; i < sides.Length; i++)
                            {
                                for (int j = 0; j < sides.Length; j++)
                                {
                                    if (Math.Abs(i - j) > 1 &&
                                        sides[i].a != sides[j].a &&
                                        sides[i].b != sides[j].b &&
                                        sides[i].a != sides[j].b &&
                                        sides[i].b != sides[j].a &&
                                        sides[i].intersects(sides[j]))
                                        throw new Exception("Intersection detected near " + sides[i].a.X.ToString() + " " + sides[i].a.Y.ToString() + " FID=" + MS.ToString());
                                }
                            }
                        }
                    }
                    Console.WriteLine("Calculation successful. Saving result...");
                //}
                //catch (Exception e)
                //{
                //    Console.WriteLine("An error occured: " + e.Message);
                //}

                // Вывод в XML
                root.Descendants(Constants.gml + "featureMember").Remove();
                foreach (FeatureMember MS in map)
                    root.Add(new XElement(Constants.gml + "featureMember", MS.getXML()));
                root.Save(file);
            }
        }
    }
}