using System;
using System.Data;

// FileName : AxiomException.cs
// Author : Sean Kessler

namespace Axiom.Interpreter
{
  public class AxiomException : Exception
  {
    public AxiomResult AxiomResult{get;set;}
    public AxiomException()
    {
    }
    public AxiomException(AxiomResult axiomResult,Exception exception)
    : base(exception.Message,exception)
    {
      this.AxiomResult=axiomResult;
    }
  }
}
