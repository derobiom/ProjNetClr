using System;
using Microsoft.SqlServer.Types; // SqlGeometry and SqlGeography
using ProjNet.CoordinateSystems.Transformations; // Proj.NET

using System.Collections.Generic;
using System.Text;
using System.Data.SqlTypes;
using System.Data.SqlClient; // Required for context connection
using Microsoft.SqlServer.Server; // SqlFunction Decoration
using ProjNet.CoordinateSystems; // ProjNET coordinate systems
using ProjNet.CoordinateSystems.Transformations; // ProjNET transformation functions
using ProjNet.Converters.WellKnownText; //ProjNET WKT functions

namespace ClrTransformation
{
    class TransformGeographyToGeometrySink : IGeographySink110
    {
        private readonly ICoordinateTransformation _trans;
        private readonly IGeometrySink110 _sink;
        public TransformGeographyToGeometrySink(
        ICoordinateTransformation trans,
        IGeometrySink110 sink
        )
        {
            _trans = trans;
            _sink = sink;
        }
        public void BeginGeography(OpenGisGeographyType type)
        {
            // Begin creating a new geometry of the type requested
            _sink.BeginGeometry((OpenGisGeometryType)type);
        }
        public void BeginFigure(double latitude, double longitude, double? z, double? m)
        {
            // Use ProjNET Transform() method to project lat,lng coordinates to x,y
            double[] startPoint = _trans.MathTransform.Transform(new double[] { longitude, latitude });
            // Begin a new geometry figure at corresponding x,y coordinates
            _sink.BeginFigure(startPoint[0], startPoint[1], z, m);
        }
        public void AddLine(double latitude, double longitude, double? z, double? m)
        {
            // Use ProjNET to transform end point of the line segment being added
            double[] toPoint = _trans.MathTransform.Transform(new double[] { longitude, latitude });
            // Add this line to the geometry
            _sink.AddLine(toPoint[0], toPoint[1], z, m);
        }
        public void AddCircularArc(double latitude1, double longitude1, double? z1, double? m1,
        double latitude2, double longitude2, double? z2, double? m2
        )
        {
            // Transform both the anchor point and destination of the arc segment
            double[] anchorPoint = _trans.MathTransform.Transform(new double[] { longitude1, latitude1 });
            double[] toPoint = _trans.MathTransform.Transform(new double[] { longitude2, latitude2 });
            // Add this arc to the geometry
            _sink.AddCircularArc(anchorPoint[0], anchorPoint[1], z1, m1,
            toPoint[0], toPoint[1], z2, m2);
        }
        public void EndFigure()
        {
            _sink.EndFigure();
        }
        public void EndGeography()
        {
            _sink.EndGeometry();
        }
        public void SetSrid(int srid)
        {
            // Just pass through
        }
    }

    public partial class UserDefinedFunctions
    {
        [Microsoft.SqlServer.Server.SqlFunction(DataAccess = DataAccessKind.Read)]
        public static SqlGeometry GeographyToGeometry(SqlGeography geog, SqlInt32 toSRID)
        {
            // Use the context connection to the SQL Server instance on which this is executed
            using (SqlConnection conn = new SqlConnection("context connection=true"))
            {
                // Open the connection
                conn.Open();
                // Retrieve the parameters of the source spatial reference system
                SqlCommand cmd = new SqlCommand("SELECT well_known_text FROM prospatial_reference_systems WHERE spatial_reference_id = @srid", conn);
                cmd.Parameters.Add(new SqlParameter("srid", geog.STSrid));
                object fromResult = cmd.ExecuteScalar();
                // Check that details of the source SRID have been found
                if (fromResult is System.DBNull || fromResult == null)
                { return null; }
                // Retrieve the WKT
                String fromWKT = Convert.ToString(fromResult);
                // Create the source coordinate system from WKT
                ICoordinateSystem fromCS = CoordinateSystemWktReader.Parse(fromWKT) as
                ICoordinateSystem;
                // Retrieve the parameters of the destination spatial reference system
                cmd.Parameters["srid"].Value = toSRID;
                object toResult = cmd.ExecuteScalar();
                // Check that details of the destination SRID have been found
                if (toResult is System.DBNull || toResult == null)
                { return null; }
                // Execute the command and retrieve the WKT
                String toWKT = Convert.ToString(toResult);
                // Clean up
                cmd.Dispose();
                // Create the destination coordinate system from WKT
                ICoordinateSystem toCS = CoordinateSystemWktReader.Parse(toWKT) as
                ICoordinateSystem;
                // Create a CoordinateTransformationFactory instance
                CoordinateTransformationFactory ctfac = new CoordinateTransformationFactory();
                // Create the transformation between the specified coordinate systems
                ICoordinateTransformation trans = ctfac.CreateFromCoordinateSystems(fromCS, toCS);
                // Create a geometry instance to be populated by the sink
                SqlGeometryBuilder b = new SqlGeometryBuilder();
                // Set the SRID to match the destination SRID
                b.SetSrid((int)toSRID);
                // Create a sink for the transformation and plug it in to the builder
                TransformGeographyToGeometrySink s = new TransformGeographyToGeometrySink(trans, b);
                // Populate the sink with the supplied geography instance
                geog.Populate(s);
                // Return the transformed geometry instance
                return b.ConstructedGeometry;
            }
        }
    }
}


