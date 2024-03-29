﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using GMap.NET;
using GMap.NET.WindowsForms;
using GMap.NET.WindowsForms.Markers;
using GMap.NET.MapProviders;



namespace GPSTest3
{

    public partial class Form1 : Form
    {
        //Initialise variables used in the form
        int RangeInt;
        double LatDbl = 37.8651;
        double LongDbl = -119.5383;
        string LatStr;
        string LongStr;
        string RangeStr;
        int ZoomInt;
        double GPSLatMin = 180;
        double GPSLatMax = -180;
        double GPSLngMin = 180;
        double GPSLngMax = -180;
        double GPSLatDroneCount;
        double GPSLngDroneCount;
        double GPSLngDroneCountDn;
        double GPSLatDroneCountDn;
        double GPSLatRes;
        double GPSLngRes;
        double GPSinM = 111.32;
        double DroneRange;
        bool ready = false;
        double count;
        double LineLength;

        //Map Waypoints and drawing grid area overlay names
        GMapOverlay markers = new GMapOverlay("markers");
        GMapOverlay polygons = new GMapOverlay("polygons");

        //Lists used throughout the code
        List<PointLatLng> DrawnCoordinates = new List<PointLatLng>();
        List<PointLatLng> AllGridPoints = new List<PointLatLng>();
        List<PointLatLng> GridPointsInsideOperatingArea = new List<PointLatLng>();

        //Defined points used for user defined functions
        PointLatLng CheckPoint0;
        PointLatLng CheckPoint1;
        PointLatLng CheckPoint2;


        public Form1()
        {
            //Initialise the form
            InitializeComponent();

            //Configure and initialise the GMap module.
            gmap.MapProvider = GMap.NET.MapProviders.GoogleMapProvider.Instance;
            GMap.NET.GMaps.Instance.Mode = GMap.NET.AccessMode.ServerOnly;
            gmap.Position = new GMap.NET.PointLatLng(37.8651, -119.5383);
            gmap.Zoom = 14;
            gmap.ShowCenter = false;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            //Setup the map to go to the desired position based on text box entries
            LatStr = LatText.Text;
            LongStr = LongText.Text;
            LatDbl = Convert.ToDouble(LatStr);
            LongDbl = Convert.ToDouble(LongStr);
            RangeStr = RangeText.Text;
            RangeInt = Convert.ToInt32(RangeStr);

            //Set the zoom to a range suitable for the operating distance
            if (RangeInt >= 10000)
                ZoomInt = 8;
            if (RangeInt <= 10000)
                ZoomInt = 10;
            if (RangeInt <= 5000)
                ZoomInt = 12;
            if (RangeInt <= 2500)
                ZoomInt = 12;
            if (RangeInt <= 1000)
                ZoomInt = 14;
            if (RangeInt <= 500)
                ZoomInt = 16;
            if (RangeInt <= 250)
                ZoomInt = 17;
            if (RangeInt <= 100)
                ZoomInt = 18;

            //Configure the map module
            gmap.MapProvider = GMap.NET.MapProviders.GoogleMapProvider.Instance;
            GMap.NET.GMaps.Instance.Mode = GMap.NET.AccessMode.ServerOnly;
            gmap.Position = new GMap.NET.PointLatLng(LatDbl, LongDbl);
            gmap.Zoom = ZoomInt;
            gmap.ShowCenter = false;
        }

        private void button3_Click(object sender, EventArgs e)
        {
            //Clear all button press
            //Reset all values back to default
            markers.Markers.Clear();
            GPSOutputText.Text = "";
            DrawnCoordinates.Clear();
            polygons.Polygons.Clear();
            GPSLatMin = 180;
            GPSLatMax = -180;
            GPSLngMin = 180;
            GPSLngMax = -180;
            AllGridPoints.Clear();
            GridPointsInsideOperatingArea.Clear();
        }

        private void gmap_MouseClick(object sender, MouseEventArgs e)
        {
            //Log a coordinate point when clicking on the map
            if (e.Button == System.Windows.Forms.MouseButtons.Left)
            {
                double lat = gmap.FromLocalToLatLng(e.X, e.Y).Lat;
                double lng = gmap.FromLocalToLatLng(e.X, e.Y).Lng;

                //Place a marker on the map when clicked
                AddMarker(lat, lng);

            }
        }


