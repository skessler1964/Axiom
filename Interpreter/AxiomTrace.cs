using System;
using System.Configuration;
using System.Diagnostics;

// FileName : AxiomTrace.cs
// Author : Sean Kessler

namespace Axiom.Interpreter
{
  public class AxiomTrace
  {
    public AxiomTrace()
    {
      IsTracing = false;
    }
    public bool IsTracing { get; set; }
    public String[] Values { get; set; }
    public int Column { get; set; }

    public static AxiomTrace FromConfig()
    {
      AxiomTrace trace = new AxiomTrace();
      try
      {
        String tracingEnabled = ConfigurationManager.AppSettings["AXIOM_TRACE_ENABLED"];
        if (null == tracingEnabled) return trace;
        trace.IsTracing = Boolean.Parse(tracingEnabled);
        trace.Column = int.Parse(ConfigurationManager.AppSettings["AXIOM_TRACE_COLUMN"]);
        trace.Values = ConfigurationManager.AppSettings["AXIOM_TRACE_VALUES"].Split(',');
        return trace;
      }
      catch (Exception /*exception*/)
      {
        return trace;
      }
    }
    public bool Break()
    {
      if (!Debugger.IsAttached) return false;
      Debugger.Break();  // if you wind up here you are in the debugger AND AXIOM_TRACE_ENABLED is set in the congifuration AND the break condition has been met.  Step over this breakpoint to continue debugging OR set AXIOM_TRACE_ENABLED to false in config and restart
      return true;
    }
  }
}
