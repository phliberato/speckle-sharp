﻿using ConnectorGrashopper.Extras;
using GH_IO.Serialization;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ConnectorGrashopper.Conversion
{
  public class ToSpeckleConverter : GH_TaskCapableComponent<ToSpeckleConverter.SolveResults>
  {
    public override Guid ComponentGuid { get => new Guid("2092AF4C-51CD-4CB3-B297-5348C51FC49F"); }

    protected override System.Drawing.Bitmap Icon { get => null; }

    public override GH_Exposure Exposure => GH_Exposure.primary;

    private ISpeckleConverter Converter;

    private ISpeckleKit Kit;

    public ToSpeckleConverter() : base("To Speckle", "⇒ SPK", "Converts objects to their Speckle equivalents.", "Speckle 2", "Conversion")
    {
      Kit = KitManager.GetDefaultKit();
      try
      {
        Converter = Kit.LoadConverter(Applications.Rhino);
        Message = $"Using the \n{Kit.Name}\n Kit Converter";
      }
      catch
      {
        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "No default kit found on this machine.");
      }
    }

    public override void AppendAdditionalMenuItems(ToolStripDropDown menu)
    {
      Menu_AppendSeparator(menu);
      Menu_AppendItem(menu, "Select the converter you want to use:");

      var kits = KitManager.GetKitsWithConvertersForApp(Applications.Rhino);

      foreach (var kit in kits)
      {
        Menu_AppendItem(menu, $"{kit.Name} ({kit.Description})", (s, e) => { SetConverterFromKit(kit.Name); }, true, kit.Name == Kit.Name);
      }

      Menu_AppendSeparator(menu);
    }

    private void SetConverterFromKit(string kitName)
    {
      if (kitName == Kit.Name) return;

      Kit = KitManager.Kits.FirstOrDefault(k => k.Name == kitName);
      Converter = Kit.LoadConverter(Applications.Rhino);

      Message = $"Using the \n{Kit.Name}\n Kit Converter";
      ExpireSolution(true);
    }

    public override bool Read(GH_IReader reader)
    {
      // TODO: Read kit name and instantiate converter
      return base.Read(reader);
    }

    public override bool Write(GH_IWriter writer)
    {
      // TODO: Write kit name to disk
      return base.Write(writer);
    }

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
      pManager.AddGenericParameter("Objects", "O", "Objects you want to convert to Speckle", GH_ParamAccess.list);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
      pManager.AddGenericParameter("Converterd", "C", "Converted objects.", GH_ParamAccess.list);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
      if (InPreSolve)
      {
        var data = new List<IGH_Goo>();
        DA.GetDataList(0, data);
        if (data == null || data.Count == 0) return;

        var task = Task.Run(() => { return Compute(data); }, CancelToken);

        TaskList.Add(task);
        return;
      }

      if (!GetSolveResults(DA, out SolveResults result))
      {
        var data = new List<IGH_Goo>();
        DA.GetDataList(0, data);
        if (data == null || data.Count == 0) return;

        result = Compute(data);
      }

      if(result != null)
      {
        DA.SetDataList(0, result.ConvertedObjects);
      }


      //List<IGH_Goo> data = new List<IGH_Goo>();
      //DA.GetDataList(0, data);

      //var myList = new List<object>();
      //var values = new List<IGH_Goo>();
      //var j = 0;
      //DA.GetDataList(0, values);

      //if (values == null || values.Count == 0)
      //{
      //  AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "List is null or empty.");
      //  return;
      //}

      //foreach (var item in values)
      //{
      //  var conv = TryConvertItem(item);
      //  myList.Add(conv);
      //  if (conv == null)
      //  {
      //    AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"Data of type {item.GetType().Name} at index {j} could not be converted.");
      //  }
      //  j++;
      //}

      //DA.SetDataList(0, myList);
    }


    public class SolveResults
    {
      public List<object> ConvertedObjects = new List<object>();
    }

    private SolveResults Compute(List<IGH_Goo> objects)
    {
      var results = new SolveResults();
      var j = 1;
      foreach (var item in objects)
      {
        var conv = TryConvertItem(item);
        results.ConvertedObjects.Add(conv);
        if (conv == null)
        {
          AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"Data of type {item.GetType().Name} at index {j} could not be converted.");
        }
        j++;
      }

      return results;
    }

    private object TryConvertItem(object value)
    {
      object result = null;

      if (value is Grasshopper.Kernel.Types.IGH_Goo)
      {
        value = value.GetType().GetProperty("Value").GetValue(value);
      }
      else if (value is Base || Speckle.Core.Models.Utilities.IsSimpleType(value.GetType()))
      {
        return value;
      }

      if (Converter.CanConvertToSpeckle(value))
      {
        return new GH_SpeckleBase() { Value = Converter.ConvertToSpeckle(value) };
      }

      return result;
    }

  }


}