        private void ZoneButton_Click(object sender, EventArgs e)
        {
            DroneRange = (Convert.ToDouble(textBox1.Text)) / 1000;
            //Draw polygons around the points generated
            GMapPolygon polygon = new GMapPolygon(DrawnCoordinates, "Operating Area");
            polygons.Polygons.Add(polygon);

            //Clear all the markers drawn as a shape has now been drawn around the area
            markers.Markers.Clear();

            //Generate the min and max lat and lng points of all the points listed and store in the appropriate variable
            foreach (PointLatLng point in DrawnCoordinates)
            {
                if (GPSLatMax <= point.Lat)
                {
                    GPSLatMax = point.Lat;
                }
                if (GPSLatMin >= point.Lat)
                {
                    GPSLatMin = point.Lat;
                }
                if (GPSLngMax <= point.Lng)
                {
                    GPSLngMax = point.Lng;
                }
                if (GPSLngMin >= point.Lng)
                {
                    GPSLngMin = point.Lng;
                }

            }

            //Draw a box around the operating area to determine where the grid of possible drone points should be
            //calculated based on the min and max lat and long values to generate a square around the operating area.
            List<PointLatLng> points = new List<PointLatLng>();
            points.Add(new PointLatLng(GPSLatMax, GPSLngMax));
            points.Add(new PointLatLng(GPSLatMax, GPSLngMin));
            points.Add(new PointLatLng(GPSLatMin, GPSLngMin));
            points.Add(new PointLatLng(GPSLatMin, GPSLngMax));

            //Draw the operating area polygon vector
            gmap.Overlays.Add(polygons);

            //Calculate how many drones across the latitude based on the range of the drone radio relay and the distance of the operating area
            //Each degree of latitude is 113.53km so the total lat/long value is multiplied by this to give a distance in km and then divided by the range of each drone to give
            //total number of drones required in both lat and long to generate the grid of coordinates.
            GPSLatDroneCount = ((GPSLatMax - GPSLatMin) * GPSinM) / DroneRange;
            GPSLngDroneCount = ((GPSLngMax - GPSLngMin) * GPSinM) / DroneRange;

            //Round up to the nearest whole number to allow for the grid to be generated, you can't have a fraction of a drone in the real world!
            GPSLatDroneCountDn = Math.Ceiling(GPSLatDroneCount);
            GPSLngDroneCountDn = Math.Ceiling(GPSLngDroneCount);

            //Calculate the resolution for the drone grid to calculate the spacing of the GPS beacons.
            GPSLatRes = (GPSLatMax - GPSLatMin) / GPSLngDroneCount;
            GPSLngRes = (GPSLngMax - GPSLngMin) / GPSLatDroneCount;


            //For each of the points in the lat direction generate a waypoint
            for (int countlat = 0; countlat <= GPSLngDroneCountDn; countlat++)
            {
                //for each of the points in the long direction generate a waypoint
                //This nested loop will generate a grid of x*y waypoints, where x is the number of drones in the lat direction, 
                //and y is the number of drones required in the long direction
                for (int countlng = 0; countlng <= GPSLatDroneCountDn; countlng++)
                {
                    //Generate waypoints for all of the points
                    AllGridPoints.Add(new PointLatLng((GPSLatMin + (0.0001) + (GPSLatRes * countlat)), (GPSLngMin + (GPSLngRes * countlng))));
                }


            }

            //Used to ensure that functions are not called unless all 3 checkpoints are populated.
            ready = false;

            //For all the points drawn to determine the operating zone generate triangles based on 3 points defined by checkpoint0, 1, and 2.
            foreach (PointLatLng ncoord in DrawnCoordinates)
            {
                
                if (count == 0)
                {
                    //Store GPS coordinate point 1
                    CheckPoint0 = ncoord;
                    count++;
                }
                else if (count == 1)
                {
                    //Store GPS coordinate point 2
                    CheckPoint1 = ncoord;
                    count++;

                }
                else if (count == 2)
                {
                    //Store GPS coordinate point 3
                    CheckPoint2 = ncoord;

                    //Point 0 stays static to ensure even coverage of the operating area
                    count = 1;
                    //Allow the code to start performing calculations
                    ready = true;
                }
                //Only continue if all 3 points are populated
                    if (ready == true)
                {
                    //for all points generated in a square over the operating area check if they lie within the operating area
                    foreach (PointLatLng pointsin in AllGridPoints)
                    {
                        //If the point is within the area add it to a new list of points that are within the operating area.
                        if (PointChecker(CheckPoint0, CheckPoint1, CheckPoint2, pointsin) == true)
                        {
                            GridPointsInsideOperatingArea.Add(pointsin);
                        }

                    }
                }


                
                

            }

            //For all points inside the operating area draw a marker and output the GPS coordinate to the textbox.
            foreach (PointLatLng ncoordinate in GridPointsInsideOperatingArea)
            {
                //Output all the GPS coordinates to the text box
                GPSOutputText.Text += Environment.NewLine + ncoordinate;
                AddMarker(ncoordinate.Lat, ncoordinate.Lng);
            }

            //Update the GMap drawing window to show the new markers.
            gmap.Position = new GMap.NET.PointLatLng(LatDbl, LongDbl);
        }

