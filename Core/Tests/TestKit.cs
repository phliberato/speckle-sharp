using Newtonsoft.Json;
using Speckle.Core.Kits;
using Speckle.Core.Models;

namespace Tests;

/// <summary>
/// Simple speckle kit (no conversions) used in tests.
/// </summary>
public class TestKit : ISpeckleKit
{
  public TestKit() { }

  public IEnumerable<Type> Types =>
    GetType().Assembly.GetTypes().Where(type => type.IsSubclassOf(typeof(Base)));

  public string Description => "Simple object model for with some types for tests.";

  public string Name => nameof(TestKit);

  public string Author => "Dimitrie";

  public string WebsiteOrEmail => "hello@Speckle.Core.works";

  public IEnumerable<string> Converters => new List<string>();

  public ISpeckleConverter LoadConverter(string app)
  {
    return null;
  }

  public Base ToSpeckle(object @object)
  {
    throw new NotImplementedException();
  }

  public bool CanConvertToSpeckle(object @object)
  {
    throw new NotImplementedException();
  }

  public object ToNative(Base @object)
  {
    throw new NotImplementedException();
  }

  public bool CanConvertToNative(Base @object)
  {
    throw new NotImplementedException();
  }

  public IEnumerable<string> GetServicedApplications()
  {
    throw new NotImplementedException();
  }

  public void SetContextDocument(object @object)
  {
    throw new NotImplementedException();
  }
}

public class FakeMesh : Base
{
  public FakeMesh() { }

  [DetachProperty, Chunkable]
  public List<double> Vertices { get; set; } = new();

  [DetachProperty, Chunkable(1000)]
  public double[] ArrayOfDoubles { get; set; }

  [DetachProperty, Chunkable(1000)]
  public TableLeg[] ArrayOfLegs { get; set; }

  [DetachProperty, Chunkable(2500)]
  public List<Tabletop> Tables { get; set; } = new();
}

public class DiningTable : Base
{
  public DiningTable()
  {
    LegOne = new TableLeg() { height = 2 * 3, radius = 10 };
    LegTwo = new TableLeg() { height = 1, radius = 5 };

    MoreLegs.Add(new TableLeg() { height = 4 });
    MoreLegs.Add(new TableLeg() { height = 10 });

    Tabletop = new Tabletop()
    {
      length = 200,
      width = 12,
      thickness = 3
    };
  }

  [DetachProperty]
  public TableLeg LegOne { get; set; }

  [DetachProperty]
  public TableLeg LegTwo { get; set; }

  [DetachProperty]
  public List<TableLeg> MoreLegs { get; set; } = new();

  [DetachProperty]
  public Tabletop Tabletop { get; set; }

  public string TableModel { get; set; } = "Sample Table";
}

public class Tabletop : Base
{
  public Tabletop() { }

  public double length { get; set; }
  public double width { get; set; }
  public double thickness { get; set; }
}

public class TableLeg : Base
{
  public TableLeg() { }

  public double height { get; set; }
  public double radius { get; set; }

  [DetachProperty]
  public TableLegFixture fixture { get; set; } = new();
}

public class TableLegFixture : Base
{
  public TableLegFixture() { }

  public string nails { get; set; } = "MANY NAILS WOW ";
}

public class Point : Base
{
  public Point() { }

  public Point(double X, double Y, double Z)
  {
    this.X = X;
    this.Y = Y;
    this.Z = Z;
  }

  public double X { get; set; }
  public double Y { get; set; }
  public double Z { get; set; }
}

public class SuperPoint : Point
{
  public SuperPoint() { }

  public double W { get; set; }
}

public class Mesh : Base
{
  public List<int> Faces = new();

  [JsonIgnore]
  public List<Point> Points = new();

  public Mesh() { }

  public List<double> Vertices
  {
    get => Points.SelectMany(pt => new List<double>() { pt.X, pt.Y, pt.Z }).ToList();
    set
    {
      for (int i = 0; i < value.Count; i += 3)
        Points.Add(new Point(value[i], value[i + 1], value[i + 2]));
    }
  }
}

public interface ICurve
{
  // Just for fun
}

/// <summary>
/// Store individual points in a list structure for developer ergonomics. Nevertheless, for performance reasons (hashing, serialisation & storage) expose the same list of points as a typed array.
/// </summary>
public class Polyline : Base, ICurve
{
  [JsonIgnore]
  public List<Point> Points = new();

  public Polyline() { }

  public List<double> Vertices
  {
    get => Points.SelectMany(pt => new List<double>() { pt.X, pt.Y, pt.Z }).ToList();
    set
    {
      for (int i = 0; i < value.Count; i += 3)
        Points.Add(new Point(value[i], value[i + 1], value[i + 2]));
    }
  }
}

public class Line : Base, ICurve
{
  public Line() { }

  public Point Start { get; set; }
  public Point End { get; set; }
}

/// <summary>
/// This class exists to purely test some weird cases in which Intefaces might trash serialisation.
/// </summary>
public class PolygonalFeline : Base
{
  public PolygonalFeline() { }

  public List<ICurve> Whiskers { get; set; } = new();

  public Dictionary<string, ICurve> Claws { get; set; } = new();

  [DetachProperty]
  public ICurve Tail { get; set; }

  public ICurve[] Fur { get; set; } = new ICurve[1000];
}
