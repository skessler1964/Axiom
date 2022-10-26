using System;
using System.Collections.Generic;
using System.IO;
using System.Configuration;
using log4net;
using System.Globalization;


// FileName : Assembler.cs
// Author : Sean Kessler 

namespace Axiom.Interpreter
{
  public class Assembler
  {
    private static ILog logger = LogManager.GetLogger(typeof(Assembler));
    private BinaryReader binaryReader;
    private SymbolTable symbolTable;
    private CodeStack codeStack = new CodeStack();
    private bool isInError=false;
    private String lastMessage;

    public Assembler(BinaryReader binaryReader, SymbolTable symbolTable)
    {
      this.binaryReader=binaryReader;
      this.symbolTable = symbolTable;
      this.isInError=false;
    }
    public bool IsInError
    {
      get{return isInError;}
    }
    public String LastMessage
    {
      get{return lastMessage;}
    }
    public bool Debug {get;set;}
    public void Dispose()
    {
    }
// ******************************************************************************************************************************************************************************************************************************
// ****************************************************************************************************** M A I N  A S S E M B L E R  S T A G E S  ******************************************************************************
// ******************************************************************************************************************************************************************************************************************************
// main code assembler
    public bool Assemble()
    {
      
      try
      {
        isInError=false;
        Parser.ParserSymbols symbol = Parser.ParserSymbols.undefined2;
        while (symbol != Parser.ParserSymbols.codeend2)
        {
          if(isInError)break;
          long position = binaryReader.BaseStream.Position;
          symbol = (Parser.ParserSymbols)binaryReader.ReadInt32();
          switch (symbol)
          {
            case Parser.ParserSymbols.directive_clear_modified2 :
              DirectiveClearModified();
              break;
            case Parser.ParserSymbols.declare2 :
              Declare();
              break;
            case Parser.ParserSymbols.oror2 :
              Or();
              break;
            case Parser.ParserSymbols.andand2 :
              AndAnd();
              break;
            case Parser.ParserSymbols.less2 :
              Less();
              break;
            case Parser.ParserSymbols.lessequal2 :
              LessEqual();
              break;
            case Parser.ParserSymbols.greater2 :
              Greater();
              break;
            case Parser.ParserSymbols.greaterequal2 :
              GreaterEqual();
              break;
            case Parser.ParserSymbols.equalequal2 :
              EqualEqual();
              break;
            case Parser.ParserSymbols.notequal2 :
              NotEqual();
              break;
            case Parser.ParserSymbols.noop2 :
              Nop();
              break;
            case Parser.ParserSymbols.goto2 :
              Goto();
              break;
            case Parser.ParserSymbols.defaddr2 :
              Defaddr();
              break;
            case Parser.ParserSymbols.divide2 :  // when we add we pop 2 items, add them and then push the result on the stack
              Divide();
              break;
            case Parser.ParserSymbols.multiply2 :  // when we add we pop 2 items, add them and then push the result on the stack
              Multiply();
              break;
            case Parser.ParserSymbols.add2 :  // when we add we pop 2 items, add them and then push the result on the stack
              Add();
              break;
            case Parser.ParserSymbols.subtract2 :
              Subtract();
              break;
            case Parser.ParserSymbols.assign2 :
              Assign();
              break;
            case Parser.ParserSymbols.variableaccess2 :
              VariableAccess();
              break;
            case Parser.ParserSymbols.call2 :
              Call();
              break;
            case Parser.ParserSymbols.push2 :
              Push();
              break;
            case Parser.ParserSymbols.negate2 :
              Negate();
              break;
            case Parser.ParserSymbols.not2 :
              Not();
              break;
            case Parser.ParserSymbols.codeend2 :
              break;
            default :
              logger.Info(String.Format("No action for symbol {0}",Parser.SymbolToString(symbol)));
              break;
          }
        }
        return !isInError;
      }
      catch(Exception exception)
      {
        logger.ErrorFormat("Exception during Assembly:{0}",exception);
        return false;
      }
    }
// the disassembler
    public long Disassemble()
    {
      String variableType = null;
      isInError=false;
      Parser.ParserSymbols symbol = Parser.ParserSymbols.undefined2;
      binaryReader.BaseStream.Seek(0, SeekOrigin.Begin);
      long position=binaryReader.BaseStream.Position;
      long lastPosition = 0;
      while (symbol != Parser.ParserSymbols.codeend2)
      {
        long value = 0L;
        position = binaryReader.BaseStream.Position;
        try { symbol = (Parser.ParserSymbols)binaryReader.ReadInt32(); }
        catch (Exception) { break; }
        switch (symbol)
        {
          case Parser.ParserSymbols.defaddr2 :
            value = binaryReader.ReadInt64();
            logger.Info(String.Format("Position:{0} Symbol:{1} Value:{2}",position,Parser.SymbolToString(symbol),value));
            break;
          case Parser.ParserSymbols.goto2 :
            value = binaryReader.ReadInt64();
            logger.Info(String.Format("Position:{0} Symbol:{1} Value:{2}",position,Parser.SymbolToString(symbol),value));
            break;
          case Parser.ParserSymbols.directive_clear_modified2 :
          case Parser.ParserSymbols.oror2 :
          case Parser.ParserSymbols.andand2 :
          case Parser.ParserSymbols.less2 :
          case Parser.ParserSymbols.lessequal2 :
          case Parser.ParserSymbols.greater2 :
          case Parser.ParserSymbols.greaterequal2 :
          case Parser.ParserSymbols.equalequal2 :
          case Parser.ParserSymbols.notequal2 :
          case Parser.ParserSymbols.noop2 :
          case Parser.ParserSymbols.divide2 :  // when we div we pop 2 items, div them and then push the result on the stack
          case Parser.ParserSymbols.multiply2 :  // when we mul we pop 2 items, mul them and then push the result on the stack
          case Parser.ParserSymbols.add2 :  // when we add we pop 2 items, add them and then push the result on the stack
          case Parser.ParserSymbols.subtract2 :
          case Parser.ParserSymbols.assign2 :
          case Parser.ParserSymbols.negate2 :
          case Parser.ParserSymbols.declare2 :
          case Parser.ParserSymbols.not2 :
            logger.Info(String.Format("Position:{0} Symbol:{1}",position,Parser.SymbolToString(symbol)));
            break;
          case Parser.ParserSymbols.codeend2 :
            lastPosition = binaryReader.BaseStream.Position;
            logger.Info(String.Format("Position:{0} Symbol:{1}",position,Parser.SymbolToString(symbol)));
            break;
          case Parser.ParserSymbols.variableaccess2 :
            variableType = binaryReader.ReadString();
            String variableName = binaryReader.ReadString();
            logger.Info(String.Format("Position:{0} Symbol:{1} VariableType:{2} VariableName:{3}",position,Parser.SymbolToString(symbol),variableType,variableName));
            break;
          case Parser.ParserSymbols.call2 :
            String functionNameType = binaryReader.ReadString();
            String functionName = binaryReader.ReadString();
            int stackParameterCount = binaryReader.ReadInt32();
            logger.Info(String.Format("Position:{0} Symbol:{1} FunctionType:{2} FunctionName:{3} StackItems:{4}",position,Parser.SymbolToString(symbol),functionNameType,functionName,stackParameterCount));
            break;
          case Parser.ParserSymbols.push2 :
            variableType = binaryReader.ReadString();
            String variableValue = binaryReader.ReadString();
            logger.Info(String.Format("Position:{0} Symbol:{1} VariableType:{2} VariableValue:{3}",position,Parser.SymbolToString(symbol),variableType,variableValue));
            break;
          default :
            break;
        }
      }
      binaryReader.BaseStream.Seek(0, SeekOrigin.Begin);
      return lastPosition;
    }
// Fixup is used for combining multiple assembly streams.  It resolves jump targets across each of these streams by offsetting the target address destinations by the relative code lengths of the streams.
    public void Fixup(long fixupAddress)
    {
      Dictionary<long, long> offsets = new Dictionary<long, long>();
      String variableType = null;
      if(isInError)return;
      Parser.ParserSymbols symbol = Parser.ParserSymbols.undefined2;
      binaryReader.BaseStream.Seek(0, SeekOrigin.Begin);
      while (symbol != Parser.ParserSymbols.codeend2)
      {
        long offset = 0L;
        long value = 0L;
        long position = binaryReader.BaseStream.Position;
        try { symbol = (Parser.ParserSymbols)binaryReader.ReadInt32(); }
        catch (Exception) { break; }
        switch (symbol)
        {
          case Parser.ParserSymbols.defaddr2 :
            offset = binaryReader.BaseStream.Position;
            value = binaryReader.ReadInt64();
            offsets.Add(offset,value);
            break;
          case Parser.ParserSymbols.goto2 :
            offset = binaryReader.BaseStream.Position;
            value = binaryReader.ReadInt64();
            offsets.Add(offset,value);
            break;
          case Parser.ParserSymbols.directive_clear_modified2 :
          case Parser.ParserSymbols.oror2 :
          case Parser.ParserSymbols.andand2 :
          case Parser.ParserSymbols.less2 :
          case Parser.ParserSymbols.lessequal2 :
          case Parser.ParserSymbols.greater2 :
          case Parser.ParserSymbols.greaterequal2 :
          case Parser.ParserSymbols.equalequal2 :
          case Parser.ParserSymbols.notequal2 :
          case Parser.ParserSymbols.noop2 :
          case Parser.ParserSymbols.divide2 :  // when we add we pop 2 items, add them and then push the result on the stack
          case Parser.ParserSymbols.multiply2 :  // when we add we pop 2 items, add them and then push the result on the stack
          case Parser.ParserSymbols.add2 :  // when we add we pop 2 items, add them and then push the result on the stack
          case Parser.ParserSymbols.subtract2 :
          case Parser.ParserSymbols.assign2 :
          case Parser.ParserSymbols.negate2 :
          case Parser.ParserSymbols.declare2 :
          case Parser.ParserSymbols.not2 :
            break;
          case Parser.ParserSymbols.variableaccess2 :
            variableType = binaryReader.ReadString();
            String variableName = binaryReader.ReadString();
            break;
          case Parser.ParserSymbols.call2 :
            String functionNameType = binaryReader.ReadString();
            String functionName = binaryReader.ReadString();
            int stackParameterCount = binaryReader.ReadInt32();
            break;
          case Parser.ParserSymbols.push2 :
            variableType = binaryReader.ReadString();
            String variableValue = binaryReader.ReadString();
            break;
          case Parser.ParserSymbols.codeend2 :
            break;
          default :
            break;
        }
      }
      binaryReader.BaseStream.Seek(0, SeekOrigin.Begin);
      BinaryWriter binaryWriter = new BinaryWriter(binaryReader.BaseStream);
      List<long> addressList = new List<long>(offsets.Keys);
      foreach (long address in addressList)
      {
        binaryWriter.Seek((int)address, SeekOrigin.Begin);
        long value=offsets[address];
        binaryWriter.Write((long)(value+fixupAddress));
        binaryWriter.Flush();
      }
      binaryReader.BaseStream.Seek(0, SeekOrigin.Begin);
    }
// ******************************************************************************************************************************************************************************************************************************
// ****************************************************************************************************** D E C L A R A T I O N  ************************************************************************************************
// ******************************************************************************************************************************************************************************************************************************
    private void Declare()
    {
      if(isInError)return;
      String variableType = binaryReader.ReadString();
      String variableName = binaryReader.ReadString();
      if(symbolTable.ContainsKey(variableName))
      {
        isInError = true;
        lastMessage=String.Format("Runtime Error: The variable '{0}' has already been declared.",variableName);
        return;
      }
      Symbol symbol = new Symbol(variableName, Scanner.ScanSymbols.variable1, Symbol.SymbolType.UserDynamicSymbol);
      symbol.GenericData = new GenericData();
      symbol.GenericData.SetNull(variableType);
      symbolTable.Add(variableName, symbol);
    }
// ******************************************************************************************************************************************************************************************************************************
// ************************************************************************************************************* C A L L S  *****************************************************************************************************
// ******************************************************************************************************************************************************************************************************************************
    private void Call()
    {
      try
      {
        if(isInError)return;
        String functionNameType = binaryReader.ReadString();
        String functionName = binaryReader.ReadString();
        int stackParameterCount = binaryReader.ReadInt32();
        StackElement[] stackElements = new StackElement[stackParameterCount];
        for (int index = stackElements.Length - 1; index >= 0; index--)
        {
          stackElements[index] = codeStack.Pop();
        }
        switch (functionName)
        {
          //case "bdp":
          //  PerformBDP(stackElements);
          //  break;
          case "abs":
            PerformABS(stackElements);
             break;
          case "pow":
            PerformPOW(stackElements);
            break;
          case "sqrt":
            PerformSQRT(stackElements);
            break;
          case "convert" :
            PerformConvert(stackElements);
            break;
          case "in" :
            PerformIn(stackElements);
            break;
          case "isnull" :
            PerformIsNull(stackElements);
            break;
          case "like" :
            PerformLike(stackElements);
            break;
          case "trim" :
            PerformTrim(stackElements);
            break;
          case "upper" :
            PerformUpper(stackElements);
            break;
          case "lower" :
            PerformLower(stackElements);
            break;
          case "substring" :
            PerformSubstring(stackElements);
            break;
          case "getprice" :
            PerformGetPrice(stackElements);
            break;
          default:
            break;
        }
      }
      finally
      {
      }
    }
// ******************************************************************************************************************************************************************************************************************************
// **************************************************************************************************** O P E R A T I O N S *****************************************************************************************************
// ******************************************************************************************************************************************************************************************************************************
    private void Nop()
    {
    }
    private void Goto()
    {
      try
      {
        if(isInError)return;
        long position = binaryReader.ReadInt64();
        binaryReader.BaseStream.Seek(position, SeekOrigin.Begin);
      }
      catch(Exception exception)
      {
        isInError=true;
        lastMessage=exception.ToString();
      }
    }
    private void Defaddr()
    {
      try
      {
        if(isInError)return;
        long position = binaryReader.ReadInt64();
        StackElement stackElement1 = codeStack.Pop();
        bool result = stackElement1.Value.Get<Boolean>();
        if (!result) binaryReader.BaseStream.Seek(position, SeekOrigin.Begin);
      }
      catch(Exception exception)
      {
        isInError=true;
        lastMessage=exception.ToString();
      }
    }
    private void Or()
    {
      try
      {
        if(isInError)return;
        StackElement stackElement2 = codeStack.Pop();
        StackElement stackElement1 = codeStack.Pop();
        StackElement element = new StackElement();
        GenericData genericData1 = null == stackElement1.Symbol ? stackElement1.Value : stackElement1.Symbol.GenericData;
        GenericData genericData2 = null == stackElement2.Symbol ? stackElement2.Value : stackElement2.Symbol.GenericData;
        if(null==genericData1)genericData1=new GenericData();
        element.Value = genericData1.Or(genericData2);
        codeStack.Push(element);
      }
      catch(Exception exception)
      {
        isInError=true;
        lastMessage=exception.ToString();
      }
    }
    private void AndAnd()
    {
      try
      {
        if(isInError)return;
        StackElement stackElement2 = codeStack.Pop();
        StackElement stackElement1 = codeStack.Pop();
        StackElement element = new StackElement();
        GenericData genericData1 = null == stackElement1.Symbol ? stackElement1.Value : stackElement1.Symbol.GenericData;
        GenericData genericData2 = null == stackElement2.Symbol ? stackElement2.Value : stackElement2.Symbol.GenericData;
        if(null==genericData1)genericData1=new GenericData();
        element.Value = genericData1.And(genericData2);
        codeStack.Push(element);
      }
      catch(Exception exception)
      {
        isInError=true;
        lastMessage=exception.ToString();
      }
    }
    private void LessEqual()
    {
      try
      {
        if(isInError)return;
        StackElement stackElement2 = codeStack.Pop();
        StackElement stackElement1 = codeStack.Pop();
        StackElement element = new StackElement();
        GenericData genericData1 = null == stackElement1.Symbol ? stackElement1.Value : stackElement1.Symbol.GenericData;
        GenericData genericData2 = null == stackElement2.Symbol ? stackElement2.Value : stackElement2.Symbol.GenericData;
        if(null==genericData1)genericData1=new GenericData();
        element.Value = genericData1.LessEqual(genericData2);
        codeStack.Push(element);
      }
      catch(Exception exception)
      {
        isInError=true;
        lastMessage=exception.ToString();
      }
    }
    private void Greater()
    {
      try
      {
        if(isInError)return;
        StackElement stackElement2 = codeStack.Pop();
        StackElement stackElement1 = codeStack.Pop();
        StackElement element = new StackElement();
        GenericData genericData1 = null == stackElement1.Symbol ? stackElement1.Value : stackElement1.Symbol.GenericData;
        GenericData genericData2 = null == stackElement2.Symbol ? stackElement2.Value : stackElement2.Symbol.GenericData;
        if(null==genericData1)genericData1=new GenericData();
        element.Value = genericData1.Greater(genericData2);
        codeStack.Push(element);
      }
      catch(Exception exception)
      {
        isInError=true;
        lastMessage=exception.ToString();
      }
    }
    private void GreaterEqual()
    {
      try
      {
        if(isInError)return;
        StackElement stackElement2 = codeStack.Pop();
        StackElement stackElement1 = codeStack.Pop();
        StackElement element = new StackElement();
        GenericData genericData1 = null == stackElement1.Symbol ? stackElement1.Value : stackElement1.Symbol.GenericData;
        GenericData genericData2 = null == stackElement2.Symbol ? stackElement2.Value : stackElement2.Symbol.GenericData;
        element.Value = genericData1.GreaterEqual(genericData2);
        if(null==genericData1)genericData1=new GenericData();
        codeStack.Push(element);
      }
      catch(Exception exception)
      {
        isInError=true;
        lastMessage=exception.ToString();
      }
    }
    private void EqualEqual()
    {
      try
      {
        if(isInError)return;
        StackElement stackElement2 = codeStack.Pop();
        StackElement stackElement1 = codeStack.Pop();
        StackElement element = new StackElement();
        GenericData genericData1 = null == stackElement1.Symbol ? stackElement1.Value : stackElement1.Symbol.GenericData;
        GenericData genericData2 = null == stackElement2.Symbol ? stackElement2.Value : stackElement2.Symbol.GenericData;
        if(null==genericData1)genericData1=new GenericData();
        element.Value = genericData1.EqualEqual(genericData2);
        codeStack.Push(element);
      }
      catch(Exception exception)
      {
        isInError=true;
        lastMessage=exception.ToString();
      }
    }
    private void NotEqual()
    {
      try
      {
        if(isInError)return;
        StackElement stackElement2 = codeStack.Pop();
        StackElement stackElement1 = codeStack.Pop();
        StackElement element = new StackElement();
        GenericData genericData1 = null == stackElement1.Symbol ? stackElement1.Value : stackElement1.Symbol.GenericData;
        GenericData genericData2 = null == stackElement2.Symbol ? stackElement2.Value : stackElement2.Symbol.GenericData;
        if(null==genericData1)genericData1=new GenericData();
        element.Value = genericData1.NotEqual(genericData2);
        codeStack.Push(element);
      }
      catch(Exception exception)
      {
        isInError=true;
        lastMessage=exception.ToString();
      }
    }
    private void Less()
    {
      try
      {
        if(isInError)return;
        StackElement stackElement2 = codeStack.Pop();
        StackElement stackElement1 = codeStack.Pop();
        StackElement element = new StackElement();
        GenericData genericData1 = null == stackElement1.Symbol ? stackElement1.Value : stackElement1.Symbol.GenericData;
        GenericData genericData2 = null == stackElement2.Symbol ? stackElement2.Value : stackElement2.Symbol.GenericData;
        if(null==genericData1)genericData1=new GenericData();
        element.Value = genericData1.Less(genericData2);
        codeStack.Push(element);
      }
      catch(Exception exception)
      {
        isInError=true;
        lastMessage=exception.ToString();
      }
    }
    private void Negate()
    {
      try
      {
        if(isInError)return;
        StackElement stackElement = codeStack.Pop();
        StackElement element = new StackElement();
        if (null != stackElement.Symbol) element.Value = stackElement.Symbol.GenericData.Negate();
        else element.Value = stackElement.Value.Negate();
        codeStack.Push(element);
      }
      catch(Exception exception)
      {
        isInError=true;
        lastMessage=exception.ToString();
      }
    }
    private void Not()
    {
      try
      {
        if(isInError)return;
        StackElement stackElement = codeStack.Pop();
        StackElement element = new StackElement();
        if (null != stackElement.Symbol) element.Value = stackElement.Symbol.GenericData.Not();
        else element.Value = stackElement.Value.Not();
        codeStack.Push(element);
      }
      catch(Exception exception)
      {
        isInError=true;
        lastMessage=exception.ToString();
      }
    }
    private void Add()
    {
      try
      {
        if(isInError)return;
        StackElement stackElement2 = codeStack.Pop();
        StackElement stackElement1 = codeStack.Pop();
        StackElement element = new StackElement();
        GenericData genericData1 = null == stackElement1.Symbol ? stackElement1.Value : stackElement1.Symbol.GenericData;
        GenericData genericData2 = null == stackElement2.Symbol ? stackElement2.Value : stackElement2.Symbol.GenericData;
        element.Value = genericData1.Add(genericData2);
        codeStack.Push(element);
      }
      catch(Exception exception)
      {
        isInError=true;
        lastMessage=exception.ToString();
      }
    }
    private void Multiply()
    {
      try
      {
        if(isInError)return;
        StackElement stackElement2 = codeStack.Pop();
        StackElement stackElement1 = codeStack.Pop();
        StackElement element = new StackElement();
        GenericData genericData1 = null == stackElement1.Symbol ? stackElement1.Value : stackElement1.Symbol.GenericData;
        GenericData genericData2 = null == stackElement2.Symbol ? stackElement2.Value : stackElement2.Symbol.GenericData;
        element.Value = genericData2.Multiply(genericData1);
        codeStack.Push(element);
      }
      catch(Exception exception)
      {
        isInError=true;
        lastMessage=exception.ToString();
      }
    }
    private void Divide()
    {
      try
      {
        if(isInError)return;
        StackElement stackElement2 = codeStack.Pop();
        StackElement stackElement1 = codeStack.Pop();
        StackElement element = new StackElement();
        GenericData genericData1 = null == stackElement1.Symbol ? stackElement1.Value : stackElement1.Symbol.GenericData;
        GenericData genericData2 = null == stackElement2.Symbol ? stackElement2.Value : stackElement2.Symbol.GenericData;
        element.Value = genericData1.Divide(genericData2);
        codeStack.Push(element);
      }
      catch(Exception exception)
      {
        isInError=true;
        lastMessage=exception.ToString();
      }
    }
    private void Subtract()
    {
      try
      {
        if(isInError)return;
        StackElement stackElement2 = codeStack.Pop();
        StackElement stackElement1 = codeStack.Pop();
        StackElement element = new StackElement();
        GenericData genericData1 = null == stackElement1.Symbol ? stackElement1.Value : stackElement1.Symbol.GenericData;
        GenericData genericData2 = null == stackElement2.Symbol ? stackElement2.Value : stackElement2.Symbol.GenericData;
        element.Value = genericData1.Subtract(genericData2);
        codeStack.Push(element);
      }
      catch(Exception exception)
      {
        isInError=true;
        lastMessage=exception.ToString();
      }
    }
    public void DirectiveClearModified()
    {
      try
      {
        if(isInError)return;
        symbolTable.ClearModified();
      }
      catch(Exception exception)
      {
        isInError=true;
        lastMessage=exception.ToString();
      }
    }
    public void Assign()
    {
      try
      {
        if(isInError)return;
        StackElement sourceElement = codeStack.Pop();
        StackElement destinationElement = codeStack.Pop();
        GenericData sourceData = (null == sourceElement.Symbol ? sourceElement.Value : sourceElement.Symbol.GenericData);
        if (null != destinationElement.Symbol)
        {
          destinationElement.Symbol.GenericData = GenericData.Clone(sourceData);
          destinationElement.Symbol.IsModified=true;
        }
        else
        {
          destinationElement.Value = new GenericData();
          destinationElement.Value.Data = GenericData.Clone(sourceData);
          destinationElement.Value.IsModified=true;
        }
      }
      catch(Exception exception)
      {
        isInError=true;
        lastMessage=exception.ToString();
        logger.ErrorFormat("Exception:{0}",exception);
      }
    }
    public void VariableAccess()
    {
      try
      {
        if(isInError)return;
        String variableType = binaryReader.ReadString();
        String variableName = binaryReader.ReadString();
        Symbol symbol = null;
        if (!symbolTable.ContainsKey(variableName))  // All symbols must be present in the symbol table.  This can be done by direct injection into the symbol table or through code using a DECLARE
        {
          isInError=true;
          lastMessage = String.Format("The symbol '{0}' must be declared prior to use.",variableName);
          //symbol = new Symbol();
          //GenericData genericData = new GenericData();
          //symbol.SymbolName = variableName;
          //symbol.GenericData = genericData;
          //symbol.GenericData.SetNull();
          //symbolTable.Add(variableName, symbol);
        }
        else 
        {
          symbol = symbolTable[variableName];
          StackElement stackElement = new StackElement();
          stackElement.Symbol = symbol;
          codeStack.Push(stackElement);
        }
      }
      catch(Exception exception)
      {
        isInError=true;
        lastMessage=exception.ToString();
      }
    }
    public void Push()
    {
      try
      {
        if(isInError)return;
        String variableType = binaryReader.ReadString();
        String variableValue = binaryReader.ReadString();
        StackElement stackElement = new StackElement();
        stackElement.Value = new GenericData();
        if(variableType.Equals("System.Nullable"))stackElement.Value.SetData(null, variableType);
        else stackElement.Value.SetData(variableValue, variableType);
        codeStack.Push(stackElement);
      }
      catch(Exception exception)
      {
        isInError=true;
        lastMessage=exception.ToString();
      }
    }
// ******************************************************************************************************************************************************************************************************************************
// ********************************************************************************************************** F U N C T I O N S *************************************************************************************************
// ******************************************************************************************************************************************************************************************************************************
    public void PerformABS(StackElement[] stackElements)
    {
      try
      {
        if(isInError)return;
        if (1 != stackElements.Length) return;
        StackElement element = new StackElement();
        GenericData param1 = null;
        if (null == stackElements[0].Value) param1 = stackElements[0].Symbol.GenericData;
        else param1 = stackElements[0].Value;
        element.Value = param1.Abs();
        codeStack.Push(element);
      }
      catch(Exception exception)
      {
        isInError=true;
        lastMessage=exception.ToString();
      }
    }
    public void PerformPOW(StackElement[] stackElements)
    {
      try
      {
        if(isInError)return;
        if (2 != stackElements.Length) return;
        StackElement element = new StackElement();
        GenericData param1 = null;
        GenericData param2 = null;

        if (null == stackElements[0].Value) param1 = stackElements[0].Symbol.GenericData;
        else param1 = stackElements[0].Value;
        if (null == stackElements[1].Value) param2 = stackElements[1].Symbol.GenericData;
        else param2 = stackElements[1].Value;
        GenericData genericData = new GenericData();
        genericData.SetData(Math.Pow(param1.Get<double>(), param2.Get<double>()));
        element.Value = genericData;
        codeStack.Push(element);
      }
      catch(Exception exception)
      {
        isInError=true;
        lastMessage=exception.ToString();
      }
    }
    public void PerformSQRT(StackElement[] stackElements)
    {
      try
      {
        if(isInError)return;
        if (1 != stackElements.Length) return;
        StackElement element = new StackElement();
        GenericData param1 = null;

        if (null == stackElements[0].Value) param1 = stackElements[0].Symbol.GenericData;
        else param1 = stackElements[0].Value;
        GenericData genericData = new GenericData();
        genericData.SetData(Math.Sqrt(param1.Get<double>()));
        element.Value = genericData;
        codeStack.Push(element);
      }
      catch(Exception exception)
      {
        isInError=true;
        lastMessage=exception.ToString();
      }
    }
    //public void PerformBDP(StackElement[] stackElements)
    //{
    //  try
    //  {
    //    if(isInError)return;
    //    if (3 != stackElements.Length) return;
    //    CreateConnection();
    //    IEnumerable<SecurityReferenceData> securityReferenceDataEnum = null;
    //    StackElement element = new StackElement();
    //    GenericData genericData = new GenericData();
    //    GenericData param1 = null;
    //    GenericData param2 = null;
    //    GenericData param3 = null;