        public void AddMarker(double lat1, double lng1)
        {
            //function used to add a marker to the map with coordinates of (lat1, lng1)
            DrawnCoordinates.Add(new PointLatLng(lat1, lng1));
            GMapMarker marker = new GMarkerGoogle(
                new PointLatLng(lat1, lng1),
                GMarkerGoogleType.red_pushpin);
            markers.Markers.Add(marker);
            gmap.Overlays.Add(markers);
            gmap.Position = new GMap.NET.PointLatLng(LatDbl, LongDbl);
        }

        public double LineCalc(PointLatLng point0, PointLatLng point1)
        {
            //Function used to calculate the distance between any 2 GPS coordinates that have been passed into this function
            LineLength = (Math.Sqrt((Math.Pow((point0.Lat - point1.Lat), 2)) + (Math.Pow((point0.Lng - point1.Lng), 2))));

            return LineLength;
        }

        public double AngleCalc(PointLatLng pointA, PointLatLng pointB, PointLatLng pointX)
        {
            //Function used to calculate the angle at pointX when passed any 3 points. 
            //Returns the angle in radians.
            double Linea;
            double Lineb;
            double Linex;
            double Anglex;

            Linex = LineCalc(pointA, pointB);
            Linea = LineCalc(pointB, pointX);
            Lineb = LineCalc(pointX, pointA);

            //Using cosine rule of angle X = (sin^-1)a^2+b^2-x^2 / 2ab
            Anglex = Math.Acos(((Linea*Linea)+(Lineb*Lineb)-(Linex*Linex))/(2*Linea*Lineb));

            return Anglex;
        }

        public bool PointChecker(PointLatLng pointA, PointLatLng pointB, PointLatLng pointC, PointLatLng pointCheck)
        {
            //Function used to check if a point is within a defined area
            //Breaks the user defined operating area into triangles and checks a grid of points to decide
            //which points are within the shape, and which are outside. If a point is inside the shape it will add
            //to the internal list which is used to generate the final list of gps coordinates drones should fly to.
            double Angle0;
            double Angle1;
            double Angle2;
            double TotalAngle;
            bool result;

            Angle0 = AngleCalc(pointA, pointB, pointCheck);
            Angle1 = AngleCalc(pointB, pointC, pointCheck);
            Angle2 = AngleCalc(pointA, pointC, pointCheck);

            TotalAngle = Angle0 + Angle1 + Angle2;

            //If the total angle between the point and 3 of the user defined points is ~=360degrees, this means the point is inside the shape, otherwise it is outside.
            if ((TotalAngle <= (2*Math.PI + 0.1)) && (TotalAngle >= (2*Math.PI - 0.1)))
            {
                result = true;
            }
            else result = false;


            return result;
        }
    }

}
