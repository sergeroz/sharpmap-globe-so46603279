using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace sharpmap_globe_so46603279
{
    public partial class Form1 : Form
    {

        private Mapping mapping;
        private Point pbMouseDownLocation;
        private SharpMap.Map renderedMap;
        private double[] mapExtents = new double[2];
        private double[] l2Offset = new double[2];

        public Form1()
        {
            InitializeComponent();

            mapping = new Mapping();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            openFileDialog1.InitialDirectory = Program.AssemblyDirectory;
            openFileDialog1.FileOk += (sender2, e2) =>
            {
                textBox1.Text = openFileDialog1.FileName;
                button2.Enabled = (!String.IsNullOrEmpty(textBox1.Text) && System.IO.File.Exists(textBox1.Text));
            };
            openFileDialog1.ShowDialog();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if(!String.IsNullOrEmpty(textBox1.Text) && System.IO.File.Exists(textBox1.Text))
            {
                mapping.LoadCountries(textBox1.Text);

                var countries = mapping.ISO3CountryList;

                comboBox1.Items.Clear();

                comboBox1.DataSource = null;
                comboBox1.DisplayMember = "NAME";
                comboBox1.ValueMember = "ISO3";
                comboBox1.DataSource = mapping.Countries.DefaultView;
                comboBox1.Refresh();

            }
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            var key = (string)((ComboBox)sender).SelectedValue;

            textBox2.Text = getCountryWkt(key);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            var selectedCountryISO3 = (string)comboBox1.SelectedValue;

            if (!String.IsNullOrEmpty(selectedCountryISO3))
            {
                renderedMap = renderMap(selectedCountryISO3);
                updatePicture(renderedMap);
            }
        }

        private void pictureBox1_MouseDown(object sender, MouseEventArgs e)
        {
            if(e.Button == MouseButtons.Left)
            {
                pbMouseDownLocation = e.Location;
            }
        }

        private void pictureBox1_MouseMove(object sender, MouseEventArgs e)
        {
            if(e.Button == MouseButtons.Left)
            {
                double x = e.X + pictureBox1.Left - pbMouseDownLocation.X;
                x = x / pictureBox1.Width;
                l2Offset[0] = l2Offset[0] + x * mapExtents[0];

                updatePicture(renderedMap);
            }
        }

        private string getCountryWkt(string ISO3)
        {
            var dv = mapping.Countries.AsDataView();
            dv.RowFilter = $"ISO3 = '{ISO3}'";

            if (dv.Count == 1)
            {
                var row = dv[0];
                var wkt = (string)row["WKT"];

                return wkt;
            }
            return null;
        }

        private SharpMap.Map renderMap(string selectedCountry)
        {
            var map = new SharpMap.Map(new Size(1280, 1280));
            map.BackColor = Color.Transparent;

            var dv = mapping.Countries.AsDataView();
            dv.RowFilter = $"ISO3 <> '{selectedCountry}'";

            Dictionary<string, string> iso3WktMap =
            dv.OfType<System.Data.DataRowView>().ToDictionary(r => (string)r["ISO3"], r => (string)r["WKT"]);

            mapping.DrawLayers(map, iso3WktMap, "#F00");

            var selectedCountryWkt = getCountryWkt(selectedCountry);

            iso3WktMap = new Dictionary<string, string>();
            iso3WktMap.Add("COUNTRY", selectedCountryWkt);
            mapping.DrawLayers(map, iso3WktMap, "#000");

            mapExtents[0] = map.GetExtents().MaxX;
            mapExtents[1] = map.GetExtents().MaxY;
            return map;
        }

        private void updatePicture(SharpMap.Map map)
        {
            var layer = map.Layers["COUNTRY"];

            Image centered = new Bitmap(1280, 1280);
            using (Graphics gr = Graphics.FromImage(centered))
            {
                var initialCenter = map.Center;

                map.Center = layer.Envelope.Centre;

                var img = map.GetMap();
                gr.DrawImage(img, new Point(0, 0));

                /*
                var transformationFactory = new ProjNet.CoordinateSystems.Transformations.CoordinateTransformationFactory();
                var trans = transformationFactory.CreateFromCoordinateSystems(
                            ProjNet.CoordinateSystems.GeographicCoordinateSystem.WGS84,
                            ProjNet.CoordinateSystems.ProjectedCoordinateSystem.WebMercator);

                var endPoint = trans.MathTransform.Transform(new double[] { 179.99999999, 89.99999999 });



                TODO: Insert code here to work out by how much map should be moved/adjusted

                */

                map.Center.X = initialCenter.X - l2Offset[0];
                map.Center.Y = initialCenter.Y - l2Offset[1];

                var img2 = map.GetMap();

                Rectangle rect = new Rectangle(0, 0, img2.Width, img2.Height);

                gr.DrawImage(img2, rect, 0, 0, rect.Width, rect.Height, GraphicsUnit.Pixel);

                txtCtryEnvCenterX.Text = initialCenter.X.ToString();
                txtCtryEnvCenterY.Text = initialCenter.Y.ToString();
                txtWorld2LCenterX.Text = (initialCenter.X - l2Offset[0]).ToString();
                txtWorld2LCenterY.Text = (initialCenter.Y - l2Offset[1]).ToString();
            }


            //var savePath = System.IO.Path.Combine(Program.AssemblyDirectory, $"Map_{DateTime.Now.ToString("yyyyMMdd_HHmmss")}.png");
            //img.Save(savePath, System.Drawing.Imaging.ImageFormat.Png);
            pictureBox1.Image = centered;
        }
    }
}
