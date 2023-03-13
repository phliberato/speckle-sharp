﻿using System;
using System.Collections.Generic;
using ConnectorGrasshopper.Extras;
using GH_IO.Serialization;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Serilog;
using Serilog.Context;
using Speckle.Core.Helpers;

namespace ConnectorGrasshopper
{
  /// <summary>
  /// Base implementation for all Speckle nodes.
  /// INFO: Any changes here should be mirrored in GH_SpeckleTaskCapableComponent and GH_SpeckleAsyncComponent
  /// </summary>
  public abstract class GH_SpeckleComponent: GH_Component, ISpeckleTrackingComponent
  {
    public ComponentTracker Tracker { get; set; }
    public bool IsNew { get; set; } = true;
    
    protected GH_SpeckleComponent(string name, string nickname, string description, string category, string subCategory) : base(name, nickname, description, category, subCategory)
    {
      Tracker = new ComponentTracker(this);
    }

    public override bool Read(GH_IReader reader)
    {
      // Set isNew to false, indicating this node already existed in some way. This prevents the `NodeCreate` event from being raised.
      IsNew = false;
      return base.Read(reader);
    }

    public override void AddedToDocument(GH_Document document)
    {
      base.AddedToDocument(document);
      // If the node is new (i.e. GH has not called Read(...) ) we log the node creation event.
      if (IsNew)
      {
        Tracker.TrackNodeCreation();
        IsNew = false;
      }
    }
    
    protected sealed override void SolveInstance(IGH_DataAccess DA)
    {
      var guid = Guid.NewGuid();
      
      // TODO: The traceId should also be added as part of the request headers, but those live inside the Speckle `Client` class
      // TODO: We should add a way to override the TraceId on the client side so instead of all this we do Client.SetTraceId() or similar.
      // var httpClient = Http.GetHttpProxyClient();
      // var name = "x-request-id";
      // if (httpClient.DefaultRequestHeaders.Contains(name))
      //   httpClient.DefaultRequestHeaders.Remove(name);
      // httpClient.DefaultRequestHeaders.Add(name, guid.ToString());
      
      using (LogContext.PushProperty("hostApplication", Utilities.GetVersionedAppName()))
      using (LogContext.PushProperty("grasshopperComponent", GetType().Name))
      using(LogContext.PushProperty("traceId", guid))
        SolveInstanceWithLogContext(DA);
    }

    public abstract void SolveInstanceWithLogContext(IGH_DataAccess DA);
  }
}
