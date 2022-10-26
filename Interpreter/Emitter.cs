using System;
using System.IO;
using log4net;

// FileName : Emitter.cs
// Author : Sean Kessler

namespace Axiom.Interpreter
{
  public class Emitter
  {
    private static ILog logger = LogManager.GetLogger(typeof(Emitter));
    private bool emitting = true;
    private int lastSymbol;
    private BinaryReader inputStream;
    private BinaryWriter outputStream;
    private bool debug = true;

    public Emitter(BinaryReader inputStream, BinaryWriter outputStream)
    {
      this.inputStream = inputStream;
      this.outputStream = outputStream;
    }
    public bool Debug
    {
      get { return debug; }
      set { debug = value; }
    }
    public void Emit(String literalValue)
    {
      if (!emitting) return;
      outputStream.Write(literalValue.Length);
      outputStream.Write(literalValue);
    }
// ************************************************************************
    public void Emit(Scanner.ScanSymbols code)
    {
      if (!emitting) return;
      outputStream.Write((int)code);
      if(Debug)logger.Info(Scanner.SymbolToString(code));
    }
    public void Emit(Scanner.ScanSymbols code,int value)
    {
      if (!emitting) return;
      outputStream.Write((int)code);
      outputStream.Write(value);
      if(Debug)logger.Info(Scanner.SymbolToString(code)+","+value.ToString());
    }
    public void Emit(Scanner.ScanSymbols code,double value)
    {
      if (!emitting) return;
      outputStream.Write((int)code);
      outputStream.Write(value);
      if(Debug)logger.Info(Scanner.SymbolToString(code)+","+value.ToString());
    }
    public void Emit(Scanner.ScanSymbols code,String value)
    {
      if (!emitting) return;
      outputStream.Write((int)code);
      outputStream.Write(value);
      if(Debug)logger.Info(Scanner.SymbolToString(code)+","+value.ToString());
    }
// **********************************************************************************************************************************************
    public long CodePointer()
    {
      return outputStream.BaseStream.Position;
    }
    public void Seek(long position)
    {
      outputStream.BaseStream.Seek(position, SeekOrigin.Begin);
    }
    public void Emit(Parser.ParserSymbols code)
    {
      if (!emitting) return;
      long positionBefore=outputStream.BaseStream.Position;
      outputStream.Write((int)code);
      long positionAfter=outputStream.BaseStream.Position;
      if(Debug)logger.Info(Parser.SymbolToString(code)+"["+positionBefore+","+positionAfter+"]");

    }
    public void Emit(Parser.ParserSymbols code,Object value)
    {
      if (!emitting) return;
      long positionBefore=outputStream.BaseStream.Position;
      outputStream.Write((int)code);
      Type type = value.GetType();
      outputStream.Write(type.ToString());
      outputStream.Write(value.ToString());
      long positionAfter=outputStream.BaseStream.Position;
      if(Debug)logger.Info(Parser.SymbolToString(code)+","+type.ToString()+","+value.ToString()+"["+positionBefore+","+positionAfter+"]");
    }
    public void Emit(Parser.ParserSymbols code,Object value,int intValue)
    {
      if (!emitting) return;
      long positionBefore=outputStream.BaseStream.Position;
      outputStream.Write((int)code);
      Type type = value.GetType();
      outputStream.Write(type.ToString());
      outputStream.Write(value.ToString());
      outputStream.Write(intValue);
      long positionAfter=outputStream.BaseStream.Position;
      if(Debug)logger.Info(Parser.SymbolToString(code)+","+type.ToString()+","+value.ToString()+","+intValue+"["+positionBefore+","+positionAfter+"]");
    }
    public void Emit(Parser.ParserSymbols code,long value)
    {
      if (!emitting) return;
      long positionBefore=outputStream.BaseStream.Position;
      outputStream.Write((int)code);
      outputStream.Write(value);
      long positionAfter=outputStream.BaseStream.Position;
      if(Debug)logger.Info(Parser.SymbolToString(code)+","+value.ToString()+","+value.ToString()+"["+positionBefore+","+positionAfter+"]");
    }
    public void EmitAsNull(Parser.ParserSymbols code)
    {
      if (!emitting) return;
      long positionBefore=outputStream.BaseStream.Position;
      outputStream.Write((int)code);
      Type type = typeof(System.Nullable); //value.GetType();
      outputStream.Write(type.ToString());
      outputStream.Write("null".ToString());
      long positionAfter=outputStream.BaseStream.Position;
      if(Debug)logger.Info(Parser.SymbolToString(code)+","+type.ToString()+","+"null".ToString()+"["+positionBefore+","+positionAfter+"]");
    }
// ************************************************************************
    public void Emit(int code, int op)
    {
      if (!emitting) return;
      outputStream.Write(code);
      outputStream.Write(op);
    }
    public void Emit(int identifier)
    {
      if (!emitting) return;
      outputStream.Write(identifier);
    }
    public void Emit(byte value)
    {
      if (!emitting) return;
      outputStream.Write(value);
    }
    public int Peek(ref int value)
    {
      value = inputStream.PeekChar();
      return value;
    }
    public int Peek()
    {
      int value = inputStream.PeekChar();
      return value;
    }
    public int Read()
    {
      lastSymbol = inputStream.Read();
      return lastSymbol;
    }
    public int Read(ref String literal)
    {
      literal=inputStream.ReadString();
      return 0;
    }
    public int Read(ref byte value)
    {
      try { value = inputStream.ReadByte(); return 0; }
      catch (EndOfStreamException) { return 0xFFFF; }
    }
    public int Read(ref int value)
    {
      try { value = inputStream.ReadInt32(); return 0; }
      catch (EndOfStreamException) { return 0xFFFF; }

    }
    public int Read(ref double value)
    {
      try { value = inputStream.ReadDouble(); return 0; }
      catch (EndOfStreamException) { return 0xFFFF; }
    }
    public bool Emitting
    {
      get
      {
        return emitting;
      }
      set
      {
        emitting = value;
      }
    }
  }
}
