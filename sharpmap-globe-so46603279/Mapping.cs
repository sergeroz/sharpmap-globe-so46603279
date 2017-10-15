using GeoAPI.Geometries;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;
using SharpMap;
using SharpMap.Converters.WellKnownText;
using SharpMap.Data.Providers;
using SharpMap.Layers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sharpmap_globe_so46603279
{
    internal class Mapping
    {
        private Dictionary<string, Feature> iso2Countries;
        private Dictionary<string, Feature> iso3Countries;
        private ArrayList countryFeatures;
        System.Data.DataTable countriesDt;
        public string CountryWKTFromISO2(string iso2)
        {
            if (iso2Countries == null) return null;

            Feature feature;
            if (iso2Countries.TryGetValue(iso2, out feature))
            {
                return feature.Geometry.ToString();
            }
            return null;
        }
        public string CountryWKTFromISO3(string iso3)
        {
            if (iso3Countries == null) return null;

            Feature feature;
            if (iso3Countries.TryGetValue(iso3, out feature))
            {
                return feature.Geometry.ToString();
            }
            return null;
        }

        public string[] ISO2CountryList
        {
            get { return iso2Countries != null ? iso2Countries.Keys.ToArray() : null; }
        }
        public string[] ISO3CountryList
        {
            get { return iso3Countries != null ? iso3Countries.Keys.ToArray() : null; }
        }

        public System.Data.DataTable Countries { get { return countriesDt; } }

        private readonly string[] countryDtKeyCandidates = new string[] { "ISO2", "ISO3" };
        public void LoadCountries(string path)
        {
            iso2Countries = new Dictionary<string, Feature>();
            iso3Countries = new Dictionary<string, Feature>();
            countryFeatures = new ArrayList();

            countriesDt = new System.Data.DataTable();

            GeometryFactory factory = new GeometryFactory();
            using (ShapefileDataReader reader = new ShapefileDataReader(path, factory))
            {
                foreach (var f in reader.DbaseHeader.Fields)
                {
                    countriesDt.Columns.Add(f.Name, f.Type);
                }
                var keyCol = countryDtKeyCandidates
                    .Intersect(countriesDt.Columns.OfType<System.Data.DataColumn>().Select(col => col.ColumnName))
                    .FirstOrDefault();
                if (keyCol != null)
                {
                    countriesDt.PrimaryKey = new System.Data.DataColumn[] { countriesDt.Columns[keyCol] };
                }
                countriesDt.Columns.Add("WKT", typeof(string));

                while (reader.Read())
                {
                    Feature feature = new Feature();
                    AttributesTable attributesTable = new AttributesTable();

                    string[] keys = new string[reader.DbaseHeader.NumFields];
                    IGeometry geometry = (Geometry)reader.Geometry;

                    System.Data.DataRow row = countriesDt.NewRow();
                    for (int i = 0; i < reader.DbaseHeader.NumFields; i++)
                    {
                        DbaseFieldDescriptor fldDescriptor = reader.DbaseHeader.Fields[i];
                        keys[i] = fldDescriptor.Name;
                        attributesTable.AddAttribute(fldDescriptor.Name, reader.GetValue(i));

                        row[i] = reader.GetValue(i);
                    }
                    row["WKT"] = geometry.AsText();
                    countriesDt.Rows.Add(row);
                    feature.Geometry = geometry;
                    feature.Attributes = attributesTable;

                    if (feature.Attributes.Exists("ISO2") && feature.Attributes.Exists("ISO3"))
                    {
                        int index = countryFeatures.Add(feature);
                        var iso2 = (string)feature.Attributes["ISO2"];
                        var iso3 = (string)feature.Attributes["ISO3"];
                        iso2Countries.Add(iso2, feature);
                        iso3Countries.Add(iso3, feature);
                    }

                }
                reader.Close();
            }
        }

        public class GetImageCommand
        {
            public int Width { get; set; }
            public int Height { get; set; }

            public string BorderColor { get; set; }

            public float BorderWidth { get; set; }

            public string CountryLevelWkt { get; set; }

            public GetImageCommand()
            {
            }

        }

        public void DrawLayers(Map map, Dictionary<string,string> layers, string color)
        {

            foreach (var key in layers.Keys)
            {
                var iso3 = key;
                var wkt = layers[key];

                Mapping.GetImageCommand cmd = new Mapping.GetImageCommand()
                {
                    Width = 1280,
                    Height = 1280,
                    CountryLevelWkt = wkt,
                    BorderColor = color,
                    BorderWidth = 2
                };

                DrawCountry(cmd, map, iso3);
            }
        }

        public Map DrawCountry(GetImageCommand command, Map map, string layerName)
        {
            var countryGeometry = GeometryFromWKT.Parse(command.CountryLevelWkt);
            IProvider countryProvider = new GeometryFeatureProvider(countryGeometry);
            
            VectorLayer countryLayer = new VectorLayer(layerName, countryProvider);
            var borderColor = System.Drawing.ColorTranslator.FromHtml(command.BorderColor);
            countryLayer.Style.EnableOutline = true;
            countryLayer.Style.Outline = new Pen(borderColor);
            countryLayer.Style.Outline.Width = command.BorderWidth;
            countryLayer.Style.Fill = Brushes.Transparent;

            var transformationFactory = new CoordinateTransformationFactory();
            countryLayer.CoordinateTransformation = transformationFactory.CreateFromCoordinateSystems(
                        GeographicCoordinateSystem.WGS84,
                        ProjectedCoordinateSystem.WebMercator);

            map.Layers.Add(countryLayer);

            map.ZoomToExtents();
            return map;
        }
    }
}