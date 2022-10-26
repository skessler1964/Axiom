using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Data;
using log4net;

// FileName : CodeRunner.cs
// Author : Sean Kessler

namespace Axiom.Interpreter
{
  public class CodeRunner : IDisposable
  {
    private static ILog logger = LogManager.GetLogger(typeof(CodeRunner));
    private SymbolTable symbolTable = null;
    private Dictionary<int, Stream> precompiles = new Dictionary<int, Stream>();
    private bool useCache=false;
    private bool trace=false;
    private bool parseStrict=false;
    private bool scanStrict=false;
    private bool disposed = false;

    public CodeRunner()
    {
      symbolTable=new SymbolTable();
    }
    ~CodeRunner()
    {
      Dispose(false);
    }
    public void Dispose()
    {
      if (!disposed)
      {
        Dispose(true);
        GC.SuppressFinalize(this);
      }
    }
    protected virtual void Dispose(bool disposing)
    {
      if(disposing)
      {
        List<Stream> streams=new List<Stream>(precompiles.Values);
        foreach(Stream stream in streams)
        {
          try{stream.Close();stream.Dispose();}catch(Exception){;}
        }
      }
      disposed = true;
    }
    public String LastMessage{get;set;}
    public bool ScanStrict
    {
      get{return scanStrict;}
      set{scanStrict=value;}
    }
    public bool ParseStrict
    {
      get{return parseStrict;}
      set{parseStrict=value;}
    }
    public bool Trace
    {
      get {return trace;}
      set {trace=value;}
    }
    public bool UseCache
    {
      get { return useCache; }
      set { useCache = value; }
    }
    public String GetValue(String name)
    {
      if (!symbolTable.ContainsKey(name)) return "null";
      Symbol symbol = symbolTable[name];
      GenericData genericData = symbol.GenericData;
      if (null == genericData || genericData.IsNull()) return "null";
      return genericData.Get<String>();
    }
    public T GetValue<T>(String name)
    {
      if(!symbolTable.ContainsKey(name)) return default(T);
      Symbol symbol=symbolTable[name];
      GenericData genericData=symbol.GenericData;
      if(null==genericData||genericData.IsNull()) return default(T);
      return genericData.Get<T>();
    }
    public SymbolTable SymbolTable
    {
      get {return symbolTable;}
    }
    public void Reset()
    {
      if (null == symbolTable) return;
      symbolTable.Reset();
    }
    public bool Execute(DataTable dataTable, int row, String expression)
    {
      Reset();
      return ExecuteExpressionOnRow(dataTable, row, expression);
    }
    private bool ExecuteExpressionOnRow(DataTable dataTable, int row, String expression)
    {
      symbolTable.AddUserSymbols(dataTable);                                         // add symbol names from the data table columns
      symbolTable.AddUserValues(dataTable.Rows[row], dataTable);
      if (UseCache)
      {
        if (!ApplyRuleWithCache(expression)) return false;
      }
      else
      {
        if (!Execute(expression))return false;
      }
      return true;
    }
    public bool Execute(String expression)
    {
      BinaryReader binaryReader = null;
      BinaryWriter binaryWriter = null;
      BinaryReader parserReader = null;
      BinaryWriter parserWriter = null;
      BinaryReader assemblerReader = null;
      Assembler assembler = null;

      try
      {
        binaryReader = new BinaryReader(Utility.Utility.StreamFromString(expression));
        binaryWriter = new BinaryWriter(new MemoryStream());
        Scanner scanner = new Scanner(binaryReader, binaryWriter, symbolTable);
        scanner.Debug = Trace;
        scanner.StrictMode=ScanStrict;
        if (!scanner.Analyze())
        {
          LastMessage="Failed to scan the input document, possible invalid character sequence.";
          logger.ErrorFormat(LastMessage);
          return false;
        }
        binaryWriter.BaseStream.Seek(0, SeekOrigin.Begin);
        parserReader = new BinaryReader(binaryWriter.BaseStream);
        parserWriter = new BinaryWriter(new MemoryStream());
        Parser parser = new Parser(parserReader, parserWriter, symbolTable);
        parser.Debug = Trace;
        parser.StrictMode=ParseStrict;        
        parser.Parse();
        if (parser.IsInError)
        {
          LastMessage=String.Format("Failed to parse the input, {0} at {1}", parser.LastMessage, parser.LastLineNumber);
          logger.ErrorFormat(LastMessage);
          return false;
        }
        parserWriter.BaseStream.Seek(0, SeekOrigin.Begin);
        assemblerReader = new BinaryReader(parserWriter.BaseStream);
        assembler = new Assembler(assemblerReader, symbolTable);
        assembler.Debug = Trace;
        assembler.Assemble();
        if(assembler.IsInError)
        {
          LastMessage=String.Format("Error: Failed to run the assembler, {0}", assembler.LastMessage);
          logger.ErrorFormat(LastMessage);
          return false;
        }
        if (Trace) DumpSymbolTable();
        assembler.Dispose();
        assembler = null;
        return true;
      }
      catch (Exception exception)
      {
        LastMessage=String.Format("Exception:{0}",exception.ToString());
        logger.ErrorFormat(LastMessage);
        return false;
      }
      finally
      {
        if (null != binaryReader) { binaryReader.Close();binaryReader.Dispose(); }
        if (null != binaryWriter) { binaryWriter.Close();binaryWriter.Dispose(); }
        if (null != parserReader) { parserReader.Close();parserReader.Dispose(); }
        if (null != parserWriter) { parserWriter.Close(); parserWriter.Dispose(); }
        if (null != assemblerReader) { assemblerReader.Close(); assemblerReader.Dispose(); }
        if (null != assembler) { assembler.Dispose();assembler = null; }
      }
    }
//  The cache will not work as effectively if the expression variables are not loaded directly into the SymbolTable
    public bool ApplyRuleWithCache(String expression)
    {
      BinaryReader assemblerReader = null;
      Stream precompiledStream = null;
      Assembler assembler = null;
      try
      {
        int hashcode = expression.GetHashCode();
        if (Trace) logger.InfoFormat("Trace:{0}", expression);
        if (precompiles.ContainsKey(hashcode))
        {
          precompiledStream = precompiles[hashcode];    // if the precompiled cache contains the expression then fetch it
        }
        else
        {
          precompiledStream = Compile(expression,true); // otherwise compile the expression and add it to the cache.
          precompiles.Add(hashcode, precompiledStream);
        }
        if (null == precompiledStream) return false;
        precompiledStream.Seek(0, SeekOrigin.Begin);
        assemblerReader = new BinaryReader(precompiledStream);
        assembler = new Assembler(assemblerReader, symbolTable);
        assembler.Assemble();
        if(assembler.IsInError)
        {
          LastMessage=String.Format("Error: Failed to run the assembler, {0}", assembler.LastMessage);
          logger.ErrorFormat(LastMessage);
          return false;
        }
        assembler.Dispose();
        assembler = null;
        return true;
      }
      catch (Exception exception)
      {
        LastMessage=String.Format("Exception:{0}", exception.ToString());
        logger.ErrorFormat(LastMessage);
        return false;
      }
      finally
      {
        if (null != assembler) { assembler.Dispose(); }
      }
    }
    public AxiomResult SyntaxCheck(String expression)
    {
      BinaryReader binaryReader = null;
      BinaryWriter binaryWriter = null;
      BinaryReader parserReader = null;
      BinaryWriter parserWriter = null;
      AxiomResult axiomResult = new AxiomResult();

      try
      {
        axiomResult.Success = true;
        binaryWriter = new BinaryWriter(new MemoryStream());
        binaryReader = new BinaryReader(Utility.Utility.StreamFromString(expression));
        Scanner scanner = new Scanner(binaryReader, binaryWriter, symbolTable);
        if (!scanner.Analyze())
        {
          axiomResult.Success = false;
          axiomResult.LastMessage = "Failed to scan the input document, possible invalid character sequence.";
          return axiomResult;
        }
        binaryWriter.BaseStream.Seek(0, SeekOrigin.Begin);
        parserReader = new BinaryReader(binaryWriter.BaseStream);
        parserWriter = new BinaryWriter(new MemoryStream());
        Parser parser = new Parser(parserReader, parserWriter, symbolTable);
        parser.StrictMode=false;            // we do not require all variable declarations to be present.
        parser.StrictStatementMode=true;    // we require at least one valid statement to be processed.
        parser.Parse();
        if (parser.IsInError)
        {
          axiomResult.Success = false;
          axiomResult.LastMessage = "Message:" + parser.LastMessage;
          return axiomResult;
        }
        return axiomResult;
      }
      catch (Exception exception)
      {
        axiomResult.LastMessage = String.Format("Exception:{0}", exception.ToString());
        logger.Error(axiomResult.LastMessage);
        return axiomResult;
      }
      finally
      {
        if (null != binaryReader) { binaryReader.Close(); binaryReader.Dispose(); }
        if (null != binaryWriter) { binaryWriter.Close(); binaryWriter.Dispose(); }
        if (null != parserReader) { parserReader.Close(); parserReader.Dispose(); }
        if (null != parserWriter) { parserWriter.Close(); parserWriter.Dispose(); }
      }
    }
    private Stream Compile(String expression, bool includeCodeEnd = true)
    {
      BinaryReader binaryReader = null;
      BinaryWriter binaryWriter = null;
      BinaryReader parserReader = null;
      BinaryWriter parserWriter = null;

      try
      {
        binaryReader = new BinaryReader(Utility.Utility.StreamFromString(expression));
        binaryWriter = new BinaryWriter(new MemoryStream());
        Scanner scanner = new Scanner(binaryReader, binaryWriter, symbolTable);
        scanner.Debug = Trace;
        if(!scanner.Analyze())
        {
          LastMessage = "Failed to scan the input document, possible invalid character sequence";
          logger.Info(LastMessage);
          return null;
        }
        binaryReader.Close();
        binaryReader.Dispose();
        binaryWriter.BaseStream.Seek(0, SeekOrigin.Begin);
        parserReader = new BinaryReader(binaryWriter.BaseStream);
        parserWriter = new BinaryWriter(new MemoryStream());
        Parser parser = new Parser(parserReader, parserWriter, symbolTable);
        parser.Debug = Trace;
        parser.Parse(includeCodeEnd);
        if (parser.IsInError)
        {
          LastMessage = String.Format("Error:{0} at {1}", parser.LastMessage, parser.LastLineNumber);
          logger.Info(LastMessage);
          return null;
        }
        parserReader.Close();
        parserReader.Dispose();
        parserReader = null;
        binaryWriter.Close();
        binaryWriter.Dispose();
        binaryWriter = null;
        parserWriter.BaseStream.Seek(0, SeekOrigin.Begin);
        return parserWriter.BaseStream;
      }
      catch (Exception exception)
      {
        logger.ErrorFormat("Exception:{0}", exception.ToString());
        return null;
      }
      finally
      {
        if (null != binaryReader) { binaryReader.Close(); binaryReader.Dispose(); }
        if (null != binaryWriter) { binaryWriter.Close(); binaryWriter.Dispose(); }
        if (null != parserReader) { parserReader.Close();parserReader.Dispose(); }
      }
    }
    private void DumpSymbolTable()
    {
      logger.Info("********************************************************* O U T P U T ************************************************");
      List<Symbol> list = new List<Symbol>(symbolTable.Values);
      list = (from Symbol symbol in list where symbol.TypeOfSymbol.Equals(Symbol.SymbolType.UserSymbol) select symbol).ToList();
      foreach (Symbol symbol in list)
      {
        logger.Info(String.Format("SYMBOL NAME:'{0}',VALUE:'{1}'", symbol.SymbolName, null == symbol.GenericData ? "<null>" : symbol.GenericData.ToString()));
      }
    }
  }
}