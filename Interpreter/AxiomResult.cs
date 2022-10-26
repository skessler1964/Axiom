using System;
using System.Data;

// FileName : AxiomResult.cs
// Author : Sean Kessler

namespace Axiom.Interpreter
{
  public class AxiomResult
  {
    public AxiomResult()
    {
      Success = false;
    }
    public AxiomResult(bool success,String lastMessage)
    {
      Success = success;
      LastMessage = lastMessage;
    }
    public bool Success { get; set; }
    public String LastMessage { get; set; }
    public DataTable DataTable { get; set; }
    public int ContextSpecificId{get;set;}
    public Object ContextSpecificObj { get; set; }
  }
}
