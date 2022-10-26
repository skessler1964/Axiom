using System;

// FileName : Symbol.cs
// Author : Sean Kessler 

namespace Axiom.Interpreter
{
  public class Symbol
  {
    public enum SymbolType { KeywordSymbol,FunctionSymbol, UserSymbol, UserDynamicSymbol, DirectiveSymbol};  
    private bool isMutable=true;
    private bool isModified=false;
    public Symbol()
    {
      IsMutable = true;
    }
    public Symbol(String symbolName, Scanner.ScanSymbols identifier,SymbolType symbolType = SymbolType.UserSymbol)
    {
      this.SymbolName = symbolName;
      this.Identifier = identifier;
      this.TypeOfSymbol = symbolType;
      if (symbolType.Equals(SymbolType.UserSymbol)) IsMutable = true;
      else IsMutable = false;
    }
    public String SymbolName{get;set;}
    public Scanner.ScanSymbols Identifier{get;set;}
    public SymbolType TypeOfSymbol{get;set;}
    public GenericData GenericData { get; set; }
    public bool IsMutable
    {
      get { return isMutable; }
      set {isMutable=value;}
    }
    public bool IsModified
    {
      get {return isModified;}
      set {isModified=value;}
    }
  }
}
