using System;
using System.Text;
using System.IO;

// FileName : ParseSymbols.cs
// Author : Sean Kessler 

namespace Axiom.Interpreter
{
  public class Scanner : Emitter
  {
    public enum ScanSymbols { unknown1, directive_clear_modified1,declare1, null1, isnull1, convert1, getprice1, substring1, in1, like1, trim1, upper1, lower1, assign1, if1,then1,else1,goto1,equal1,equalequal1,less1,lessequal1,greater1,greaterequal1, notequal1,variable1, asterisk1, apostrophe1, comma1, label1, literal1, leftcurly1, rightcurly1, leftbracket1, rightbracket1, numeral1, char1, divide1, plus1, minus1, leftparen1, rightparen1, newline1, semicolon1,endtext1, andand1, oror1, abs1, not1, pow1, sqrt1,end1 };
    private enum WhiteSpace{spacechar=32,tabchar=9};
    private int character;
    private StringBuilder word;
    private SymbolTable symbolTable;
    private bool strictMode = true;  // if this is set to true then variables must first be declared (i.e.) a variable must either be 1) present in the symbol table or 2) declared prior to use.

    public Scanner(BinaryReader binaryReader, BinaryWriter binaryWriter,SymbolTable symbolTable)
      : base(binaryReader, binaryWriter)
    {
      this.symbolTable = symbolTable;
    }
    public bool Analyze()
    {
      if(!ReadCh())return false;
      while ((int)character != -1 && character != 0x001A)
      {
        SkipSeparators();
        if (0xFFFF == character || 0x001A == character) break;
        if (IsDigit(character) || '.'.Equals((char)character)) ScanNumeral();
        else if ('\r' == character) ScanNewLine();
        else if (';' == character) { Emit(ScanSymbols.semicolon1); ReadCh(); }
        else if (',' == character) { Emit(ScanSymbols.comma1); ReadCh(); }
        else if ('[' == character) { Emit(ScanSymbols.leftbracket1); ReadCh(); }
        else if (']' == character) { Emit(ScanSymbols.rightbracket1); ReadCh(); }
        else if ('*' == character) { Emit(ScanSymbols.asterisk1); ReadCh(); }
        else if ('/' == character) { ScanDivide(); }
        else if ('+' == character) { Emit(ScanSymbols.plus1); ReadCh(); }
        else if ('-' == character) { Emit(ScanSymbols.minus1); ReadCh(); }
        else if ('(' == character) { Emit(ScanSymbols.leftparen1); ReadCh(); }
        else if (')' == character) { Emit(ScanSymbols.rightparen1); ReadCh(); }
        else if ('!' == character) { Emit(ScanSymbols.not1); ReadCh(); }
        else if ('{' == character) { Emit(ScanSymbols.leftcurly1); ReadCh(); }
        else if ('}' == character) { Emit(ScanSymbols.rightcurly1); ReadCh(); }
        else if ('\'' == character) ScanLiteral();
        else if (';' == character) ScanComment();
        else if ('=' == character) ScanEqual();
        else if ('<' == character) ScanLess();
        else if ('>' == character) ScanGreater();
        else if ('&' == character) ScanAnd();
        else if ('|' == character) ScanOr();
        else if (IsAlpha(character))
        {
          if (!ScanWord()) return false;
        }
        else if ('#' == character)
        {
          if (!ScanDirective()) return false;
        }
        else 
        {
          ScanUnknown();
          return false;
        }
	    }
      Emit(Scanner.ScanSymbols.endtext1);
	    return true;
    }
    public bool StrictMode
    {
      get{return strictMode;}
      set{strictMode=value;}
    }
    public bool ReadCh()
    {
      try
      {
        character = Read();
        if (-1 == character) return false;
        return true;
      }
      catch(Exception)
      {
        character = -1;
        return false;
      }
    }
    public void ScanDivide()
    {
      Emit(ScanSymbols.divide1); 
      ReadCh();    
    }
    public bool PeekCh(ref char ch)
    {
      try
      {
        int character=Peek();
        if(-1==character)return false;
        ch=(Char)character;
        return true;
      }
      catch(Exception)
      {
        return false;
      }
    }
    public bool ScanDirective()
    {
      char nextChar='\0';
      if(!PeekCh(ref nextChar))return false;
      if(!nextChar.Equals('#'))return ScanWord();
      while (-1!=character && 0x0D != character && 0xFFFF != character)ReadCh();   // if we have ## then take the line as a comment
      return true;
    }
    public bool ScanWord()
    {
      StringBuilder sb = new StringBuilder();
      while (-1!=character && 0x0D != character && !IsKeySymbol() && 0xFFFF != character && (int)WhiteSpace.spacechar!=character && (int)WhiteSpace.tabchar != character)
      {
        sb.Append((char)character);
        ReadCh();
      }
      if (0 == sb.Length) return true;
      String symbolName = sb.ToString();
      if (':'.Equals((char)character))
      {
        Emit(ScanSymbols.label1);
        Emit(symbolName);
        ReadCh();
      }
      else if(symbolTable.ContainsKey(symbolName))
      {
        Symbol symbol = symbolTable[symbolName];
        if (Symbol.SymbolType.FunctionSymbol.Equals(symbol.TypeOfSymbol)) Emit(symbol.Identifier);
        else if(Symbol.SymbolType.KeywordSymbol.Equals(symbol.TypeOfSymbol)) Emit(symbol.Identifier);
        else if(Symbol.SymbolType.DirectiveSymbol.Equals(symbol.TypeOfSymbol)) Emit(symbol.Identifier);
        else Emit(ScanSymbols.variable1, symbol.SymbolName);
      }
      else if(symbolTable.ContainsKey(symbolName.ToLower()))
      {
        symbolName = symbolName.ToLower();
        Symbol symbol = symbolTable[symbolName];
        if (Symbol.SymbolType.FunctionSymbol.Equals(symbol.TypeOfSymbol)) Emit(symbol.Identifier);
        else if(Symbol.SymbolType.KeywordSymbol.Equals(symbol.TypeOfSymbol)) Emit(symbol.Identifier);
        else Emit(ScanSymbols.variable1, symbol.SymbolName);
      }
      else if(StrictMode) // if we are in strict mode then do not add the symbol to the symbol table.  The parser will then generate an undefined symbol error if the symbol is not declared prior to use
      {
        Emit(ScanSymbols.variable1, symbolName);   
      }
      else  // if it is not in the symbol table AND we are not in STRICT mode then create a new variable on the fly. If we are in STRUCT mode then a DECLARATION is necessary in order to introduce a new variable
      {
        Symbol symbol=new Symbol(symbolName,Scanner.ScanSymbols.variable1,Symbol.SymbolType.UserSymbol);    // if it is not in the symbol table then create a new variable on the fly
        symbolTable.Add(symbolName,symbol);
        Emit(ScanSymbols.variable1, symbol.SymbolName);
      }
      return true;
    }
    public bool ScanNumeral()
    {
      int[] chBuffer = new int[128];
      int chIndex = 0;
      while (0xFFFF != character && (IsDigit(character) || IsInHex(character) || '.'.Equals((char)character)))
      {
        if (chIndex >= chBuffer.Length) return false;
        chBuffer[chIndex++] = character;
        ReadCh();
      }
      if (character == 69) Exponent(chBuffer, chIndex);
      else if ('h' == character || 'H' == character) { Hex(chBuffer, chIndex); ReadCh(); }
      else if ('b' == character || 'B' == character) { Binary(chBuffer, chIndex); ReadCh(); }
      else if (chIndex > 0 && ('b' == chBuffer[chIndex - 1] || 'B' == chBuffer[chIndex - 1])) Binary(chBuffer, chIndex - 1);
      else Decimal(chBuffer, chIndex);
      return true;
    }
    public void Exponent(int[] chBuffer, int chIndex)
    {
      chBuffer[chIndex++] = character;
      ReadCh();
      while (0xFFFF != character && (character == 43 || character == 45 || IsDigit(character)))
      {
        if (chIndex >= chBuffer.Length) return;
        chBuffer[chIndex++] = character;
        ReadCh();
      }
      Decimal(chBuffer, chIndex);
    }
    public void Binary(int[] chBuffer,int chIndex)
    {
      int value=0;
	    int multiplier=1;

      for(--chIndex;chIndex>=0;chIndex--)
	    {
	      switch(chBuffer[chIndex])
		    {
		      case '0' : break;
		      case '1' : {value+=multiplier;break;}
			    default : {Emit(ScanSymbols.unknown1);return;}
		    }
        multiplier*=2;
      }
	    Emit(ScanSymbols.numeral1,value);
    }
    public void Decimal(int[] chBuffer,int chIndex)
    {
      StringBuilder sb = new StringBuilder();
      for (int index = 0; index < chBuffer.Length; index++) sb.Append((char)chBuffer[index]);
      double value = double.Parse(sb.ToString());
	    Emit(ScanSymbols.numeral1,value);
    }
    public bool IsInHex(int character)
    {
      if ('A'.Equals(character) || 'B'.Equals(character) || 'C'.Equals(character) || 'D'.Equals(character) || 'E'.Equals(character) || 'F'.Equals(character) ||
         'a'.Equals(character) || 'b'.Equals(character) || 'c'.Equals(character) || 'd'.Equals(character) || 'e'.Equals(character) || 'f'.Equals(character)) return true;
      return false;
    }
    void Hex(int[] chBuffer, int chIndex)
    {
        int value=0;
        int multiplier=1;

        for(--chIndex;chIndex>=0;chIndex--)
        {
          switch(chBuffer[chIndex])
          {
            case '0' : break;
            case '1' : {value+=multiplier;break;}
            case '2' : {value+=multiplier*2;break;}
            case '3' : {value+=multiplier*3;break;}
            case '4' : {value+=multiplier*4;break;}
            case '5' : {value+=multiplier*5;break;}
            case '6' : {value+=multiplier*6;break;}
            case '7' : {value+=multiplier*7;break;}
            case '8' : {value+=multiplier*8;break;}
            case '9' : {value+=multiplier*9;break;}
            case 'a' :
            case 'A' : {value+=multiplier*10;break;}
            case 'b' :
            case 'B' : {value+=multiplier*11;break;}
            case 'c' :
            case 'C' : {value+=multiplier*12;break;}
            case 'd' :
            case 'D' : {value+=multiplier*13;break;}
            case 'e' :
            case 'E' : {value+=multiplier*14;break;}
            case 'f' :
            case 'F' : {value+=multiplier*15;break;}
            default : {Emit(ScanSymbols.unknown1);return;}
          }
          multiplier*=16;
        }
        Emit(ScanSymbols.numeral1,value);
    }