    //    if (null == stackElements[0].Value) param1 = stackElements[0].Symbol.GenericData;
    //    else param1 = stackElements[0].Value;
    //    if (null == stackElements[1].Value) param2 = stackElements[1].Symbol.GenericData;
    //    else param2 = stackElements[1].Value;
    //    if (null == stackElements[2].Value) param3 = stackElements[2].Symbol.GenericData;
    //    else param3 = stackElements[2].Value;
    //    if (!bloombergAPIService.LoggedIn)
    //    {
    //      genericData.Data = null;
    //      return;
    //    }
    //    securityReferenceDataEnum = bloombergAPIService.ProcessBloombergAPIRequest(param2.Get<String>(), param1.Get<String>(), param3.Get<String>(), null, null, null, null);
    //    String value = BloombergAPIService.GetFirstValue(securityReferenceDataEnum);
    //    genericData.Data = value;
    //    element.Value = genericData;
    //    codeStack.Push(element);
    //  }
    //  catch(Exception exception)
    //  {
    //    isInError=true;
    //    lastMessage=exception.ToString();
    //  }
    //}
    public void PerformConvert(StackElement[] stackElements)
    {
      try
      {
        if(isInError)return;
        if (2 != stackElements.Length) return;
        StackElement element = new StackElement();
        GenericData genericData = null;
        GenericData param1 = null;
        GenericData param2 = null;

        if (null == stackElements[0].Value) param1 = stackElements[0].Symbol.GenericData;
        else param1 = stackElements[0].Value;
        if (null == stackElements[1].Value) param2 = stackElements[1].Symbol.GenericData;
        else param2 = stackElements[1].Value;
        genericData=GenericData.PerformConvert(param1,param2);
        element.Value = genericData;
        codeStack.Push(element);
      }
      catch(Exception exception)
      {
        isInError=true;
        lastMessage=exception.ToString();
      }
    }
    public void PerformSubstring(StackElement[] stackElements)
    {
      try
      {
        if(isInError)return;
        if (3 != stackElements.Length) return;
        StackElement result = new StackElement();
        GenericData resultData = null;
        GenericData argumentData = null;
        GenericData startIndexData = null;
        GenericData lengthData = null;

        if (null == stackElements[0].Value) argumentData = stackElements[0].Symbol.GenericData;
        else argumentData = stackElements[0].Value;

        if (null == stackElements[1].Value) startIndexData = stackElements[1].Symbol.GenericData;
        else startIndexData = stackElements[1].Value;

        if (null == stackElements[2].Value) lengthData = stackElements[2].Symbol.GenericData;
        else lengthData = stackElements[2].Value;

        resultData=argumentData.Substring(startIndexData,lengthData);
        result.Value = resultData;
        codeStack.Push(result);
      }
      catch(Exception exception)
      {
        isInError=true;
        lastMessage=exception.ToString();
      }
    }
    public void PerformGetPrice(StackElement[] stackElements)
    {
      try
      {
        if(isInError)return;
        if (3 != stackElements.Length) return;
        StackElement result = new StackElement();
        GenericData resultData = null;
        GenericData symbolData = null;
				GenericData dateData = null;
			  GenericData opData = null;

        if (null == stackElements[0].Value) symbolData = stackElements[0].Symbol.GenericData;
        else symbolData = stackElements[0].Value;

        if (null == stackElements[1].Value) dateData = stackElements[1].Symbol.GenericData;
        else dateData = stackElements[1].Value;

				if (null == stackElements[2].Value) opData = stackElements[2].Symbol.GenericData;
				else opData = stackElements[2].Value;

        resultData=symbolData.GetPrice(dateData,opData);
        result.Value = resultData;
        codeStack.Push(result);
      }
      catch(Exception exception)
      {
        isInError=true;
        lastMessage=exception.ToString();
      }
    }
    public void PerformTrim(StackElement[] stackElements)
    {
      try
      {
        if(isInError)return;
        if (1 != stackElements.Length) return;
        StackElement result = new StackElement();
        GenericData resultData = null;
        GenericData argumentData = null;

        if (null == stackElements[0].Value) argumentData = stackElements[0].Symbol.GenericData;
        else argumentData = stackElements[0].Value;

        resultData=argumentData.Trim();
        result.Value = resultData;
        codeStack.Push(result);
      }
      catch(Exception exception)
      {
        isInError=true;
        lastMessage=exception.ToString();
      }
    }
    public void PerformUpper(StackElement[] stackElements)
    {
      try
      {
        if(isInError)return;
        if (1 != stackElements.Length) return;
        StackElement result = new StackElement();
        GenericData resultData = null;
        GenericData argumentData = null;
        if (null == stackElements[0].Value) argumentData = stackElements[0].Symbol.GenericData;
        else argumentData = stackElements[0].Value;
        resultData = argumentData.Upper();
        result.Value = resultData;
        codeStack.Push(result);
      }
      catch(Exception exception)
      {
        isInError=true;
        lastMessage=exception.ToString();
      }
    }
    public void PerformLower(StackElement[] stackElements)
    {
      try
      {
        if(isInError)return;
        if (1 != stackElements.Length) return;
        StackElement result = new StackElement();
        GenericData resultData = null;
        GenericData argumentData = null;

        if (null == stackElements[0].Value) argumentData = stackElements[0].Symbol.GenericData;
        else argumentData = stackElements[0].Value;
        resultData = argumentData.Lower();
        result.Value = resultData;
        codeStack.Push(result);
      }
      catch(Exception exception)
      {
        isInError=true;
        lastMessage=exception.ToString();
      }
    }
    public void PerformIn(StackElement[] stackElements)
    {
      try
      {
        if(isInError)return;
        StackElement element = new StackElement();
        GenericData genericData = new GenericData();
        StackElement argument= codeStack.Pop();
        GenericData argumentData = null;
        bool evaluationResult = false;
        if (null != argument.Value) argumentData = argument.Value;
        else argumentData = argument.Symbol.GenericData;
        for (int index = 0; index < stackElements.Length; index++)
        {
          GenericData stackData = null;
          if (null == stackElements[index].Value) stackData = stackElements[index].Symbol.GenericData;
          else stackData = stackElements[index].Value;
          GenericData result = stackData.EqualEqual(argumentData);
          if (result.IsNull()) continue;
          if(result.Get<bool>().Equals(true))
          {
            evaluationResult = true;
            break;
          }
        }
        genericData.SetData(evaluationResult);
        element.Value = genericData;
        codeStack.Push(element);
      }
      catch(Exception exception)
      {
        isInError=true;
        lastMessage=exception.ToString();
      }
    }
    public void PerformIsNull(StackElement[] stackElements)
    {
      try
      {
        if(isInError)return;
        StackElement element = new StackElement();
        GenericData stackData = null;
        for (int index = 0; index < stackElements.Length; index++)
        {
          if (null == stackElements[index].Value) stackData = stackElements[index].Symbol.GenericData;
          else stackData = stackElements[index].Value;
          if (!stackData.IsNull()) break;
        }
        element.Value = stackData;
        codeStack.Push(element);
      }
      catch(Exception exception)
      {
        isInError=true;
        lastMessage=exception.ToString();
      }
    }
    public void PerformLike(StackElement[] stackElements)
    {
      try
      {
        if(isInError)return;
        StackElement element = new StackElement();
        StackElement stackElement= codeStack.Pop();
        GenericData stackData = null;
        GenericData genericData = null;
        GenericData param1 = null;
        if (1 != stackElements.Length) return;
        if (null != stackElement.Value) stackData = stackElement.Value;
        else stackData = stackElement.Symbol.GenericData;
        if (null == stackElements[0].Value) param1 = stackElements[0].Symbol.GenericData;
        else param1 = stackElements[0].Value;
        if (null == stackData) { genericData = new GenericData(); genericData.SetData(false); }
        else genericData=stackData.Like(param1);
        element.Value = genericData;
        codeStack.Push(element);
      }
      catch(Exception exception)
      {
        isInError=true;
        lastMessage=exception.ToString();
      }
    }
  }
}
