#if ADVANCESTEEL2023
using System;
using System.Collections.Generic;
using System.Linq;

using Speckle.Core.Models;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Acad = Autodesk.AutoCAD.Geometry;
using AcadDB = Autodesk.AutoCAD.DatabaseServices;

using Objects.BuiltElements.AdvanceSteel;
using Alignment = Objects.BuiltElements.Alignment;
using Arc = Objects.Geometry.Arc;
using Interval = Objects.Primitive.Interval;
using Polycurve = Objects.Geometry.Polycurve;
using Curve = Objects.Geometry.Curve;
using Featureline = Objects.BuiltElements.Featureline;
using Line = Objects.Geometry.Line;
using Point = Objects.Geometry.Point;
using Brep = Objects.Geometry.Brep;
using Mesh = Objects.Geometry.Mesh;
using Pipe = Objects.BuiltElements.Pipe;
using Plane = Objects.Geometry.Plane;
using Polyline = Objects.Geometry.Polyline;
using Profile = Objects.BuiltElements.Profile;
using Spiral = Objects.Geometry.Spiral;
using SpiralType = Objects.Geometry.SpiralType;
using Station = Objects.BuiltElements.Station;
using Structure = Objects.BuiltElements.Structure;
using Objects.Other;
using ASBeam = Autodesk.AdvanceSteel.Modelling.Beam;
using Autodesk.AdvanceSteel.CADAccess;
using Autodesk.AdvanceSteel.CADLink.Database;
using CADObjectId = Autodesk.AutoCAD.DatabaseServices.ObjectId;
using ASObjectId = Autodesk.AdvanceSteel.CADLink.Database.ObjectId;
using Autodesk.AdvanceSteel.DocumentManagement;
using Autodesk.AdvanceSteel.Geometry;
using ASPoint3d = Autodesk.AdvanceSteel.Geometry.Point3d;
using System.Security.Cryptography;
using System.Collections;
using Autodesk.AdvanceSteel.Modeler;
using StructuralUtilities.PolygonMesher;
using Objects.Geometry;
using Autodesk.AutoCAD.BoundaryRepresentation;
using MathNet.Spatial.Euclidean;
using MathPlane = MathNet.Spatial.Euclidean.Plane;
using TriangleNet.Geometry;
using TriangleVertex = TriangleNet.Geometry.Vertex;
using TriangleMesh = TriangleNet.Mesh;

namespace Objects.Converter.AutocadCivil
{
  public partial class ConverterAutocadCivil
  {
    public bool CanConvertASToSpeckle(DBObject @object)
    {
      switch (@object.ObjectId.ObjectClass.DxfName)
      {
        case DxfNames.BEAM:
          return true;
      }

      return false;
    }

    public Base ObjectASToSpeckle(DBObject @object, ApplicationObject reportObj, List<string> notes)
    {
      Base @base = null;

      switch (@object.ObjectId.ObjectClass.DxfName)
      {
        case DxfNames.BEAM:
          ASBeam beam = GetFilerObjectByEntity<ASBeam>(@object);
          reportObj.Update(descriptor: beam.GetType().ToString());
          return BeamToSpeckle(beam, notes);
      }

      return @base;
    }

    private AdvanceSteelBeam BeamToSpeckle(ASBeam beam, List<string> notes)
    {
      AdvanceSteelBeam advanceSteelBeam = new AdvanceSteelBeam();

      var startPoint = beam.GetPointAtStart();
      var endPoint = beam.GetPointAtEnd();
      var units = ModelUnits;
      var factor = Factor;

      Point speckleStartPoint = PointToSpeckle(startPoint, units);
      Point speckleEndPoint = PointToSpeckle(endPoint, units);
      advanceSteelBeam.baseLine = new Line(speckleStartPoint, speckleEndPoint, units);
      advanceSteelBeam.baseLine.length = speckleStartPoint.DistanceTo(speckleEndPoint);

      var modelerBody = beam.GetModeler(Autodesk.AdvanceSteel.Modeler.BodyContext.eBodyContext.kDetailed);

      advanceSteelBeam.area = beam.GetPaintArea();
      advanceSteelBeam.volume = modelerBody.Volume;

      advanceSteelBeam.displayValue = new List<Mesh> { GetMeshFromModelerBody2(modelerBody, beam.GeomExtents) };

      GetMeshFromModelerBody(modelerBody, beam.GeomExtents);

      SetUnits(advanceSteelBeam);

      return advanceSteelBeam;
    }

