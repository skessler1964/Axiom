using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

// FileName : Parser.cs
// Author : Sean Kessler

namespace Axiom.Interpreter
{
  public class Parser : Emitter
  {
    public enum ParserSymbols { undefined2, directive_clear_modified2, declare2, do2, defaddr2, goto2, add2, subtract2, multiply2, divide2, assign2, negate2, less2, lessequal2, greater2, greaterequal2, equalequal2, notequal2, variableaccess2, call2, push2, not2, codeend2, oror2, andand2,noop2 };
    public enum ErrorCodes{DivideByZero};
    private SymbolTable symbolTable;
    private ParseSymbols directiveSymbols = new ParseSymbols();
    private ParseSymbols assignmentStatementSymbols = new ParseSymbols();
    private ParseSymbols statementSymbols = new ParseSymbols();
    private ParseSymbols simpleExpressionSymbols = new ParseSymbols();
    private ParseSymbols mathSymbols = new ParseSymbols();
    private ParseSymbols termSymbols = new ParseSymbols();
    private ParseSymbols factorSymbols = new ParseSymbols();  
    private ParseSymbols addSymbols = new ParseSymbols();
    private ParseSymbols signSymbols = new ParseSymbols();
    private ParseSymbols expressionSymbols = new ParseSymbols();
    private ParseSymbols parseSymbols = new ParseSymbols();
    private ParseSymbols relationSymbols = new ParseSymbols();
    private ParseSymbols logicalSymbols = new ParseSymbols();
    private ParseSymbols declarationSymbols = new ParseSymbols();
    private ParseSymbols mathFunctionSymbols = new ParseSymbols();
    private ParseSymbols stringManipulationSymbols = new ParseSymbols();
    private Scanner.ScanSymbols currentSymbolType;
    private double numeralValue;
    private String stringValue;
    private bool strictMode = false;         // if StrictMode is true then the parser will return an error if a variable declaration is missing
    private bool strictStatementMode=false;  //  is StrictStatementMode is true then the parser will return an error if there is not at least one statement processed.

