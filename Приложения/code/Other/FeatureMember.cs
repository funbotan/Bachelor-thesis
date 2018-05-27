using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Globalization;

namespace PSI_GIS
{
    class FeatureMember
    {
        public List<Poly> pols;
        XElement context;
        string srsName;
        string srsDimension;

        public FeatureMember(XElement node)
        {
            pols = new List<Poly>();
            context = new XElement(node);
            List<XElement> patches = context.Descendants(Constants.gml + "PolygonPatch").ToList<XElement>();
            XElement meta;
            if (context.Descendants(Constants.gml + "MultiSurface").Any())
                meta = context.Descendants(Constants.gml + "MultiSurface").First();
            else
                meta = context.Descendants(Constants.gml + "Surface").First();
            srsName = meta.Attribute("srsName").Value;
            srsDimension = meta.Attribute("srsDimension").Value;
            foreach (XElement patch in patches)
            {
                OuterPoly poly = new OuterPoly(patch.Element(Constants.gml + "exterior").Descendants(Constants.gml + "posList").First().Value);
                poly.IP = (from el in patch.Elements(Constants.gml + "interior") select new InnerPoly(el.Descendants(Constants.gml + "posList").First().Value)).ToArray();
                pols.Add(poly);
            }
            context.Descendants(Constants.gml + "multiSurfaceProperty").Remove();
            context.Descendants(Constants.gml + "surfaceProperty").Remove();
        }

        // Вывод
        public XElement getXML()
        {
            XElement ret = new XElement(context);
            if (pols.Count > 1)
            {
                ret.Add(new XElement(Constants.gml + "multiSurfaceProperty",
                       new XElement(Constants.gml + "MultiSurface",
                       new XAttribute("srsName", srsName),
                       new XAttribute("srsDimension", srsDimension))));
                XElement MultiSurface = ret.Descendants(Constants.gml + "MultiSurface").First();
                foreach (Poly poly in pols)
                {
                    MultiSurface.Add(new XElement(Constants.gml + "surfaceMember",
                                     new XElement(Constants.gml + "Surface",
                                     poly.toXML())));
                }
            }
            else
            {
                ret.Add(new XElement(Constants.gml + "surfaceProperty",
                        new XElement(Constants.gml + "Surface",
                        new XAttribute("srsName", srsName),
                        new XAttribute("srsDimension", srsDimension),
                        pols.First().toXML())));
            }
            return ret;
        }
    }
}
