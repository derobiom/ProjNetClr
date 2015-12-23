using System;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using Microsoft.SqlServer.Server;
using Microsoft.SqlServer.Types;
using ProjNet.Converters;
using ProjNet.CoordinateSystems;
using ProjNet.Converters.WellKnownText;
using ProjNet.CoordinateSystems.Transformations;
using ProjNet;

public partial class UserDefinedFunctions
{

 
    [Microsoft.SqlServer.Server.SqlFunction]
    public static SqlGeometry TransformCoordinates (SqlDouble X,
        SqlDouble Y, SqlString FromSystem, SqlString ToSystem)
    {
        
        
    string wkt_utm = "PROJCS[\"NAD83 / UTM zone 17N\"," +
   "GEOGCS[\"NAD83\"," +
      " DATUM[\"North_American_Datum_1983\"," +
          " SPHEROID[\"GRS 1980\",6378137,298.257222101," +
             "  AUTHORITY[\"EPSG\",\"7019\"]]," +
          " AUTHORITY[\"EPSG\",\"6269\"]]," +
     "  PRIMEM[\"Greenwich\",0," +
      "     AUTHORITY[\"EPSG\",\"8901\"]]," +
      " UNIT[\"degree\",0.01745329251994328," +
      "     AUTHORITY[\"EPSG\",\"9122\"]]," +
      " AUTHORITY[\"EPSG\",\"4269\"]]," +
 "  UNIT[\"metre\",1," +
  "     AUTHORITY[\"EPSG\",\"9001\"]]," +
  " PROJECTION[\"Transverse_Mercator\"]," +
  " PARAMETER[\"latitude_of_origin\",0]," +
  " PARAMETER[\"central_meridian\",-81]," +
  " PARAMETER[\"scale_factor\",0.9996]," +
  " PARAMETER[\"false_easting\",500000]," +
  " PARAMETER[\"false_northing\",0]," +
  " AUTHORITY[\"EPSG\",\"26917\"]," +
  " AXIS[\"Easting\",EAST]," +
  " AXIS[\"Northing\",NORTH]]";

        string wkt_PAStatePlane = "PROJCS[\"NAD83 / Pennsylvania North (ftUS)\"," +
      "GEOGCS[\"NAD83\"," +
         "DATUM[\"North_American_Datum_1983\"," +
              "SPHEROID[\"GRS 1980\",6378137,298.257222101," +
                 "AUTHORITY[\"EPSG\",\"7019\"]]," +
                 "AUTHORITY[\"EPSG\",\"6269\"]]," +
         "PRIMEM[\"Greenwich\",0.0," +
             "AUTHORITY[\"EPSG\",\"8901\"]]," +
         "UNIT[\"degree\",0.01745329251994328," +
            //"AXIS[\"Geodetic latitude\",NORTH]," +
            //"AXIS[\"Geodetic longitude\",EAST]," +
            // "AUTHORITY[\"EPSG\",\"9122\"]]," +
         "AUTHORITY[\"EPSG\",\"4269\"]]," +
            //"UNIT[\"US survey foot\",0.3048006096012192," +
          "AUTHORITY[\"EPSG\",\"9003\"]]," +
//     "PROJECTION[\"Lambert_Conic_Conformal_(2SP)\"]," +
     "PROJECTION[\"Lambert_Conformal_Conic\"]," +
     "PARAMETER[\"standard_parallel_1\",41.95]," +
     "PARAMETER[\"standard_parallel_2\",40.88333333333333]," +
     "PARAMETER[\"latitude_of_origin\",40.16666666666666]," +
     "PARAMETER[\"central_meridian\",-77.75]," +
     "PARAMETER[\"false_easting\",1968500]," +
     "PARAMETER[\"false_northing\",0]," +
            //"PARAMETER[\"standard_parallel_2\",51.16666666666667]," +
     "UNIT[\"US survey foot\",0.3048006096012192]," +
            //  "AXIS[\"X\",\"EAST\"]," +
            //  "AXIS[\"Y\",\"NORTH\"]," +
            //"AUTHORITY[\"EPSG\",\"9003\"]]," +
     "AUTHORITY[\"EPSG\",\"2271\"]]" +
     "AXIS[\"X\",\"EAST\"]," +
     "AXIS[\"Y\",\"NORTH\"]]";

        CoordinateTransformationFactory ctfac = new CoordinateTransformationFactory();
        ICoordinateSystem wgs84geo = CoordinateSystemWktReader.Parse(wkt_PAStatePlane) as ICoordinateSystem;
        IProjectedCoordinateSystem pcs_UTM17N = ProjectedCoordinateSystem.WGS84_UTM(17, true);
        ICoordinateTransformation trans;
        int targetSRID;
        SqlGeometry geom = new SqlGeometry();

        if (FromSystem == "StatePlane" && ToSystem == "UTM")
        {
            trans = ctfac.CreateFromCoordinateSystems(wgs84geo, pcs_UTM17N);
            targetSRID = 26917;
        }
        else if (FromSystem == "UTM" && ToSystem == "StatePlane")
        {
            trans = ctfac.CreateFromCoordinateSystems(pcs_UTM17N, wgs84geo);
            targetSRID = 2271;
        }
        else
        {
            geom = SqlGeometry.Point(666, 666, 0);
            return geom; 
        }
        
        try {
            double[] toPoint = trans.MathTransform.Transform(new double[] { (Double)X, (Double)Y });
            geom = SqlGeometry.Point(toPoint[0], toPoint[1], targetSRID);
        }
        catch 
        {
            geom = SqlGeometry.Point(666, 666, 0);
        }
        

        //SqlString s = "xd: " + xd.ToString() + "  yd: " + yd.ToString() + "  geomX: " + geom.STX.ToString() + "  geomY: " + geom.STY.ToString();

        //return s;
        return geom;

    }
}