    public Parser(BinaryReader binaryReader, BinaryWriter binaryWriter, SymbolTable symbolTable)
      : base(binaryReader, binaryWriter)
    {
      this.symbolTable = symbolTable;
      currentSymbolType = Scanner.ScanSymbols.unknown1;
      CreateDeclarationSymbols();
      CreateAssignmentStatementSymbols();
      CreateStatementSymbols();
      CreateSimpleExpressionSymbols();
      CreateMathSymbols();
      CreateTermSymbols();
      CreateFactorSymbols();
      CreateAddSymbols();
      CreateSignSymbols();
      CreateExpressionSymbols();
      CreateRelationSymbols();
      CreateFunctionSymbols();
      CreateLogicalSymbols();
      CreateMathFunctionSymbols();
      CreateStringManipulationSymbols();
      IsInError = false;
      LineNumber = 0;
      LastLineNumber = 0;
      SymbolNumber = -1;
      LastSymbolNumber = -1;
      StatementNumber = 0;
      LastStatementNumber = 0;
      LastMessage = "";
    }
    public bool Parse(bool includeCodeEnd=true)
    {
      NextSymbol();
      InsertSymbols(new ParseSymbol(Scanner.ScanSymbols.endtext1));
      InsertSymbols(new ParseSymbol(Scanner.ScanSymbols.directive_clear_modified1));
      while (!(Scanner.ScanSymbols.endtext1.Equals(currentSymbolType)) && !IsInError) Statement();
      RemoveSymbols(new ParseSymbol(Scanner.ScanSymbols.endtext1));
      RemoveSymbols(new ParseSymbol(Scanner.ScanSymbols.directive_clear_modified1));
      if(includeCodeEnd)Emit(Parser.ParserSymbols.codeend2);
      if(StrictStatementMode && 0==StatementNumber)SyntaxError("The input document does not contain any valid statements.");
      if (IsInError) return false;
      if(Debug)
      {
        if (parseSymbols.Count != 0) Console.WriteLine("ParseSymbols is not empty.");
        else Console.WriteLine("ParseSymbols, all symbols cleared from stack.");
      }
      return !IsInError;
    }
    public bool StrictMode
    {
      get { return strictMode; }
      set{strictMode=value;}
    }
    public bool StrictStatementMode
    {
      get { return strictStatementMode; }
      set{strictStatementMode=value;}
    }
    public int ParseSymbolsCount
    {
      get{return parseSymbols.Count;}
    }
    public void Statement()
    {
      InsertSymbols(new ParseSymbol(Scanner.ScanSymbols.semicolon1));
      if(SymbolIn(new ParseSymbol(Scanner.ScanSymbols.directive_clear_modified1)))
      {
        DirectiveClearModified();
      }
      else if(SymbolIn(new ParseSymbol(Scanner.ScanSymbols.declare1)))
      {
        DeclarationStatement();
      }
      else if (SymbolIn(new ParseSymbol(Scanner.ScanSymbols.label1)))
      {
        SyntaxError();
        // LabelStatement();
      }
      else if (SymbolIn(new ParseSymbol(Scanner.ScanSymbols.literal1)))
      {
        ParseLiteral();
      }
      else if (Scanner.ScanSymbols.variable1.Equals(currentSymbolType))
      {
        bool assignmentStatement = PeekSymbol(new ParseSymbol(Scanner.ScanSymbols.equal1));
        VariableAccess();
        if(assignmentStatement)
        {
          AssignmentStatement();
          StatementNumber++;
        }
      }
      else if (Scanner.ScanSymbols.if1.Equals(currentSymbolType))
      {
        IfStatement();
        StatementNumber++;
      }
      else if (SymbolIn(termSymbols))
      {
        Term();
      }
      else SyntaxCheck();

      InsertSymbols(new ParseSymbol(Scanner.ScanSymbols.endtext1));
      InsertSymbols(statementSymbols);
      if (SymbolIn(new ParseSymbol(Scanner.ScanSymbols.semicolon1)))
      {
        Expect(Scanner.ScanSymbols.semicolon1);
      }
      RemoveSymbols(statementSymbols);
      RemoveSymbols(new ParseSymbol(Scanner.ScanSymbols.semicolon1));
      RemoveSymbols(new ParseSymbol(Scanner.ScanSymbols.endtext1));
      return;
    }
    public void DeclarationStatement()
    {
      bool moreDeclarations = true;
      InsertSymbols(new ParseSymbol(Scanner.ScanSymbols.variable1));
      Expect(Scanner.ScanSymbols.declare1);
      SyntaxCheck();
      while(moreDeclarations)
      {
        InsertSymbols(new ParseSymbol(Scanner.ScanSymbols.comma1));
        Expect(Scanner.ScanSymbols.variable1);
        Emit(Parser.ParserSymbols.declare2, stringValue);
        RemoveSymbols(new ParseSymbol(Scanner.ScanSymbols.comma1));
        if (SymbolIn(new ParseSymbol(Scanner.ScanSymbols.comma1)))
        {
          Expect(Scanner.ScanSymbols.comma1);
          SyntaxCheck();
        }
        else moreDeclarations = false;
      }
      RemoveSymbols(new ParseSymbol(Scanner.ScanSymbols.variable1));
    }
    public void DirectiveClearModified()
    {
      Emit(Parser.ParserSymbols.directive_clear_modified2);
      Expect(Scanner.ScanSymbols.directive_clear_modified1);
    }
    public void ParseLiteral()
    {
      Emit(Parser.ParserSymbols.push2, stringValue);
      Expect(Scanner.ScanSymbols.literal1);
    }
    public void IfStatement()
    {
      Stack<Parser.ParserSymbols> logicalOperatorStack = new Stack<Parser.ParserSymbols>();
      InsertSymbols(expressionSymbols);
      InsertSymbols(directiveSymbols);
      Expect(Scanner.ScanSymbols.if1);
      Expect(Scanner.ScanSymbols.leftparen1);
      RemoveSymbols(expressionSymbols);
      RemoveSymbols(directiveSymbols);
      if (IsInError) return;
      while (!IsInError)
      {
        bool inShortFunction = false;
        InsertSymbols(expressionSymbols);
        if (PeekSymbolIn(directiveSymbols)) inShortFunction = true;
        InsertSymbols(new ParseSymbol(Scanner.ScanSymbols.rightparen1));
        InsertSymbols(logicalSymbols);
        Expression();
        Relation();
        InsertSymbols(relationSymbols);
        SyntaxCheck();
        if (IsInError) return;
        RemoveSymbols(relationSymbols);
        RemoveSymbols(logicalSymbols);
        RemoveSymbols(new ParseSymbol(Scanner.ScanSymbols.rightparen1));
        if (inShortFunction||SymbolIn(logicalSymbols))
        {
          if(SymbolIn(new ParseSymbol(Scanner.ScanSymbols.oror1)))
          {
            logicalOperatorStack.Push(Parser.ParserSymbols.oror2);
            InsertSymbols(expressionSymbols);
            InsertSymbols(directiveSymbols);
            Expect(currentSymbolType);
            RemoveSymbols(expressionSymbols);
            RemoveSymbols(expressionSymbols);  // yes do this twice
            RemoveSymbols(directiveSymbols);
            continue;
          }
          else if(SymbolIn(new ParseSymbol(Scanner.ScanSymbols.andand1)))
          {
            logicalOperatorStack.Push(Parser.ParserSymbols.andand2);
            InsertSymbols(expressionSymbols);
            InsertSymbols(directiveSymbols);
            Expect(currentSymbolType);
            RemoveSymbols(expressionSymbols);
            RemoveSymbols(expressionSymbols);  // yes do this twice
            RemoveSymbols(directiveSymbols);
            continue;
          }
        }
        ParseSymbol relationSymbol = new ParseSymbol(currentSymbolType);
        InsertSymbols(new ParseSymbol(Scanner.ScanSymbols.then1));         // going to handle if() then stmt; else stmt;
        InsertSymbols(new ParseSymbol(Scanner.ScanSymbols.leftcurly1));  // going to handle if(){stmt;stmt;}else{stmt;stmt;}
        InsertSymbols(logicalSymbols);
        InsertSymbols(directiveSymbols);
        Expect(currentSymbolType);  
        RemoveSymbols(expressionSymbols);
        RemoveSymbols(directiveSymbols);
        if (!inShortFunction)
        {
          InsertSymbols(new ParseSymbol(Scanner.ScanSymbols.rightparen1));
          if (SymbolIn(directiveSymbols))
          {
            InsertSymbols(relationSymbols);
            Directive();
            Relation();
            InsertSymbols(relationSymbols);
          }
          else
          {
            Statement();
          }
          RemoveSymbols(new ParseSymbol(Scanner.ScanSymbols.rightparen1));
          if (SymbolIn(new ParseSymbol(Scanner.ScanSymbols.rightparen1))) Expect(Scanner.ScanSymbols.rightparen1);
          if (relationSymbol.Equals(new ParseSymbol(Scanner.ScanSymbols.notequal1)))
          {
            Emit(Parser.ParserSymbols.notequal2);
          }
          else if (relationSymbol.Equals(new ParseSymbol(Scanner.ScanSymbols.equalequal1)))
          {
            Emit(Parser.ParserSymbols.equalequal2);
          }
          else if (relationSymbol.Equals(new ParseSymbol(Scanner.ScanSymbols.less1)))
          {
            Emit(Parser.ParserSymbols.less2);
          }
          else if (relationSymbol.Equals(new ParseSymbol(Scanner.ScanSymbols.lessequal1)))
          {
            Emit(Parser.ParserSymbols.lessequal2);
          }
          else if (relationSymbol.Equals(new ParseSymbol(Scanner.ScanSymbols.greater1)))
          {
            Emit(Parser.ParserSymbols.greater2);
          }
          else if (relationSymbol.Equals(new ParseSymbol(Scanner.ScanSymbols.greaterequal1)))
          {
            Emit(Parser.ParserSymbols.greaterequal2);
          }
        }
        if (0 != logicalOperatorStack.Count) Emit(logicalOperatorStack.Pop());
        if(SymbolIn(new ParseSymbol(Scanner.ScanSymbols.oror1)))
        {
          logicalOperatorStack.Push(Parser.ParserSymbols.oror2);
          InsertSymbols(expressionSymbols);
          InsertSymbols(directiveSymbols);
          Expect(currentSymbolType);
          RemoveSymbols(expressionSymbols);
          RemoveSymbols(directiveSymbols);
        }
        else if(SymbolIn(new ParseSymbol(Scanner.ScanSymbols.andand1)))
        {
          logicalOperatorStack.Push(Parser.ParserSymbols.andand2);
          InsertSymbols(expressionSymbols);
          InsertSymbols(directiveSymbols);
          Expect(currentSymbolType);
          RemoveSymbols(expressionSymbols);
          RemoveSymbols(directiveSymbols);
        }
        RemoveSymbols(logicalSymbols);
        RemoveSymbols(new ParseSymbol(Scanner.ScanSymbols.then1));
        RemoveSymbols(new ParseSymbol(Scanner.ScanSymbols.leftcurly1));
        RemoveSymbols(expressionSymbols);
        if (!SymbolIn(expressionSymbols)&&!SymbolIn(directiveSymbols)) break;
      } // while true
      if (0 != logicalOperatorStack.Count) Emit(logicalOperatorStack.Pop());
      HandleCompoundStatements();
    }
    public void HandleCompoundStatements()
    {
      bool containsElse=true;
      long codePointerFalseInstructionAddress=0;
      long codePointerFalseGotoTarget=0;
      long codePointerTrueInstructionAddress=0;
      long codePointerTrueGotoTarget=0;
      long codePointerTerminalAddress=0;

      codePointerFalseInstructionAddress = CodePointer();                  // get the address of where I am going to write the next instruction
      Emit(Parser.ParserSymbols.defaddr2,0L);                              // write the next instruction with a zero.  It will be filled in later with a valid address.  This will be jump to condition:false   
      InsertSymbols(statementSymbols);
      if(SymbolIn(new ParseSymbol(Scanner.ScanSymbols.then1)))
      {
        Expect(Scanner.ScanSymbols.then1);
        InsertSymbols(new ParseSymbol(Scanner.ScanSymbols.else1));
        Statement();                                                         // after this statement we need to jump to the end
      }
      else if(SymbolIn(new ParseSymbol(Scanner.ScanSymbols.leftcurly1)))
      {
        InsertSymbols(new ParseSymbol(Scanner.ScanSymbols.rightcurly1));
        Expect(Scanner.ScanSymbols.leftcurly1);
        while (!IsInError)
        {
          Statement();
          if (SymbolIn(new ParseSymbol(Scanner.ScanSymbols.rightcurly1)))
          {
            InsertSymbols(new ParseSymbol(Scanner.ScanSymbols.else1));
            Expect(Scanner.ScanSymbols.rightcurly1);
            RemoveSymbols(new ParseSymbol(Scanner.ScanSymbols.else1));
            break;
          }
        }
        RemoveSymbols(new ParseSymbol(Scanner.ScanSymbols.rightcurly1));
      }
      else SyntaxError();
      codePointerTrueInstructionAddress = CodePointer();
      Emit(Parser.ParserSymbols.goto2,0L);
      RemoveSymbols(new ParseSymbol(Scanner.ScanSymbols.else1));
      codePointerFalseGotoTarget = CodePointer();                                  // This is where instructions start for the false condition
      Seek(codePointerFalseInstructionAddress);
      Emit(Parser.ParserSymbols.defaddr2,codePointerFalseGotoTarget);
      Seek(codePointerFalseGotoTarget);
      if(!SymbolIn(new ParseSymbol(Scanner.ScanSymbols.else1)))containsElse=false;
      if(containsElse)
      {
        InsertSymbols(new ParseSymbol(Scanner.ScanSymbols.leftcurly1));
        Expect(Scanner.ScanSymbols.else1);
        RemoveSymbols(new ParseSymbol(Scanner.ScanSymbols.leftcurly1));
        if (SymbolIn(new ParseSymbol(Scanner.ScanSymbols.leftcurly1)))
        {
          InsertSymbols(new ParseSymbol(Scanner.ScanSymbols.rightcurly1));
          InsertSymbols(new ParseSymbol(Scanner.ScanSymbols.leftcurly1));
          InsertSymbols(statementSymbols);
          Expect(Scanner.ScanSymbols.leftcurly1);
          RemoveSymbols(statementSymbols);
          RemoveSymbols(new ParseSymbol(Scanner.ScanSymbols.leftcurly1));
          while (!IsInError)
          {
            Statement();
            if (SymbolIn(new ParseSymbol(Scanner.ScanSymbols.rightcurly1)))
            {
              Expect(Scanner.ScanSymbols.rightcurly1);
              break;
            }
          }
          RemoveSymbols(new ParseSymbol(Scanner.ScanSymbols.rightcurly1));
        }
        else
        {
          Statement();
        }
      }
      codePointerTrueGotoTarget=CodePointer();
      Emit(Parser.ParserSymbols.noop2);                                // this is the end
      codePointerTerminalAddress = CodePointer();
      Seek(codePointerTrueInstructionAddress);
      Emit(Parser.ParserSymbols.goto2,codePointerTrueGotoTarget);
      Seek(codePointerTerminalAddress);
      RemoveSymbols(statementSymbols);
    }
    public void Relation()
    {
      Stack<Parser.ParserSymbols> logicalOperatorStack = new Stack<Parser.ParserSymbols>();
      if (!SymbolIn(relationSymbols)) return;
      while (!IsInError)
      {
        if(SymbolIn(new ParseSymbol(Scanner.ScanSymbols.equalequal1)))
        {
          logicalOperatorStack.Push(Parser.ParserSymbols.equalequal2);
          InsertSymbols(expressionSymbols);
          InsertSymbols(directiveSymbols);
          Expect(currentSymbolType);
          RemoveSymbols(expressionSymbols);
          RemoveSymbols(directiveSymbols);
          continue;
        }
        else if(SymbolIn(new ParseSymbol(Scanner.ScanSymbols.notequal1)))
        {
          logicalOperatorStack.Push(Parser.ParserSymbols.notequal2);
          InsertSymbols(expressionSymbols);
          InsertSymbols(directiveSymbols);
          Expect(currentSymbolType);
          RemoveSymbols(expressionSymbols);
          RemoveSymbols(directiveSymbols);
          continue;
        }
        else if(SymbolIn(new ParseSymbol(Scanner.ScanSymbols.less1)))
        {
          logicalOperatorStack.Push(Parser.ParserSymbols.less2);
          InsertSymbols(expressionSymbols);
          InsertSymbols(directiveSymbols);
          Expect(currentSymbolType);
          RemoveSymbols(expressionSymbols);
          RemoveSymbols(directiveSymbols);
          continue;
        }
        else if(SymbolIn(new ParseSymbol(Scanner.ScanSymbols.lessequal1)))
        {
          logicalOperatorStack.Push(Parser.ParserSymbols.lessequal2);
          InsertSymbols(expressionSymbols);
          InsertSymbols(directiveSymbols);
          Expect(currentSymbolType);
          RemoveSymbols(expressionSymbols);
          RemoveSymbols(directiveSymbols);
          continue;
        }
        else if(SymbolIn(new ParseSymbol(Scanner.ScanSymbols.greater1)))
        {
          logicalOperatorStack.Push(Parser.ParserSymbols.greater2);
          InsertSymbols(expressionSymbols);
          InsertSymbols(directiveSymbols);
          Expect(currentSymbolType);
          RemoveSymbols(expressionSymbols);
          RemoveSymbols(directiveSymbols);
          continue;
        }
        else if(SymbolIn(new ParseSymbol(Scanner.ScanSymbols.greaterequal1)))
        {
          logicalOperatorStack.Push(Parser.ParserSymbols.greaterequal2);
          InsertSymbols(expressionSymbols);
          InsertSymbols(directiveSymbols);
          Expect(currentSymbolType);
          RemoveSymbols(expressionSymbols);
          RemoveSymbols(directiveSymbols);
          continue;
        }
        else if(SymbolIn(new ParseSymbol(Scanner.ScanSymbols.oror1)))
        {
          logicalOperatorStack.Push(Parser.ParserSymbols.oror2);
          InsertSymbols(expressionSymbols);
          InsertSymbols(directiveSymbols);
          Expect(currentSymbolType);
          RemoveSymbols(expressionSymbols);
          RemoveSymbols(directiveSymbols);
          continue;
        }
        else if(SymbolIn(new ParseSymbol(Scanner.ScanSymbols.andand1)))
        {
          logicalOperatorStack.Push(Parser.ParserSymbols.andand2);
          InsertSymbols(expressionSymbols);
          InsertSymbols(directiveSymbols);
          Expect(currentSymbolType);
          RemoveSymbols(expressionSymbols);
          RemoveSymbols(directiveSymbols);
          continue;
        }
        else if(SymbolIn(new ParseSymbol(Scanner.ScanSymbols.less1)))
        {
          logicalOperatorStack.Push(Parser.ParserSymbols.less2);
          InsertSymbols(expressionSymbols);
          InsertSymbols(directiveSymbols);
          Expect(currentSymbolType);
          RemoveSymbols(expressionSymbols);
          RemoveSymbols(directiveSymbols);
          continue;
        }
        InsertSymbols(new ParseSymbol(Scanner.ScanSymbols.rightparen1));
        Term();
        RemoveSymbols(new ParseSymbol(Scanner.ScanSymbols.rightparen1));
        ParseSymbol relationSymbol = new ParseSymbol(currentSymbolType);
        if (0 != logicalOperatorStack.Count) Emit(logicalOperatorStack.Pop());
        if (!SymbolIn(relationSymbols)&&!SymbolIn(logicalSymbols)) break;
      } // while true
      if (0 != logicalOperatorStack.Count) Emit(logicalOperatorStack.Pop());
    }
    public void AssignmentStatement()
    {
      InsertSymbols(expressionSymbols);
      InsertSymbols(directiveSymbols);
      Expect(Scanner.ScanSymbols.equal1);
      if (IsInError) return;
      RemoveSymbols(directiveSymbols);
      RemoveSymbols(expressionSymbols);
      if (SymbolIn(expressionSymbols))
      {
        Expression();
        Emit(Parser.ParserSymbols.assign2);
      }
      else if (SymbolIn(directiveSymbols))
      {
        RemoveSymbols(directiveSymbols);
        RemoveSymbols(expressionSymbols);
        Directive();
        if (SymbolIn(expressionSymbols))    
        {
          while (SymbolIn(expressionSymbols))
          {
            InsertSymbols(expressionSymbols);
            InsertSymbols(directiveSymbols);
            Expression();
            RemoveSymbols(directiveSymbols);
            RemoveSymbols(expressionSymbols);
          }
        }
        else if (SymbolIn(mathSymbols))   
        {
          InsertSymbols(expressionSymbols);
          InsertSymbols(directiveSymbols);
          SimpleTerm();
          RemoveSymbols(expressionSymbols);
          RemoveSymbols(directiveSymbols);
        }
        Emit(Parser.ParserSymbols.assign2);
      }
      else SyntaxError();
    }
    public void VariableAccess()
    {
      if(StrictMode && !symbolTable.ContainsKey(stringValue))
      {
        SyntaxError(String.Format("Undefined symbol {0}",stringValue));
        NextSymbol();
      }
      else
      {
        Emit(Parser.ParserSymbols.variableaccess2,stringValue);
        NextSymbol();
      }
    }
// *******************************************************************************************************************************************************************************************************************************************************************
// ************************************************************************************************************ B U I L T - I N  F U N C T I O N S  ******************************************************************************************************************
// *******************************************************************************************************************************************************************************************************************************************************************
    public void Directive()
    {
      //if (SymbolIn(new ParseSymbol(Scanner.ScanSymbols.bdp1))) ParseBDP();
      if (SymbolIn(new ParseSymbol(Scanner.ScanSymbols.abs1))) ParseABS();
      else if (SymbolIn(new ParseSymbol(Scanner.ScanSymbols.pow1))) ParsePOW();
      else if (SymbolIn(new ParseSymbol(Scanner.ScanSymbols.sqrt1))) ParseSQRT();
      else if (SymbolIn(new ParseSymbol(Scanner.ScanSymbols.convert1))) ParseConvert();
      else if (SymbolIn(new ParseSymbol(Scanner.ScanSymbols.substring1))) ParseSubstring();
      else if (SymbolIn(new ParseSymbol(Scanner.ScanSymbols.getprice1))) ParseGetPrice();
      else if (SymbolIn(new ParseSymbol(Scanner.ScanSymbols.in1))) ParseIn();
      else if (SymbolIn(new ParseSymbol(Scanner.ScanSymbols.like1))) ParseLike();
      else if (SymbolIn(new ParseSymbol(Scanner.ScanSymbols.isnull1))) ParseIsNull();
      else if (SymbolIn(new ParseSymbol(Scanner.ScanSymbols.trim1))) ParseTrim();
      else if (SymbolIn(new ParseSymbol(Scanner.ScanSymbols.upper1))) ParseUpper();
      else if (SymbolIn(new ParseSymbol(Scanner.ScanSymbols.lower1))) ParseLower();
    }
    public void ParseLike()
    {
      Symbol namedFunction = symbolTable.Find(Scanner.ScanSymbols.like1);
      Expect(Scanner.ScanSymbols.like1);
      InsertSymbols(new ParseSymbol(Scanner.ScanSymbols.literal1));
      InsertSymbols(new ParseSymbol(Scanner.ScanSymbols.variable1));
      if (SymbolIn(new ParseSymbol(Scanner.ScanSymbols.literal1)))
      {
        Emit(Parser.ParserSymbols.push2, stringValue);
        Expect(Scanner.ScanSymbols.literal1);
      }
      else if (SymbolIn(new ParseSymbol(Scanner.ScanSymbols.variable1)))
      {
        VariableAccess();
      }
      else SyntaxCheck();
      RemoveSymbols(new ParseSymbol(Scanner.ScanSymbols.literal1));
      RemoveSymbols(new ParseSymbol(Scanner.ScanSymbols.variable1));
      Emit(Parser.ParserSymbols.call2,namedFunction.SymbolName,1); 
    }
    public void ParseTrim()
    {
      Symbol namedFunction = symbolTable.Find(Scanner.ScanSymbols.trim1);
      InsertSymbols(new ParseSymbol(Scanner.ScanSymbols.leftparen1));
      Expect(Scanner.ScanSymbols.trim1);
      InsertSymbols(new ParseSymbol(Scanner.ScanSymbols.literal1));
      InsertSymbols(new ParseSymbol(Scanner.ScanSymbols.variable1));
      InsertSymbols(directiveSymbols);
      Expect(Scanner.ScanSymbols.leftparen1);
      RemoveSymbols(directiveSymbols);
      RemoveSymbols(new ParseSymbol(Scanner.ScanSymbols.leftparen1));
      InsertSymbols(new ParseSymbol(Scanner.ScanSymbols.rightparen1));
      Expression();
      InsertSymbols(addSymbols);
      Expect(Scanner.ScanSymbols.rightparen1);
      RemoveSymbols(addSymbols);
      RemoveSymbols(new ParseSymbol(Scanner.ScanSymbols.rightparen1));
      RemoveSymbols(new ParseSymbol(Scanner.ScanSymbols.literal1));
      RemoveSymbols(new ParseSymbol(Scanner.ScanSymbols.variable1));
      Emit(Parser.ParserSymbols.call2,namedFunction.SymbolName,1); 
    }
    public void ParseUpper()
    {
      Symbol namedFunction = symbolTable.Find(Scanner.ScanSymbols.upper1);
      InsertSymbols(new ParseSymbol(Scanner.ScanSymbols.leftparen1));
      Expect(Scanner.ScanSymbols.upper1);
      InsertSymbols(new ParseSymbol(Scanner.ScanSymbols.literal1));
      InsertSymbols(new ParseSymbol(Scanner.ScanSymbols.variable1));
      InsertSymbols(directiveSymbols);
      Expect(Scanner.ScanSymbols.leftparen1);
      RemoveSymbols(directiveSymbols);
      RemoveSymbols(new ParseSymbol(Scanner.ScanSymbols.leftparen1));
      InsertSymbols(new ParseSymbol(Scanner.ScanSymbols.rightparen1));
      Expression();
      InsertSymbols(addSymbols);
      Expect(Scanner.ScanSymbols.rightparen1);
      RemoveSymbols(addSymbols);
      RemoveSymbols(new ParseSymbol(Scanner.ScanSymbols.rightparen1));
      RemoveSymbols(new ParseSymbol(Scanner.ScanSymbols.literal1));
      RemoveSymbols(new ParseSymbol(Scanner.ScanSymbols.variable1));
      Emit(Parser.ParserSymbols.call2,namedFunction.SymbolName,1); 
    }
    public void ParseLower()
    {
      Symbol namedFunction = symbolTable.Find(Scanner.ScanSymbols.lower1);
      InsertSymbols(new ParseSymbol(Scanner.ScanSymbols.leftparen1));
      Expect(Scanner.ScanSymbols.lower1);
      InsertSymbols(new ParseSymbol(Scanner.ScanSymbols.literal1));
      InsertSymbols(new ParseSymbol(Scanner.ScanSymbols.variable1));
      InsertSymbols(directiveSymbols);
      Expect(Scanner.ScanSymbols.leftparen1);
      RemoveSymbols(directiveSymbols);
      RemoveSymbols(new ParseSymbol(Scanner.ScanSymbols.leftparen1));
      InsertSymbols(new ParseSymbol(Scanner.ScanSymbols.rightparen1));
      Expression();
      InsertSymbols(addSymbols);
      Expect(Scanner.ScanSymbols.rightparen1);
      RemoveSymbols(addSymbols);
      RemoveSymbols(new ParseSymbol(Scanner.ScanSymbols.rightparen1));
      RemoveSymbols(new ParseSymbol(Scanner.ScanSymbols.literal1));
      RemoveSymbols(new ParseSymbol(Scanner.ScanSymbols.variable1));
      Emit(Parser.ParserSymbols.call2,namedFunction.SymbolName,1); 
    }
// SUBSTRING("",START_INDEX,LENGTH)
    public void ParseSubstring()
    {
      InsertSymbols(new ParseSymbol(Scanner.ScanSymbols.leftparen1));
      Expect(Scanner.ScanSymbols.substring1);
      InsertSymbols(new ParseSymbol(Scanner.ScanSymbols.literal1));
      InsertSymbols(new ParseSymbol(Scanner.ScanSymbols.variable1));
      InsertSymbols(directiveSymbols);
      Expect(Scanner.ScanSymbols.leftparen1);
      RemoveSymbols(directiveSymbols);
      RemoveSymbols(new ParseSymbol(Scanner.ScanSymbols.leftparen1));
      RemoveSymbols(new ParseSymbol(Scanner.ScanSymbols.variable1));
      RemoveSymbols(new ParseSymbol(Scanner.ScanSymbols.literal1));
      InsertSymbols(new ParseSymbol(Scanner.ScanSymbols.comma1));
      Expression();
      RemoveSymbols(new ParseSymbol(Scanner.ScanSymbols.comma1));
      InsertSymbols(new ParseSymbol(Scanner.ScanSymbols.numeral1));
      InsertSymbols(new ParseSymbol(Scanner.ScanSymbols.variable1));
      Expect(Scanner.ScanSymbols.comma1);
      RemoveSymbols(new ParseSymbol(Scanner.ScanSymbols.variable1));
      InsertSymbols(new ParseSymbol(Scanner.ScanSymbols.comma1));
      RemoveSymbols(new ParseSymbol(Scanner.ScanSymbols.numeral1));
      Expression();
      RemoveSymbols(new ParseSymbol(Scanner.ScanSymbols.comma1));
      InsertSymbols(new ParseSymbol(Scanner.ScanSymbols.numeral1));
      InsertSymbols(new ParseSymbol(Scanner.ScanSymbols.variable1));
      Expect(Scanner.ScanSymbols.comma1);
      RemoveSymbols(new ParseSymbol(Scanner.ScanSymbols.numeral1));
      RemoveSymbols(new ParseSymbol(Scanner.ScanSymbols.variable1));
      InsertSymbols(new ParseSymbol(Scanner.ScanSymbols.rightparen1));
      Expression();
      RemoveSymbols(new ParseSymbol(Scanner.ScanSymbols.rightparen1));
      InsertSymbols(addSymbols);
      Expect(Scanner.ScanSymbols.rightparen1);
      RemoveSymbols(addSymbols);
      Symbol namedFunction = symbolTable.Find(Scanner.ScanSymbols.substring1);
      Emit(Parser.ParserSymbols.call2,namedFunction.SymbolName,3); 
    }
// GETPRICE("MIDD",'07-13-2021','OPEN')	  'OPEN'|'HIGH'|'LOW'|'CLOSE'
    public void ParseGetPrice()
    {
      InsertSymbols(new ParseSymbol(Scanner.ScanSymbols.leftparen1));
      Expect(Scanner.ScanSymbols.getprice1);
      InsertSymbols(new ParseSymbol(Scanner.ScanSymbols.literal1));
      InsertSymbols(new ParseSymbol(Scanner.ScanSymbols.variable1));
      InsertSymbols(directiveSymbols);
      Expect(Scanner.ScanSymbols.leftparen1);
      RemoveSymbols(directiveSymbols);
      RemoveSymbols(new ParseSymbol(Scanner.ScanSymbols.leftparen1));
      RemoveSymbols(new ParseSymbol(Scanner.ScanSymbols.variable1));
      RemoveSymbols(new ParseSymbol(Scanner.ScanSymbols.literal1));
      InsertSymbols(new ParseSymbol(Scanner.ScanSymbols.comma1));
      Expression();
      RemoveSymbols(new ParseSymbol(Scanner.ScanSymbols.comma1));
      InsertSymbols(new ParseSymbol(Scanner.ScanSymbols.literal1));
      InsertSymbols(new ParseSymbol(Scanner.ScanSymbols.variable1));
      Expect(Scanner.ScanSymbols.comma1);
      RemoveSymbols(new ParseSymbol(Scanner.ScanSymbols.variable1));
      RemoveSymbols(new ParseSymbol(Scanner.ScanSymbols.literal1));
      InsertSymbols(new ParseSymbol(Scanner.ScanSymbols.comma1));
      Expression();
      RemoveSymbols(new ParseSymbol(Scanner.ScanSymbols.comma1));
      InsertSymbols(new ParseSymbol(Scanner.ScanSymbols.literal1));
      InsertSymbols(new ParseSymbol(Scanner.ScanSymbols.variable1));
      Expect(Scanner.ScanSymbols.comma1);
      RemoveSymbols(new ParseSymbol(Scanner.ScanSymbols.variable1));
      RemoveSymbols(new ParseSymbol(Scanner.ScanSymbols.literal1));
			InsertSymbols(new ParseSymbol(Scanner.ScanSymbols.rightparen1));
      Expression();
      RemoveSymbols(new ParseSymbol(Scanner.ScanSymbols.rightparen1));
      InsertSymbols(addSymbols);
      Expect(Scanner.ScanSymbols.rightparen1);
      RemoveSymbols(addSymbols);
      Symbol namedFunction = symbolTable.Find(Scanner.ScanSymbols.getprice1);
      Emit(Parser.ParserSymbols.call2,namedFunction.SymbolName,3); 
    }
    public void ParseConvert()
    {
      InsertSymbols(addSymbols);
      InsertSymbols(mathSymbols);
      InsertSymbols(new ParseSymbol(Scanner.ScanSymbols.leftparen1));
      Expect(Scanner.ScanSymbols.convert1);
      InsertSymbols(new ParseSymbol(Scanner.ScanSymbols.literal1));
      InsertSymbols(new ParseSymbol(Scanner.ScanSymbols.variable1));
      Expect(Scanner.ScanSymbols.leftparen1);
      RemoveSymbols(new ParseSymbol(Scanner.ScanSymbols.leftparen1));
      RemoveSymbols(new ParseSymbol(Scanner.ScanSymbols.variable1));
      RemoveSymbols(new ParseSymbol(Scanner.ScanSymbols.literal1));
      if (SymbolIn(new ParseSymbol(Scanner.ScanSymbols.literal1)))
      {
        Emit(Parser.ParserSymbols.push2,stringValue);
        InsertSymbols(new ParseSymbol(Scanner.ScanSymbols.comma1));
        Expect(Scanner.ScanSymbols.literal1);
        RemoveSymbols(new ParseSymbol(Scanner.ScanSymbols.comma1));
      }
      else if (SymbolIn(new ParseSymbol(Scanner.ScanSymbols.variable1)))
      {
        VariableAccess();
      }
      else SyntaxError();
      InsertSymbols(new ParseSymbol(Scanner.ScanSymbols.comma1));
      RemoveSymbols(new ParseSymbol(Scanner.ScanSymbols.comma1));
      InsertSymbols(new ParseSymbol(Scanner.ScanSymbols.literal1));
      InsertSymbols(new ParseSymbol(Scanner.ScanSymbols.variable1));
      Expect(Scanner.ScanSymbols.comma1);
      RemoveSymbols(new ParseSymbol(Scanner.ScanSymbols.leftparen1));
      RemoveSymbols(new ParseSymbol(Scanner.ScanSymbols.variable1));
      InsertSymbols(new ParseSymbol(Scanner.ScanSymbols.rightparen1));
      RemoveSymbols(new ParseSymbol(Scanner.ScanSymbols.literal1));
      if (SymbolIn(new ParseSymbol(Scanner.ScanSymbols.literal1)))
      {
        Emit(Parser.ParserSymbols.push2,stringValue);
        Expect(Scanner.ScanSymbols.literal1);
      }
      else if (SymbolIn(new ParseSymbol(Scanner.ScanSymbols.variable1)))
      {
        VariableAccess();
      }
      else SyntaxError();
      RemoveSymbols(new ParseSymbol(Scanner.ScanSymbols.rightparen1));
      InsertSymbols(addSymbols);
      InsertSymbols(mathSymbols);
      Expect(Scanner.ScanSymbols.rightparen1);
      RemoveSymbols(addSymbols);
      RemoveSymbols(mathSymbols);
      Symbol namedFunction = symbolTable.Find(Scanner.ScanSymbols.convert1);
      Emit(Parser.ParserSymbols.call2,namedFunction.SymbolName,2); 
      RemoveSymbols(addSymbols);
      RemoveSymbols(mathSymbols);
    }
    public void ParseABS()
    {
      InsertSymbols(addSymbols);
      InsertSymbols(mathSymbols);
      InsertSymbols(new ParseSymbol(Scanner.ScanSymbols.leftparen1));
      Expect(Scanner.ScanSymbols.abs1);
      InsertSymbols(expressionSymbols);
      RemoveSymbols(new ParseSymbol(Scanner.ScanSymbols.leftparen1));
      Expect(Scanner.ScanSymbols.leftparen1);
      InsertSymbols(new ParseSymbol(Scanner.ScanSymbols.rightparen1));
      Expression();
      RemoveSymbols(expressionSymbols);
      RemoveSymbols(new ParseSymbol(Scanner.ScanSymbols.rightparen1));
      InsertSymbols(addSymbols);
      InsertSymbols(mathSymbols);
      Expect(Scanner.ScanSymbols.rightparen1);
      RemoveSymbols(addSymbols);
      RemoveSymbols(mathSymbols);
      Symbol namedFunction = symbolTable.Find(Scanner.ScanSymbols.abs1);
      Emit(Parser.ParserSymbols.call2,namedFunction.SymbolName,1); 
      RemoveSymbols(addSymbols);
      RemoveSymbols(mathSymbols);
    }
    public void ParseSQRT()
    {
      InsertSymbols(addSymbols);
      InsertSymbols(mathSymbols);
      InsertSymbols(new ParseSymbol(Scanner.ScanSymbols.leftparen1));
      Expect(Scanner.ScanSymbols.sqrt1);
      InsertSymbols(expressionSymbols);
      RemoveSymbols(new ParseSymbol(Scanner.ScanSymbols.leftparen1));
      Expect(Scanner.ScanSymbols.leftparen1);
      InsertSymbols(new ParseSymbol(Scanner.ScanSymbols.rightparen1));
      Expression();
      RemoveSymbols(expressionSymbols);
      RemoveSymbols(new ParseSymbol(Scanner.ScanSymbols.rightparen1));
      InsertSymbols(addSymbols);
      InsertSymbols(mathSymbols);
      Expect(Scanner.ScanSymbols.rightparen1);
      RemoveSymbols(addSymbols);
      RemoveSymbols(mathSymbols);
      Symbol namedFunction = symbolTable.Find(Scanner.ScanSymbols.sqrt1);
      Emit(Parser.ParserSymbols.call2,namedFunction.SymbolName,1); 
      RemoveSymbols(addSymbols);
      RemoveSymbols(mathSymbols);
    }
    //public void ParseBDP()
    //{
    //  InsertSymbols(addSymbols);
    //  InsertSymbols(mathSymbols);
    //  InsertSymbols(new ParseSymbol(Scanner.ScanSymbols.leftparen1));
    //  Expect(Scanner.ScanSymbols.bdp1);
    //  InsertSymbols(new ParseSymbol(Scanner.ScanSymbols.literal1));
    //  InsertSymbols(new ParseSymbol(Scanner.ScanSymbols.variable1));
    //  Expect(Scanner.ScanSymbols.leftparen1);
    //  RemoveSymbols(new ParseSymbol(Scanner.ScanSymbols.leftparen1));
    //  RemoveSymbols(new ParseSymbol(Scanner.ScanSymbols.variable1));
    //  RemoveSymbols(new ParseSymbol(Scanner.ScanSymbols.literal1));
    //  if (SymbolIn(new ParseSymbol(Scanner.ScanSymbols.literal1)))
    //  {
    //    Emit(Parser.ParserSymbols.push2,stringValue);
    //    InsertSymbols(new ParseSymbol(Scanner.ScanSymbols.comma1));
    //    Expect(Scanner.ScanSymbols.literal1);
    //    RemoveSymbols(new ParseSymbol(Scanner.ScanSymbols.comma1));
    //  }
    //  else if (SymbolIn(new ParseSymbol(Scanner.ScanSymbols.variable1)))
    //  {
    //    VariableAccess();
    //  }
    //  else SyntaxError();
    //  InsertSymbols(new ParseSymbol(Scanner.ScanSymbols.literal1));
    //  InsertSymbols(new ParseSymbol(Scanner.ScanSymbols.variable1));
    //  Expect(Scanner.ScanSymbols.comma1);
    //  RemoveSymbols(new ParseSymbol(Scanner.ScanSymbols.leftparen1));
    //  RemoveSymbols(new ParseSymbol(Scanner.ScanSymbols.variable1));
    //  RemoveSymbols(new ParseSymbol(Scanner.ScanSymbols.literal1));
    //  if (SymbolIn(new ParseSymbol(Scanner.ScanSymbols.literal1)))
    //  {
    //    Emit(Parser.ParserSymbols.push2,stringValue);
    //    InsertSymbols(new ParseSymbol(Scanner.ScanSymbols.comma1));
    //    Expect(Scanner.ScanSymbols.literal1);
    //    RemoveSymbols(new ParseSymbol(Scanner.ScanSymbols.comma1));
    //  }
    //  else if (SymbolIn(new ParseSymbol(Scanner.ScanSymbols.variable1)))
    //  {
    //    VariableAccess();
    //  }
    //  else SyntaxError();
    //  InsertSymbols(new ParseSymbol(Scanner.ScanSymbols.literal1));
    //  InsertSymbols(new ParseSymbol(Scanner.ScanSymbols.variable1));
    //  Expect(Scanner.ScanSymbols.comma1);
    //  RemoveSymbols(new ParseSymbol(Scanner.ScanSymbols.leftparen1));
    //  RemoveSymbols(new ParseSymbol(Scanner.ScanSymbols.variable1));
    //  InsertSymbols(new ParseSymbol(Scanner.ScanSymbols.rightparen1));
    //  RemoveSymbols(new ParseSymbol(Scanner.ScanSymbols.literal1));
    //  if (SymbolIn(new ParseSymbol(Scanner.ScanSymbols.literal1)))
    //  {
    //    Emit(Parser.ParserSymbols.push2,stringValue);
    //    Expect(Scanner.ScanSymbols.literal1);
    //  }
    //  else if (SymbolIn(new ParseSymbol(Scanner.ScanSymbols.variable1)))
    //  {
    //    VariableAccess();
    //  }
    //  else SyntaxError();
    //  RemoveSymbols(new ParseSymbol(Scanner.ScanSymbols.rightparen1));
    //  InsertSymbols(addSymbols);
    //  InsertSymbols(mathSymbols);
    //  Expect(Scanner.ScanSymbols.rightparen1);
    //  RemoveSymbols(addSymbols);
    //  RemoveSymbols(mathSymbols);
    //  Symbol namedFunction = symbolTable.Find(Scanner.ScanSymbols.bdp1);
    //  Emit(Parser.ParserSymbols.call2,namedFunction.SymbolName,3); 
    //  RemoveSymbols(addSymbols);
    //  RemoveSymbols(mathSymbols);
    //}
    public void ParsePOW()
    {
      InsertSymbols(addSymbols);
      InsertSymbols(mathSymbols);
      InsertSymbols(new ParseSymbol(Scanner.ScanSymbols.leftparen1));
      Expect(Scanner.ScanSymbols.pow1);
      InsertSymbols(new ParseSymbol(Scanner.ScanSymbols.numeral1));
      InsertSymbols(new ParseSymbol(Scanner.ScanSymbols.variable1));
      InsertSymbols(directiveSymbols);
      Expect(Scanner.ScanSymbols.leftparen1);
      RemoveSymbols(directiveSymbols);
      RemoveSymbols(new ParseSymbol(Scanner.ScanSymbols.leftparen1));
      RemoveSymbols(new ParseSymbol(Scanner.ScanSymbols.variable1));
      RemoveSymbols(new ParseSymbol(Scanner.ScanSymbols.numeral1));
      InsertSymbols(new ParseSymbol(Scanner.ScanSymbols.comma1));
      Expression();
      RemoveSymbols(new ParseSymbol(Scanner.ScanSymbols.comma1));
      InsertSymbols(new ParseSymbol(Scanner.ScanSymbols.numeral1));
      InsertSymbols(new ParseSymbol(Scanner.ScanSymbols.variable1));
      InsertSymbols(directiveSymbols);
      Expect(Scanner.ScanSymbols.comma1);
      RemoveSymbols(directiveSymbols);
      RemoveSymbols(new ParseSymbol(Scanner.ScanSymbols.leftparen1));
      RemoveSymbols(new ParseSymbol(Scanner.ScanSymbols.variable1));
      RemoveSymbols(new ParseSymbol(Scanner.ScanSymbols.numeral1));
      InsertSymbols(new ParseSymbol(Scanner.ScanSymbols.rightparen1));
      Expression();
      RemoveSymbols(new ParseSymbol(Scanner.ScanSymbols.rightparen1));
      InsertSymbols(addSymbols);
      InsertSymbols(mathSymbols);
      Expect(Scanner.ScanSymbols.rightparen1);
      RemoveSymbols(addSymbols);
      RemoveSymbols(mathSymbols);
      Symbol namedFunction = symbolTable.Find(Scanner.ScanSymbols.pow1);
      Emit(Parser.ParserSymbols.call2,namedFunction.SymbolName,2); 
      RemoveSymbols(addSymbols);
      RemoveSymbols(mathSymbols);
    }
// ******************************************************************************************************************************************************************************************************************
// ***************************************************************************************************** B U I L T - I N  B O O L E A N **********************************************************************************************
// ******************************************************************************************************************************************************************************************************************
    public void ParseIn()
    {
      int arguments = 0;
      Symbol namedFunction = symbolTable.Find(Scanner.ScanSymbols.in1);
      InsertSymbols(new ParseSymbol(Scanner.ScanSymbols.leftparen1));
      Expect(Scanner.ScanSymbols.in1);
      InsertSymbols(new ParseSymbol(Scanner.ScanSymbols.literal1));
      InsertSymbols(new ParseSymbol(Scanner.ScanSymbols.variable1));
      Expect(Scanner.ScanSymbols.leftparen1);
      RemoveSymbols(new ParseSymbol(Scanner.ScanSymbols.leftparen1));
      while (!IsInError)
      {
        if (SymbolIn(new ParseSymbol(Scanner.ScanSymbols.rightparen1))) break;
        if (SymbolIn(new ParseSymbol(Scanner.ScanSymbols.literal1)))
        {
          Emit(Parser.ParserSymbols.push2,stringValue);
          arguments++;
          InsertSymbols(new ParseSymbol(Scanner.ScanSymbols.comma1));
          InsertSymbols(new ParseSymbol(Scanner.ScanSymbols.rightparen1));
          Expect(Scanner.ScanSymbols.literal1);
          RemoveSymbols(new ParseSymbol(Scanner.ScanSymbols.comma1));
          RemoveSymbols(new ParseSymbol(Scanner.ScanSymbols.rightparen1));
        }
        else if (SymbolIn(new ParseSymbol(Scanner.ScanSymbols.variable1)))
        {
          InsertSymbols(new ParseSymbol(Scanner.ScanSymbols.rightparen1));
          VariableAccess();
          RemoveSymbols(new ParseSymbol(Scanner.ScanSymbols.rightparen1));
          arguments++;
        }
        if (SymbolIn(new ParseSymbol(Scanner.ScanSymbols.comma1)))
        {
          Expect(Scanner.ScanSymbols.comma1);
        }
      }
      InsertSymbols(new ParseSymbol(Scanner.ScanSymbols.rightparen1));
      Expect(Scanner.ScanSymbols.rightparen1);
      RemoveSymbols(new ParseSymbol(Scanner.ScanSymbols.rightparen1));
      RemoveSymbols(new ParseSymbol(Scanner.ScanSymbols.literal1));
      RemoveSymbols(new ParseSymbol(Scanner.ScanSymbols.variable1));
      Emit(Parser.ParserSymbols.call2,namedFunction.SymbolName,arguments); 
    }
    public void ParseIsNull()
    {
      int arguments = 0;
      Symbol namedFunction = symbolTable.Find(Scanner.ScanSymbols.isnull1);
      InsertSymbols(new ParseSymbol(Scanner.ScanSymbols.leftparen1));
      Expect(Scanner.ScanSymbols.isnull1);
      InsertSymbols(new ParseSymbol(Scanner.ScanSymbols.literal1));
      InsertSymbols(new ParseSymbol(Scanner.ScanSymbols.variable1));
      Expect(Scanner.ScanSymbols.leftparen1);
      RemoveSymbols(new ParseSymbol(Scanner.ScanSymbols.leftparen1));
      while (!IsInError)
      {
        if (SymbolIn(new ParseSymbol(Scanner.ScanSymbols.rightparen1))) break;
        if (SymbolIn(new ParseSymbol(Scanner.ScanSymbols.literal1)))
        {
          Emit(Parser.ParserSymbols.push2, stringValue);
          arguments++;
          InsertSymbols(new ParseSymbol(Scanner.ScanSymbols.comma1));
          InsertSymbols(new ParseSymbol(Scanner.ScanSymbols.rightparen1));
          Expect(Scanner.ScanSymbols.literal1);
          RemoveSymbols(new ParseSymbol(Scanner.ScanSymbols.comma1));
          RemoveSymbols(new ParseSymbol(Scanner.ScanSymbols.rightparen1));
        }
        else if (SymbolIn(new ParseSymbol(Scanner.ScanSymbols.variable1)))
        {
          InsertSymbols(new ParseSymbol(Scanner.ScanSymbols.rightparen1));
          InsertSymbols(new ParseSymbol(Scanner.ScanSymbols.comma1));
          VariableAccess();
          RemoveSymbols(new ParseSymbol(Scanner.ScanSymbols.rightparen1));
          RemoveSymbols(new ParseSymbol(Scanner.ScanSymbols.comma1));
          arguments++;
        }
        else if (SymbolIn(new ParseSymbol(Scanner.ScanSymbols.numeral1)))
        {
          InsertSymbols(new ParseSymbol(Scanner.ScanSymbols.rightparen1));
          InsertSymbols(new ParseSymbol(Scanner.ScanSymbols.comma1));
          Emit(Parser.ParserSymbols.push2, numeralValue);
          Expect(Scanner.ScanSymbols.numeral1);
          arguments++;
          RemoveSymbols(new ParseSymbol(Scanner.ScanSymbols.rightparen1));
          RemoveSymbols(new ParseSymbol(Scanner.ScanSymbols.comma1));
        }
        else SyntaxError();
        if (SymbolIn(new ParseSymbol(Scanner.ScanSymbols.comma1)))
        {
          Expect(Scanner.ScanSymbols.comma1);
        }
      }
      InsertSymbols(new ParseSymbol(Scanner.ScanSymbols.rightparen1));
      Expect(Scanner.ScanSymbols.rightparen1);
      RemoveSymbols(new ParseSymbol(Scanner.ScanSymbols.rightparen1));
      RemoveSymbols(new ParseSymbol(Scanner.ScanSymbols.literal1));
      RemoveSymbols(new ParseSymbol(Scanner.ScanSymbols.variable1));
      Emit(Parser.ParserSymbols.call2,namedFunction.SymbolName,arguments); 
    }
// *******************************************************************************************************************************************************************************************************************************************************************
// ************************************************************************************************************ E N D  B U I L T - I N  F U N C T I O N S  ******************************************************************************************************************
// *******************************************************************************************************************************************************************************************************************************************************************
    public void Term()
    {
      InsertSymbols(mathSymbols);
      InsertSymbols(relationSymbols);
      Factor();
      RemoveSymbols(relationSymbols);
      SimpleTerm();
      RemoveSymbols(mathSymbols);
      return;
    }
    public void SimpleTerm()
    {
      while (SymbolIn(mathSymbols))
      {
        Scanner.ScanSymbols currSymbol = currentSymbolType;
        InsertSymbols(factorSymbols);
        InsertSymbols(mathFunctionSymbols);
        Expect(currentSymbolType);
        RemoveSymbols(factorSymbols);
        RemoveSymbols(mathFunctionSymbols);
        Factor();
        if (Scanner.ScanSymbols.asterisk1.Equals(currSymbol))
        {
          Emit(Parser.ParserSymbols.multiply2);
        }
        else if (Scanner.ScanSymbols.divide1.Equals(currSymbol))
        {
          Emit(Parser.ParserSymbols.divide2);
        }
      }
    }
    public void Factor()
    {
      if (SymbolIn(new ParseSymbol(Scanner.ScanSymbols.numeral1)))
      {
        Emit(Parser.ParserSymbols.push2, numeralValue);
        Expect(currentSymbolType);
        if (SymbolIn(relationSymbols)) Relation();
        SyntaxCheck();
      }
      else if (SymbolIn(new ParseSymbol(Scanner.ScanSymbols.variable1)))
      {
        VariableAccess();
        if (SymbolIn(new ParseSymbol(Scanner.ScanSymbols.in1))) ParseIn();
        else if(SymbolIn(new ParseSymbol(Scanner.ScanSymbols.like1)))ParseLike();
        else if(SymbolIn(new ParseSymbol(Scanner.ScanSymbols.like1)))ParseTrim();
        else if (SymbolIn(relationSymbols)) Relation();
        SyntaxCheck();
      }
      else if (SymbolIn(new ParseSymbol(Scanner.ScanSymbols.literal1)))
      {
        Emit(Parser.ParserSymbols.push2,stringValue);
        Expect(currentSymbolType);
        SyntaxCheck();
      }
      else if (SymbolIn(new ParseSymbol(Scanner.ScanSymbols.null1)))
      {
        EmitAsNull(Parser.ParserSymbols.push2);
        Expect(currentSymbolType);
        SyntaxCheck();
      }
      else if (SymbolIn(new ParseSymbol(Scanner.ScanSymbols.leftparen1)))
      {
        InsertSymbols(new ParseSymbol(Scanner.ScanSymbols.rightparen1));
        InsertSymbols(expressionSymbols);
        InsertSymbols(directiveSymbols);
        Expect(currentSymbolType);
        RemoveSymbols(directiveSymbols);
        RemoveSymbols(expressionSymbols);
        if (SymbolIn(directiveSymbols)) Directive();
        Expression();
        Relation();
        Expect(Scanner.ScanSymbols.rightparen1);
        RemoveSymbols(new ParseSymbol(Scanner.ScanSymbols.rightparen1));
        SyntaxCheck();
      }
      else if (SymbolIn(directiveSymbols))
      {
        Directive();
      }
      else if (SymbolIn(new ParseSymbol(Scanner.ScanSymbols.minus1)))
      {
        SimpleExpression();
      }
      else
      {
        SyntaxError();
      }
    }
    public void Expression()
    {
      while (SymbolIn(expressionSymbols)||SymbolIn(directiveSymbols))
      {
        SimpleExpression();
      }
    }
    public void SimpleExpression()
    {
      InsertSymbols(addSymbols);
      InsertSymbols(termSymbols);
      InsertSymbols(signSymbols);
      InsertSymbols(directiveSymbols);
      InsertSymbols(new ParseSymbol(Scanner.ScanSymbols.variable1));
      InsertSymbols(new ParseSymbol(Scanner.ScanSymbols.literal1));
      InsertSymbols(new ParseSymbol(Scanner.ScanSymbols.null1));
      SyntaxCheck();
      RemoveSymbols(signSymbols);
      RemoveSymbols(directiveSymbols);
      if (SymbolIn(signSymbols))
      {
        Scanner.ScanSymbols currSymbol = currentSymbolType;
        Expect(currentSymbolType);
        if (Scanner.ScanSymbols.minus1.Equals(currSymbol))
        {
          if(SymbolIn(new ParseSymbol(Scanner.ScanSymbols.numeral1)))
          {
            Emit(Parser.ParserSymbols.push2, numeralValue);
            Expect(Scanner.ScanSymbols.numeral1);
          }
          else if(SymbolIn(new ParseSymbol(Scanner.ScanSymbols.variable1)))
          {
            VariableAccess();
          }
          else SyntaxError();
          Emit(Parser.ParserSymbols.negate2);
        }
        else if (Scanner.ScanSymbols.plus1.Equals(currSymbol))
        {
          if(SymbolIn(new ParseSymbol(Scanner.ScanSymbols.numeral1)))
          {
            Emit(Parser.ParserSymbols.push2, numeralValue);
            Expect(Scanner.ScanSymbols.numeral1);
          }
          else if(SymbolIn(new ParseSymbol(Scanner.ScanSymbols.variable1)))
          {
            VariableAccess();
          }
          //else if(SymbolIn(new ParseSymbol(Scanner.ScanSymbols.convert1)))    // sean 09/06  I think this needs to be al of the directive symbols
          //{
          //  ParseConvert();
          //}
          else if(SymbolIn(directiveSymbols))    // sean 09/06  I think this needs to be al of the directive symbols
          {
            Directive();
          }
          else SyntaxError();
          Emit(Parser.ParserSymbols.add2);
        }
        else if (Scanner.ScanSymbols.not1.Equals(currSymbol))
        {
          if(SymbolIn(new ParseSymbol(Scanner.ScanSymbols.numeral1)))
          {
            Emit(Parser.ParserSymbols.push2, numeralValue);
            Expect(Scanner.ScanSymbols.numeral1);
          }
          else if(SymbolIn(new ParseSymbol(Scanner.ScanSymbols.variable1)))
          {
            VariableAccess();
          }
          else SyntaxError();
          Emit(Parser.ParserSymbols.not2);
        }
        RemoveSymbols(termSymbols);
      }
      else
      {
        RemoveSymbols(termSymbols);
        Term();
      }
      RemoveSymbols(new ParseSymbol(Scanner.ScanSymbols.variable1));
      RemoveSymbols(new ParseSymbol(Scanner.ScanSymbols.literal1));
      RemoveSymbols(new ParseSymbol(Scanner.ScanSymbols.null1));
      while (SymbolIn(addSymbols))
      {
        Scanner.ScanSymbols currSymbol = currentSymbolType;
        InsertSymbols(termSymbols);
        InsertSymbols(new ParseSymbol(Scanner.ScanSymbols.variable1));
        InsertSymbols(new ParseSymbol(Scanner.ScanSymbols.literal1));
        InsertSymbols(new ParseSymbol(Scanner.ScanSymbols.null1));
        InsertSymbols(new ParseSymbol(Scanner.ScanSymbols.isnull1));
        InsertSymbols(stringManipulationSymbols);
        Expect(currentSymbolType);
        RemoveSymbols(stringManipulationSymbols);
        RemoveSymbols(new ParseSymbol(Scanner.ScanSymbols.variable1));
        RemoveSymbols(new ParseSymbol(Scanner.ScanSymbols.literal1));
        RemoveSymbols(new ParseSymbol(Scanner.ScanSymbols.null1));
        RemoveSymbols(new ParseSymbol(Scanner.ScanSymbols.isnull1));
        RemoveSymbols(termSymbols);
        Term();
        if (Scanner.ScanSymbols.plus1.Equals(currSymbol))
        {
          Emit(ParserSymbols.add2);
        }
        else if (Scanner.ScanSymbols.minus1.Equals(currSymbol))
        {
          Emit(ParserSymbols.subtract2);
        }
        else SyntaxError();
      }
      RemoveSymbols(addSymbols);
    }
// *********************************************************************************************************************************************************************************************************************
// ******************************************************************************************************* S Y M B O L   M A N A G E M E N T  **************************************************************************
// *********************************************************************************************************************************************************************************************************************
    public bool NextSymbol()
    {
      numeralValue = 0.00;
      int code = 0;
      int result = Read(ref code);
      if (result == 0xFFFF) return false;
      currentSymbolType = (Scanner.ScanSymbols)code;
      while (Scanner.ScanSymbols.newline1.Equals(currentSymbolType))
      {
        if (0xFFFF == Read(ref code)) return false;
        currentSymbolType=(Scanner.ScanSymbols)code;
        LineNumber++;
        SymbolNumber=-1;     // make sure the symbol number is relative zero so it conforms to the rest of the line numbers (which are relative zero)
      }
      if (Scanner.ScanSymbols.numeral1.Equals(currentSymbolType))
      {
        SymbolNumber++;
        Read(ref numeralValue);
      }
      else if (Scanner.ScanSymbols.label1.Equals(currentSymbolType))
      {
        SymbolNumber++;
        Read(ref stringValue);
      }
      else if (Scanner.ScanSymbols.literal1.Equals(currentSymbolType))
      {
        SymbolNumber++;
        Read(ref stringValue);
      }
      else if (Scanner.ScanSymbols.variable1.Equals(currentSymbolType))
      {
        SymbolNumber++;
        Read(ref stringValue);
      }
      else if(Scanner.ScanSymbols.unknown1.Equals(currentSymbolType))
      {
        SymbolNumber++;
        Read(ref stringValue);
        SyntaxError();
      }
      return true;
    }
    public bool SymbolIn(ParseSymbols parseSymbols)
    {
      return parseSymbols.SymbolIn(new ParseSymbol(currentSymbolType));
    }
    public bool SymbolIn(ParseSymbol parseSymbol)
    {
      return new ParseSymbol(currentSymbolType).Equals(parseSymbol);
    }
    public bool PeekSymbol(ParseSymbol parseSymbol)
    {
      Scanner.ScanSymbols peekSymbol;
      int intPeek = 0;
      Peek(ref intPeek);
      peekSymbol = (Scanner.ScanSymbols)intPeek;
      if (new ParseSymbol(peekSymbol).Equals(parseSymbol)) return true;
      return false;
    }
    public bool PeekSymbolIn(ParseSymbols parseSymbols)
    {
      Scanner.ScanSymbols peekSymbol;
      int intPeek = 0;
      Peek(ref intPeek);
      peekSymbol = (Scanner.ScanSymbols)intPeek;
      return parseSymbols.SymbolIn(peekSymbol);
    }
    public void Expect(Scanner.ScanSymbols symbol)
    {
      if (symbol.Equals(currentSymbolType)) NextSymbol();
      else SyntaxError(symbol);
      SyntaxCheck();
    }
    public void InsertSymbols(ParseSymbols groupSymbols)
    {
      parseSymbols.InsertSymbols(groupSymbols);
    }
    public void RemoveSymbols(ParseSymbols removeSymbols)
    {
      parseSymbols.RemoveSymbols(removeSymbols);
    }
    public void InsertSymbols(ParseSymbol parseSymbol)
    {
      parseSymbols.InsertSymbols(parseSymbol);
    }
    public void RemoveSymbols(ParseSymbol parseSymbol)
    {
      parseSymbols.RemoveSymbols(parseSymbol);
    }
    public bool IsInError { get; set; }
    public int LineNumber { get; set; }
    public int LastLineNumber { get; set; }
    public int SymbolNumber { get; set; }
    public int LastSymbolNumber { get; set; }
    public int StatementNumber { get; set; }
    public int LastStatementNumber { get; set; }
    public String LastMessage { get; set; }
// **************************************************************************************************************************************************************************************************************
// ******************************************************************************************************* E R R O R  H A N D L I N G  **************************************************************************
// **************************************************************************************************************************************************************************************************************
    public void SyntaxCheck()
    {
      if (!parseSymbols.SymbolIn(currentSymbolType)) SyntaxError();
    }
    public void SyntaxError(Scanner.ScanSymbols symbol)
    {
      ErrorExpect(symbol);
      while (!parseSymbols.SymbolIn(currentSymbolType) && NextSymbol()) ;
    }
    public void SyntaxError(String specificError)
    {
      if (!IsInError)
      {
        LastMessage=String.Format("<Syntax Error> {0} at line: {1} statement: {2} symbol: {3}",specificError,LineNumber,StatementNumber,SymbolNumber);
        LastLineNumber = LineNumber;
        LastSymbolNumber = SymbolNumber;
        LastStatementNumber = StatementNumber;
        IsInError = true;
      }
    }
    public void SyntaxError()
    {
      if (!IsInError)
      {
        if(Scanner.ScanSymbols.unknown1.Equals(currentSymbolType))LastMessage = String.Format("<Syntax Error> Unexpected Symbol '{0}' at line: {1} statement: {2} symbol: {3}",stringValue,LineNumber,StatementNumber,SymbolNumber);
        else LastMessage = String.Format("<Syntax Error> Unexpected Symbol '{0}' at line: {1} statement: {2} symbol: {3}",Scanner.SymbolToLiteralString(currentSymbolType),LineNumber,StatementNumber,SymbolNumber);
        LastLineNumber = LineNumber;
        LastSymbolNumber = SymbolNumber;
        LastStatementNumber = StatementNumber;
        IsInError = true;
      }
      while (!parseSymbols.SymbolIn(currentSymbolType) && NextSymbol()) ;
    }
    void ErrorExpect(Scanner.ScanSymbols symbolType)
    {
      if (IsInError) return;
      LastMessage=String.Format("<Syntax Error> Expected Symbol {0} at line: {1} statement: {2} symbol: {3}",Scanner.SymbolToLiteralString(symbolType),LineNumber,StatementNumber,SymbolNumber);
      LastLineNumber = LineNumber;
      LastSymbolNumber = SymbolNumber;
      LastStatementNumber = StatementNumber;
      IsInError = true;
    }
    public void Error(Parser.ErrorCodes errorCode, String message = null)
    {
      if (IsInError) return;
      switch (errorCode)
      {
        case Parser.ErrorCodes.DivideByZero :
          LastMessage = "<Divide by zero>";
          LastLineNumber = LineNumber;
          break;
      }
    }
// ***********************************************************************************************************************************************************************************************************************************************
// ***************************************************************************************************** C R E A T E   S Y M B O L S  ************************************************************************************************************
// ***********************************************************************************************************************************************************************************************************************************************
    private void CreateDeclarationSymbols()
    {
      declarationSymbols.Clear();
      declarationSymbols.Add(new ParseSymbol(Scanner.ScanSymbols.declare1));
    }
    private void CreateAssignmentStatementSymbols()
    {
      assignmentStatementSymbols.Clear();
      assignmentStatementSymbols.Add(new ParseSymbol(Scanner.ScanSymbols.equal1));
    }
    private void CreateStatementSymbols()
    {
      statementSymbols.Clear();
      statementSymbols.Add(new ParseSymbol(Scanner.ScanSymbols.variable1));
      statementSymbols.Add(new ParseSymbol(Scanner.ScanSymbols.label1));
      statementSymbols.Add(new ParseSymbol(Scanner.ScanSymbols.literal1));
      statementSymbols.Add(new ParseSymbol(Scanner.ScanSymbols.if1));
    }
    private void CreateSimpleExpressionSymbols()
    {
      simpleExpressionSymbols.Clear();
      simpleExpressionSymbols.Add(new ParseSymbol(Scanner.ScanSymbols.plus1));
      simpleExpressionSymbols.Add(new ParseSymbol(Scanner.ScanSymbols.minus1));
      simpleExpressionSymbols.Add(new ParseSymbol(Scanner.ScanSymbols.leftparen1));
      simpleExpressionSymbols.Add(new ParseSymbol(Scanner.ScanSymbols.numeral1));
    }
    private void CreateMathSymbols()
    {
      mathSymbols.Clear();
      mathSymbols.Add(new ParseSymbol(Scanner.ScanSymbols.asterisk1));
      mathSymbols.Add(new ParseSymbol(Scanner.ScanSymbols.divide1));
    }
    private void CreateTermSymbols()
    {
      termSymbols.Clear();
      termSymbols.Add(new ParseSymbol(Scanner.ScanSymbols.leftparen1));
      termSymbols.Add(new ParseSymbol(Scanner.ScanSymbols.numeral1));
    }
    private void CreateFactorSymbols()
    {
      factorSymbols.Clear();
      factorSymbols.Add(new ParseSymbol(Scanner.ScanSymbols.leftparen1));
      factorSymbols.Add(new ParseSymbol(Scanner.ScanSymbols.numeral1));
      factorSymbols.Add(new ParseSymbol(Scanner.ScanSymbols.variable1));
    }
    private void CreateAddSymbols()
    {
      addSymbols.Clear();
      addSymbols.Add(new ParseSymbol(Scanner.ScanSymbols.minus1));
      addSymbols.Add(new ParseSymbol(Scanner.ScanSymbols.plus1));
    }
    private void CreateSignSymbols()
    {
      signSymbols.Clear();
      signSymbols.Add(new ParseSymbol(Scanner.ScanSymbols.minus1));
      signSymbols.Add(new ParseSymbol(Scanner.ScanSymbols.plus1));
      signSymbols.Add(new ParseSymbol(Scanner.ScanSymbols.not1));
    }
    private void CreateExpressionSymbols()
    {
      expressionSymbols.Clear();
      expressionSymbols.Add(new ParseSymbol(Scanner.ScanSymbols.leftparen1));
      expressionSymbols.Add(new ParseSymbol(Scanner.ScanSymbols.minus1));
      expressionSymbols.Add(new ParseSymbol(Scanner.ScanSymbols.plus1));
      expressionSymbols.Add(new ParseSymbol(Scanner.ScanSymbols.numeral1));
      expressionSymbols.Add(new ParseSymbol(Scanner.ScanSymbols.variable1));
      expressionSymbols.Add(new ParseSymbol(Scanner.ScanSymbols.literal1));
      expressionSymbols.Add(new ParseSymbol(Scanner.ScanSymbols.not1));
      expressionSymbols.Add(new ParseSymbol(Scanner.ScanSymbols.null1));
    }
    private void CreateFunctionSymbols()
    {
      List<Symbol> symbols = new List<Symbol>(symbolTable.Values);
      List<Symbol> directiveSymbolsList = (from Symbol symbol in symbols where symbol.TypeOfSymbol.Equals(Symbol.SymbolType.FunctionSymbol) select symbol).ToList();
      foreach (Symbol symbol in directiveSymbolsList) directiveSymbols.Add(new ParseSymbol(symbol.Identifier));
    }
    private void CreateRelationSymbols()
    {
      relationSymbols.Clear();
      relationSymbols.Add(new ParseSymbol(Scanner.ScanSymbols.less1));
      relationSymbols.Add(new ParseSymbol(Scanner.ScanSymbols.lessequal1));
      relationSymbols.Add(new ParseSymbol(Scanner.ScanSymbols.greater1));
      relationSymbols.Add(new ParseSymbol(Scanner.ScanSymbols.greaterequal1));
      relationSymbols.Add(new ParseSymbol(Scanner.ScanSymbols.notequal1));
      relationSymbols.Add(new ParseSymbol(Scanner.ScanSymbols.equalequal1));
    }
    private void CreateLogicalSymbols()
    {
      logicalSymbols.Clear();
      logicalSymbols.Add(new ParseSymbol(Scanner.ScanSymbols.andand1));
      logicalSymbols.Add(new ParseSymbol(Scanner.ScanSymbols.oror1));
    }
    private void CreateMathFunctionSymbols()
    {
      mathFunctionSymbols.Clear();
      mathFunctionSymbols.Add(new ParseSymbol(Scanner.ScanSymbols.abs1));
      mathFunctionSymbols.Add(new ParseSymbol(Scanner.ScanSymbols.sqrt1));
      mathFunctionSymbols.Add(new ParseSymbol(Scanner.ScanSymbols.pow1));
      //mathFunctionSymbols.Add(new ParseSymbol(Scanner.ScanSymbols.bdp1));
      mathFunctionSymbols.Add(new ParseSymbol(Scanner.ScanSymbols.convert1));
    }
    private void CreateStringManipulationSymbols()
    {
      stringManipulationSymbols.Clear();
      stringManipulationSymbols.Add(new ParseSymbol(Scanner.ScanSymbols.substring1));
      stringManipulationSymbols.Add(new ParseSymbol(Scanner.ScanSymbols.trim1));
      stringManipulationSymbols.Add(new ParseSymbol(Scanner.ScanSymbols.upper1));
      stringManipulationSymbols.Add(new ParseSymbol(Scanner.ScanSymbols.lower1));
      stringManipulationSymbols.Add(new ParseSymbol(Scanner.ScanSymbols.convert1));
    }
    public static String SymbolToString(Parser.ParserSymbols code)
    {
      switch (code)
      {
        case Parser.ParserSymbols.directive_clear_modified2 :
          return "directive_clear_modified2";
        case Parser.ParserSymbols.declare2 :
          return "declare2";
        case Parser.ParserSymbols.undefined2 :
          return "undefined2";
        case Parser.ParserSymbols.add2 :
          return "add2";
        case Parser.ParserSymbols.subtract2 :
          return "subtract2" ;
        case Parser.ParserSymbols.multiply2 :
          return "multiply2";
        case Parser.ParserSymbols.divide2 :
          return "divide2";
        case Parser.ParserSymbols.assign2 :
          return "assign2";
        case Parser.ParserSymbols.negate2 :
          return "negate2";
        case Parser.ParserSymbols.variableaccess2 :
          return "variableaccess2";
        case Parser.ParserSymbols.call2 :
          return "call2";
        case Parser.ParserSymbols.push2 :
          return "push2";
        case Parser.ParserSymbols.codeend2 :
          return "codeend2";
        case Parser.ParserSymbols.do2 :
          return "do2";
        case Parser.ParserSymbols.defaddr2 :
          return "defaddr2";
        case Parser.ParserSymbols.goto2 :
          return "goto2";
        case Parser.ParserSymbols.less2 :
          return "less2";
        case Parser.ParserSymbols.lessequal2 :
          return "lessequal2";
        case Parser.ParserSymbols.greater2 :
          return "greater2";
        case Parser.ParserSymbols.greaterequal2 :
          return "greaterequal2";
        case Parser.ParserSymbols.equalequal2 :
          return "equalequal2" ;
        case Parser.ParserSymbols.noop2 :
          return "noop2" ;
        case Parser.ParserSymbols.not2 :
          return "not2" ;
        case Parser.ParserSymbols.oror2 :
          return "oror2" ;
        case Parser.ParserSymbols.andand2 :
          return "andand2" ;
        default :
          return "undefined code "+(int)code;
      }
    }
  }
}