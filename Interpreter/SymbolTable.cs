using System;
using System.Collections.Generic;
using System.Linq;
using System.Data;
using log4net;
using System.Reflection;

// FileName : SymbolTable.cs
// Author : Sean Kessler 

namespace Axiom.Interpreter
{
  public class SymbolTable : Dictionary<String, Symbol>
  {
    private static ILog logger = LogManager.GetLogger(typeof(SymbolTable));
    private Dictionary<int, Symbol> symbolsById = new Dictionary<int, Symbol>();
    public SymbolTable()
    {
      CreateSymbolTable();
    }
    public void AddObjects(List<Object> objects)
    {
      foreach(Object obj in objects)
      {
        AddObject(obj);
      }
    }
    public void AddObject(Object obj)
    {
      DataTable dataTable=new DataTable();
      Type classType=obj.GetType();
      PropertyInfo[] properties = classType.GetProperties();
      foreach(PropertyInfo propertyInfo in properties)
      {
        MethodInfo methodInfo=propertyInfo.GetGetMethod();
        String methodName=methodInfo.Name;
        if(!methodName.StartsWith("get_"))continue;
        methodName=methodName.Replace("get_",null);
        dataTable.Columns.Add(methodName);
      }
      DataRow dataRow=dataTable.NewRow();
      foreach(PropertyInfo propertyInfo in properties)
      {
        MethodInfo methodInfo=propertyInfo.GetGetMethod();
        String methodName=methodInfo.Name;
        if(!methodName.StartsWith("get_"))continue;
        methodName=methodName.Replace("get_",null);
        dataRow[methodName]=methodInfo.Invoke(obj,null);
      }
      AddUserSymbols(dataTable);
      AddUserValues(dataRow,dataTable);
    }
    public void AddUserSymbols(DataTable dataTable)
    {
      DataColumnCollection columns=dataTable.Columns;
      foreach (DataColumn column in columns)
      {
        String symbolName = column.ColumnName;
        if(ContainsKey(symbolName))continue;
        Symbol symbol=new Symbol(symbolName,Scanner.ScanSymbols.variable1,Symbol.SymbolType.UserSymbol);
        Add(symbolName,symbol);
      }
    }
    public void AddUserValues(DataRow dataRow,DataTable dataTable)
    {
      DataColumnCollection columns = dataTable.Columns;
      for(int index=0;index<columns.Count;index++)
      {
        DataColumn column = columns[index];
        String symbolName = column.ColumnName;
        if(!ContainsKey(symbolName))continue;
        Symbol symbol = this[symbolName];
        symbol.GenericData = new GenericData();
        String stringRepData = dataRow[index].ToString();
        symbol.GenericData.Data = GenericData.ConvertType(column.DataType, stringRepData);
      }
    }
    public List<Symbol> GetModfied()
    {
      List<Symbol> symbols = this.Values.ToList();
      symbols = (from Symbol symbol in symbols where symbol.TypeOfSymbol.Equals(Symbol.SymbolType.UserSymbol) && symbol.IsModified select symbol).ToList();
      return symbols;
    }
    public String GetModifiedSymbolNames()
    {
      List<Symbol> symbols = this.Values.ToList();
      List<String> symbolNames = (from Symbol symbol in symbols where symbol.TypeOfSymbol.Equals(Symbol.SymbolType.UserSymbol) && symbol.IsModified select symbol.SymbolName).ToList();
      return Utility.Utility.ListToString(symbolNames);
    }
    public void Reset()
    {
      ClearModified();
      ClearData();
      RemoveUserDynamicSymbols();  
    }
    public void RemoveUserDynamicSymbols()
    {
      List<Symbol> symbols = this.Values.ToList();
      symbols = (from Symbol symbol in symbols where symbol.TypeOfSymbol.Equals(Symbol.SymbolType.UserDynamicSymbol) select symbol).ToList();
      foreach (Symbol symbol in symbols) this.Remove(symbol.SymbolName);
    }
    public void ClearModified()
    {
      List<Symbol> symbols = this.Values.ToList();
      symbols = (from Symbol symbol in symbols where symbol.TypeOfSymbol.Equals(Symbol.SymbolType.UserSymbol) select symbol).ToList();
      foreach (Symbol symbol in symbols) symbol.IsModified = false;
    }
    public void ClearData()
    {
      List<Symbol> symbols = this.Values.ToList();
      foreach (Symbol symbol in symbols) symbol.GenericData = null;
    }
    public bool IsModified()
    {
      List<Symbol> symbols = this.Values.ToList();
      symbols = (from Symbol symbol in symbols where symbol.TypeOfSymbol.Equals(Symbol.SymbolType.UserSymbol) select symbol).ToList();
      foreach (Symbol symbol in symbols) if(symbol.IsModified)return true;
      return false;
    }
    private void CreateSymbolTable()
    {
      Add("#directive_clear_modified",new Symbol("#directive_clear_modified",Scanner.ScanSymbols.directive_clear_modified1,Symbol.SymbolType.DirectiveSymbol));
      Add("declare",new Symbol("declare",Scanner.ScanSymbols.declare1,Symbol.SymbolType.KeywordSymbol));
      Add("if",new Symbol("if",Scanner.ScanSymbols.if1,Symbol.SymbolType.KeywordSymbol));
      Add("then", new Symbol("then", Scanner.ScanSymbols.then1, Symbol.SymbolType.KeywordSymbol));
      Add("else", new Symbol("else", Scanner.ScanSymbols.else1, Symbol.SymbolType.KeywordSymbol));  
      Add("and", new Symbol("and", Scanner.ScanSymbols.andand1, Symbol.SymbolType.KeywordSymbol));  
      Add("or", new Symbol("or", Scanner.ScanSymbols.oror1, Symbol.SymbolType.KeywordSymbol));  
      Add("null", new Symbol("null", Scanner.ScanSymbols.null1, Symbol.SymbolType.KeywordSymbol));  
// high level 
      Symbol symbol=null;
      //symbol=new Symbol("bdp", Scanner.ScanSymbols.bdp1, Symbol.SymbolType.FunctionSymbol);    // bdp(security_id,security_id_type,field)
      //Add("bdp",symbol);  
      //symbolsById.Add((int)Scanner.ScanSymbols.bdp1,symbol);

      symbol=new Symbol("abs", Scanner.ScanSymbols.abs1, Symbol.SymbolType.FunctionSymbol);    // abs(x)
      Add("abs",symbol);  
      symbolsById.Add((int)Scanner.ScanSymbols.abs1,symbol);

      symbol=new Symbol("pow", Scanner.ScanSymbols.pow1, Symbol.SymbolType.FunctionSymbol);    // pow(x,y)
      Add("pow",symbol);  
      symbolsById.Add((int)Scanner.ScanSymbols.pow1,symbol);

      symbol=new Symbol("sqrt", Scanner.ScanSymbols.sqrt1, Symbol.SymbolType.FunctionSymbol);    // sqrt(x)
      Add("sqrt",symbol);  
      symbolsById.Add((int)Scanner.ScanSymbols.sqrt1,symbol);

      symbol=new Symbol("convert", Scanner.ScanSymbols.convert1, Symbol.SymbolType.FunctionSymbol);    // convert(x,'{type}')    convert(variable|literal,'{datatype}')   convert('01-02-2018','System.DateTime')
      Add("convert",symbol);  
      symbolsById.Add((int)Scanner.ScanSymbols.convert1,symbol);

      symbol=new Symbol("substring", Scanner.ScanSymbols.substring1, Symbol.SymbolType.FunctionSymbol);    // substring(variable|literal,numeral|variable,numeral|variable)    
      Add("substring",symbol);  
      symbolsById.Add((int)Scanner.ScanSymbols.substring1,symbol);

      symbol=new Symbol("getprice", Scanner.ScanSymbols.getprice1, Symbol.SymbolType.FunctionSymbol);    // getprice(variable|literal,variable|literal)  getprice('midd','07-12-2021')    
      Add("getprice",symbol);  
      symbolsById.Add((int)Scanner.ScanSymbols.getprice1,symbol);

      symbol=new Symbol("in", Scanner.ScanSymbols.in1, Symbol.SymbolType.FunctionSymbol);    // in('X','Y','Z')    
      Add("in",symbol);  
      symbolsById.Add((int)Scanner.ScanSymbols.in1,symbol);

      symbol=new Symbol("like", Scanner.ScanSymbols.like1, Symbol.SymbolType.FunctionSymbol);    // like 'x%'    
      Add("like",symbol);  
      symbolsById.Add((int)Scanner.ScanSymbols.like1,symbol);

      symbol=new Symbol("trim", Scanner.ScanSymbols.trim1, Symbol.SymbolType.FunctionSymbol);    // trim('x')
      Add("trim",symbol);  
      symbolsById.Add((int)Scanner.ScanSymbols.trim1,symbol);

      symbol=new Symbol("upper", Scanner.ScanSymbols.upper1, Symbol.SymbolType.FunctionSymbol);    // upper('x')    
      Add("upper",symbol);  
      symbolsById.Add((int)Scanner.ScanSymbols.upper1,symbol);

      symbol=new Symbol("lower", Scanner.ScanSymbols.lower1, Symbol.SymbolType.FunctionSymbol);    // lower('x')    
      Add("lower",symbol);  
      symbolsById.Add((int)Scanner.ScanSymbols.lower1,symbol);

      symbol=new Symbol("isnull", Scanner.ScanSymbols.isnull1, Symbol.SymbolType.FunctionSymbol);    // isnull(variable)
      Add("isnull",symbol);  
      symbolsById.Add((int)Scanner.ScanSymbols.isnull1,symbol);
    }
    public Symbol Find(Scanner.ScanSymbols identifier)
    {
      if (!symbolsById.ContainsKey((int)identifier)) return null;
      return this[symbolsById[(int)identifier].SymbolName];
    }
  }
}
