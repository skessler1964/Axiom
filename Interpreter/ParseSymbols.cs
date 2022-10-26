using System;
using System.Collections.Generic;
using System.Linq;

// FileName : ParseSymbols.cs
// Author : Sean Kessler

namespace Axiom.Interpreter
{
  public class ParseSymbols : List<ParseSymbol>
  {
    private Dictionary<Scanner.ScanSymbols, Scanner.ScanSymbols> uniqueSymbols = new Dictionary<Scanner.ScanSymbols, Scanner.ScanSymbols>();

    public ParseSymbols()
    {
    }
    public ParseSymbols(List<ParseSymbol> symbols)
    {
      foreach (ParseSymbol symbol in symbols) InsertSymbols(symbol);
    }
    public new void Add(ParseSymbol parseSymbol)
    {
      InsertSymbols(parseSymbol);
    }
    public void InsertSymbols(ParseSymbols symbols)
    {
      foreach (ParseSymbol parseSymbol in symbols)
      {
        base.Add(parseSymbol);
        if (!uniqueSymbols.ContainsKey(parseSymbol.Symbol)) uniqueSymbols.Add(parseSymbol.Symbol,parseSymbol.Symbol);
      }
    }
    public void InsertSymbols(ParseSymbol parseSymbol)
    {
      base.Add(parseSymbol);
      if (!uniqueSymbols.ContainsKey(parseSymbol.Symbol)) uniqueSymbols.Add(parseSymbol.Symbol,parseSymbol.Symbol);
    }
    public void RemoveSymbols(ParseSymbols symbols)
    {
      for (int index = symbols.Count - 1; index >= 0;index--)
      {
        ParseSymbol symbol = symbols[index];
        base.Remove(symbol);
        int count = (from ParseSymbol parseSymbol in this where parseSymbol.Symbol.Equals(symbol.Symbol) select parseSymbol).Count();
        if (0 == count && uniqueSymbols.ContainsKey(symbol.Symbol)) uniqueSymbols.Remove(symbol.Symbol);
      }
    }
    public void RemoveSymbols(ParseSymbol symbol)
    {
      base.Remove(symbol);
      int count = (from ParseSymbol parseSymbol in this where parseSymbol.Symbol.Equals(symbol.Symbol) select parseSymbol).Count(); 
      if(0==count&&uniqueSymbols.ContainsKey(symbol.Symbol))uniqueSymbols.Remove(symbol.Symbol);
    }
    public bool SymbolIn(Scanner.ScanSymbols symbol)
    {
      return uniqueSymbols.ContainsKey(symbol);
    }
    public bool SymbolIn(ParseSymbols parseSymbols)
    {
      foreach (ParseSymbol symbol in parseSymbols) if (uniqueSymbols.ContainsKey(symbol.Symbol)) return true;
      return false;
    }
    public bool SymbolIn(ParseSymbol parseSymbol)
    {
      return uniqueSymbols.ContainsKey(parseSymbol.Symbol); 
    }
  }
    public class ParseSymbol : IEquatable<ParseSymbol>
    {
      private Scanner.ScanSymbols symbol;
      public ParseSymbol()
      {
      }
      public ParseSymbol(Scanner.ScanSymbols symbol)
      {
        this.symbol = symbol;
      }
      public Scanner.ScanSymbols Symbol
      {
        get
        {
          return symbol;
        }
      }
      public bool Equals(ParseSymbol parseSymbol)
      {
        if (parseSymbol == null) return false;
        return Symbol.Equals(parseSymbol.Symbol);
      }
    }
}