    public void SkipSeparators()
    {
      while (character.Equals((int)WhiteSpace.spacechar) || character.Equals((int)WhiteSpace.tabchar)) ReadCh();
    }
    public void ScanNewLine()
    {
      Emit(ScanSymbols.newline1);
      ReadCh();
      if (character.Equals('\n')) ReadCh();
    }
    public void ScanAnd()
    {
      int character=0;

      Peek(ref character);
      if ('&'.Equals((char)character))
      {
        Emit(ScanSymbols.andand1);
        ReadCh();
        ReadCh();
      }
      else
      {
        Emit(ScanSymbols.unknown1);
        ReadCh();
      }
    }
    public void ScanOr()
    {
      int character=0;

      Peek(ref character);
      if ('|'.Equals((char)character))
      {
        Emit(ScanSymbols.oror1);
        ReadCh();
        ReadCh();
      }
      else
      {
        Emit(ScanSymbols.unknown1);
        ReadCh();
      }
    }
    public void ScanEqual()
    {
      int character=0;

      Peek(ref character);
      if ('='.Equals((char)character))
      {
        Emit(ScanSymbols.equalequal1);
        ReadCh();
        ReadCh();
      }
      else
      {
        Emit(ScanSymbols.equal1);
        ReadCh();
      }
    }
    public void ScanLess()
    {
      int character=0;

      Peek(ref character);
      if ('='.Equals((char)character))
      {
        Emit(ScanSymbols.lessequal1);
        ReadCh();
        ReadCh();
      }
      else if ('>'.Equals((char)character))
      {
        Emit(ScanSymbols.notequal1);
        ReadCh();
        ReadCh();
      }
      else
      {
        Emit(ScanSymbols.less1);
        ReadCh();
      }
    }
    public void ScanGreater()
    {
      int character=0;

      Peek(ref character);
      if ('='.Equals((char)character))
      {
        Emit(ScanSymbols.greaterequal1);
        ReadCh();
        ReadCh();
      }
      else
      {
        Emit(ScanSymbols.greater1);
        ReadCh();
      }
    }
    public void ScanComment()
    {
      ReadCh();
      while(!character.Equals('\r')&&!character.Equals(0xFFFF))ReadCh();
    }
    public bool IsAlpha(int character)
    {
      return Char.IsLetter((char)character);
    }
    public bool IsDigit(int character)
    {
      return Char.IsDigit((char)character);
    }
    public bool IsKeySymbol()
    {
      return ('}'==character||'{'==character||'<'==character||'>'==character||';'==character || ' ' == character || ',' == character || '[' == character || ']' == character || ':' == character || '*' == character || '/' == character || '+' == character || '-' == character || '(' == character || ')' == character ||'='==character);
    }
    public void ScanLiteral()
    {
      int character;
      word = new StringBuilder();
      ReadCh();  
      character = this.character;
      if (character == '\'')
      {
        Emit(ScanSymbols.literal1,word.ToString());
        ReadCh();
        return;
      }
      while (-1!=character && 0xFFFF != this.character && 0x0D != this.character) 
      {
        if ('\'' == this.character)
        {
          char peekChar=(char)0;
          PeekCh(ref peekChar);
          if ('\'' != peekChar) break;
          ReadCh();
        }
        word.Append((char)this.character);
        if (!ReadCh()) break;
      }
      ReadCh();
      Emit(ScanSymbols.literal1,word.ToString());
    }
    public void ScanUnknown()
    {
      ReadCh();
	    Emit(ScanSymbols.unknown1);
    }
    public static String SymbolToLiteralString(Scanner.ScanSymbols code)
    {
      switch(code)
      {
        case Scanner.ScanSymbols.directive_clear_modified1 :
          return "directive_clear_modified1";
        case Scanner.ScanSymbols.declare1 :
          return "declare";
        case Scanner.ScanSymbols.unknown1 :
          return "unknown symbol";
        case Scanner.ScanSymbols.assign1 :
          return "assignment";
        case Scanner.ScanSymbols.if1 :
          return "if";
        case Scanner.ScanSymbols.then1 :
          return "then";
        case Scanner.ScanSymbols.else1 :
          return "else";
        case Scanner.ScanSymbols.goto1 :
          return "goto";
        case Scanner.ScanSymbols.equal1 :
          return "=";
        case Scanner.ScanSymbols.equalequal1 :
          return "==";
        case Scanner.ScanSymbols.less1 :
          return "<";
        case Scanner.ScanSymbols.lessequal1 :
          return "<=";
        case Scanner.ScanSymbols.greater1 :
          return ">";
        case Scanner.ScanSymbols.greaterequal1 :
          return ">=";
        case Scanner.ScanSymbols.notequal1 :
          return "<>";
        case Scanner.ScanSymbols.variable1 :
          return "variable";
        case Scanner.ScanSymbols.asterisk1 :
          return "*";
        case Scanner.ScanSymbols.apostrophe1 :
          return "'";
        case Scanner.ScanSymbols.comma1 :
          return ",";
        case Scanner.ScanSymbols.label1 :
          return "label";
        case Scanner.ScanSymbols.literal1 :
          return "'";
        case Scanner.ScanSymbols.leftcurly1 :
          return "{";
        case Scanner.ScanSymbols.rightcurly1 :
          return "}";
        case Scanner.ScanSymbols.leftbracket1 :
          return "[";
        case Scanner.ScanSymbols.rightbracket1 :
          return "]";
        case Scanner.ScanSymbols.numeral1 :
          return "numeral";
        case Scanner.ScanSymbols.char1 :
          return "character";
        case Scanner.ScanSymbols.divide1 :
          return "/";
        case Scanner.ScanSymbols.plus1 :
          return "+";
        case Scanner.ScanSymbols.minus1 :
          return "-";
        case Scanner.ScanSymbols.leftparen1 :
          return "(";
        case Scanner.ScanSymbols.rightparen1 :
          return ")";
        case Scanner.ScanSymbols.newline1 :
          return "newline";
        case Scanner.ScanSymbols.semicolon1 :
          return ";";
        case Scanner.ScanSymbols.endtext1 :
          return ";";
        //case Scanner.ScanSymbols.bdp1 :
        //  return "bdp1";
        case Scanner.ScanSymbols.end1  :
          return "end";
        case Scanner.ScanSymbols.andand1  :
          return "&&";
        case Scanner.ScanSymbols.oror1  :
          return "||";
        case Scanner.ScanSymbols.abs1  :
          return "abs";
        case Scanner.ScanSymbols.pow1  :
          return "pow";
        case Scanner.ScanSymbols.sqrt1  :
          return "sqrt";
        case Scanner.ScanSymbols.not1  :
          return "!";
        case Scanner.ScanSymbols.convert1  :
          return "convert";
        case Scanner.ScanSymbols.in1  :
          return "in";
        case Scanner.ScanSymbols.like1 :
          return "like";
        case Scanner.ScanSymbols.trim1 :
          return "trim";
        case Scanner.ScanSymbols.upper1 :
          return "upper";
        case Scanner.ScanSymbols.lower1 :
          return "lower";
        case Scanner.ScanSymbols.substring1 :
          return "substring";
				case Scanner.ScanSymbols.getprice1 :
				  return "getprice";
        case Scanner.ScanSymbols.null1 :
          return "null";
        case Scanner.ScanSymbols.isnull1 :
          return "isnull";
        default :
          return "";
      }
    }
    public static String SymbolToString(Scanner.ScanSymbols code)
    {
      switch(code)
      {
        case Scanner.ScanSymbols.directive_clear_modified1 :
          return "directive_clear_modified1";
        case Scanner.ScanSymbols.declare1 :
          return "declare1";
        case Scanner.ScanSymbols.unknown1 :
          return "unknown1";
        case Scanner.ScanSymbols.assign1 :
          return "assign1";
        case Scanner.ScanSymbols.if1 :
          return "if1";
        case Scanner.ScanSymbols.then1 :
          return "then1";
        case Scanner.ScanSymbols.else1 :
          return "else1";
        case Scanner.ScanSymbols.goto1 :
          return "goto1";
        case Scanner.ScanSymbols.equal1 :
          return "equal1";
        case Scanner.ScanSymbols.equalequal1 :
          return "equalequal1";
        case Scanner.ScanSymbols.less1 :
          return "less1";
        case Scanner.ScanSymbols.lessequal1 :
          return "lessequal1";
        case Scanner.ScanSymbols.greater1 :
          return "greater1";
        case Scanner.ScanSymbols.greaterequal1 :
          return "greaterequal1";
        case Scanner.ScanSymbols.notequal1 :
          return "notequal1";
        case Scanner.ScanSymbols.variable1 :
          return "variable1";
        case Scanner.ScanSymbols.asterisk1 :
          return "asterisk1";
        case Scanner.ScanSymbols.apostrophe1 :
          return "apostrophe1";
        case Scanner.ScanSymbols.comma1 :
          return "comma1";
        case Scanner.ScanSymbols.label1 :
          return "label1";
        case Scanner.ScanSymbols.literal1 :
          return "literal1";
        case Scanner.ScanSymbols.leftcurly1 :
          return "leftcurly1";
        case Scanner.ScanSymbols.rightcurly1 :
          return "rightcurly1";
        case Scanner.ScanSymbols.leftbracket1 :
          return "leftbracket1";
        case Scanner.ScanSymbols.rightbracket1 :
          return "rightbracket1";
        case Scanner.ScanSymbols.numeral1 :
          return "numeral1";
        case Scanner.ScanSymbols.char1 :
          return "char1";
        case Scanner.ScanSymbols.divide1 :
          return "divide1";
        case Scanner.ScanSymbols.plus1 :
          return "plus1";
        case Scanner.ScanSymbols.minus1 :
          return "minus1";
        case Scanner.ScanSymbols.leftparen1 :
          return "leftparen1";
        case Scanner.ScanSymbols.rightparen1 :
          return "rightparen1";
        case Scanner.ScanSymbols.newline1 :
          return "newline1";
        case Scanner.ScanSymbols.semicolon1 :
          return "semicolon1";
        case Scanner.ScanSymbols.endtext1 :
          return "endtext1";
        //case Scanner.ScanSymbols.bdp1 :
        //  return "bdp1";
        case Scanner.ScanSymbols.end1  :
          return "end1";
        case Scanner.ScanSymbols.andand1  :
          return "andand1";
        case Scanner.ScanSymbols.oror1  :
          return "oror1";
        case Scanner.ScanSymbols.abs1  :
          return "abs1";
        case Scanner.ScanSymbols.pow1  :
          return "pow1";
        case Scanner.ScanSymbols.sqrt1  :
          return "sqrt1";
        case Scanner.ScanSymbols.not1  :
          return "not1";
        case Scanner.ScanSymbols.convert1  :
          return "convert1";
        case Scanner.ScanSymbols.in1  :
          return "in1";
        case Scanner.ScanSymbols.like1 :
          return "like1";
        case Scanner.ScanSymbols.trim1 :
          return "trim1";
        case Scanner.ScanSymbols.upper1 :
          return "upper1";
        case Scanner.ScanSymbols.lower1 :
          return "lower1";
        case Scanner.ScanSymbols.substring1 :
          return "substring1";
        case Scanner.ScanSymbols.getprice1 :
          return "getprice1";
        case Scanner.ScanSymbols.null1 :
          return "null1";
        case Scanner.ScanSymbols.isnull1 :
          return "isnull1";
        default :
          return "";
      }
    }
  }
}