    public Mesh GetMeshFromModelerBody(ModelerBody modelerBody, Extents extents)
    {
      modelerBody.getBrepInfo(out var verticesAS, out var facesInfo);

      List<double> vertexList = new List<double> { };
      List<int> facesList = new List<int> { };

      foreach (var faceInfo in facesInfo)
      {
        var input = new Polygon();

        IEnumerable<Point3D> vertices = verticesAS.Select(x => PointASToMath(x));
        CoordinateSystem coordinateSystemAligned = CreateCoordinateSystemAligned(vertices);
        var transformedVertices = vertices.Select(x => x.TransformBy(coordinateSystemAligned));

        List<TriangleVertex> listOuterVertices = new List<TriangleVertex>();
      
        foreach (var indexVertex in faceInfo.OuterContour)
        {
          var vertice = transformedVertices.ElementAt(indexVertex);
          listOuterVertices.Add(new TriangleVertex(vertice.X, vertice.Y));
        }

        input.Add(new Contour(listOuterVertices));

        if (faceInfo.InnerContours != null)
        {
          foreach (var listInnerContours in faceInfo.InnerContours)
          {
            List<TriangleVertex> listInnerVertices = new List<TriangleVertex>();

            foreach (var indexVertex in listInnerContours)
            {
              var vertice = transformedVertices.ElementAt(indexVertex);
              listInnerVertices.Add(new TriangleVertex(vertice.X, vertice.Y));
            }

            input.Add(new Contour(listInnerVertices), true);
          }
        }

        CoordinateSystem coordinateSystemInverted = coordinateSystemAligned.Invert();

        var triangleMesh = (TriangleMesh)input.Triangulate();

        var verticesMesh = triangleMesh.Vertices.Select(x => new Point3D(x.X, x.Y, 0).TransformBy(coordinateSystemInverted));

        vertexList.AddRange(GetFlatCoordinates(verticesMesh));

        triangleMesh.Triangles.Select(x => x.)

        //facesList.AddRange(mesher.Faces(faceIndexOffset));

      }

      return null;
      //Mesh mesh = new Mesh(vertexList, facesList, units: ModelUnits);
      //mesh.bbox = BoxToSpeckle(extents);

      //return mesh;
    }

    private IEnumerable<double> GetFlatCoordinates(IEnumerable<Point3D> verticesMesh)
    {
      foreach (var vertice in verticesMesh)
      {
        yield return vertice.X;
        yield return vertice.Y;
        yield return vertice.Z;
      }
    }

    private CoordinateSystem CreateCoordinateSystemAligned(IEnumerable<Point3D> points)
    {
      var point1 = points.ElementAt(0);
      var point2 = points.ElementAt(1);

      //Centroid calculated to avoid non-collinear points
      var centroid = Point3D.Centroid(points);
      var plane = MathPlane.FromPoints(point1, point2, centroid);

      UnitVector3D vectorX = (point2 - point1).Normalize();
      UnitVector3D vectorZ = plane.Normal;
      UnitVector3D vectorY = vectorZ.CrossProduct(vectorX);

      CoordinateSystem fromCs = new CoordinateSystem(point1, vectorX, vectorY, vectorZ);
      CoordinateSystem toCs = new CoordinateSystem(Point3D.Origin, UnitVector3D.XAxis, UnitVector3D.YAxis, UnitVector3D.ZAxis);
      return CoordinateSystem.CreateMappingCoordinateSystem(fromCs, toCs);

    }

    public Mesh GetMeshFromModelerBody2(ModelerBody modelerBody, Extents extents)
    {
      modelerBody.getBrepInfo(out var vertices, out var facesInfo);

      List<double> vertexList = new List<double> { };
      List<int> facesList = new List<int> { };

      foreach (var faceInfo in facesInfo)
      {
        int faceIndexOffset = vertexList.Count / 3;
        var mesher = new PolygonMesher();

        List<List<double>> innerLoopList = new List<List<double>>();
        List<double> outerLoopList = new List<double> { };

        foreach (var indexVertex in faceInfo.OuterContour)
        {
          var vertex = vertices[indexVertex];

          outerLoopList.Add(vertex.x);
          outerLoopList.Add(vertex.y);
          outerLoopList.Add(vertex.z);
        }

        if (faceInfo.InnerContours != null)
        {
          foreach (var listInnerContours in faceInfo.InnerContours)
          {
            var innerLoopListOfList = new List<double> { };
            innerLoopList.Add(innerLoopListOfList);

            foreach (var indexVertex in listInnerContours)
            {
              var vertex = vertices[indexVertex];

              innerLoopListOfList.Add(vertex.x);
              innerLoopListOfList.Add(vertex.y);
              innerLoopListOfList.Add(vertex.z);
            }
          }
        }

        if (innerLoopList.Any())
        {
          mesher.Init(outerLoopList, innerLoopList);
        }
        else
        {
          mesher.Init(outerLoopList);
        }

        facesList.AddRange(mesher.Faces(faceIndexOffset));
        vertexList.AddRange(mesher.Coordinates);
      }

      Mesh mesh = new Mesh(vertexList, facesList, units: ModelUnits);
      mesh.bbox = BoxToSpeckle(extents);

      return mesh;
    }

    public static T GetFilerObjectByEntity<T>(DBObject @object) where T: FilerObject
    {
      ASObjectId idCadEntity = new ASObjectId(@object.ObjectId.OldIdPtr);
      ASObjectId idFilerObject = DatabaseManager.GetFilerObjectId(idCadEntity, false);
      if (idFilerObject.IsNull())
        return null;

      return DatabaseManager.Open(idFilerObject) as T;
    }
  }
}

#endif
