using System.Collections.Generic;

// FileName : StackElement.cs
// Author : Sean Kessler

namespace Axiom.Interpreter
{
  public class CodeStack
  {
    private Stack<StackElement> codeStack = new Stack<StackElement>();
    public CodeStack()
    {
    }
    public void Push(StackElement stackElement)
    {
      codeStack.Push(stackElement);
    }
    public StackElement Pop()
    {
      return codeStack.Pop();
    }
  }
  public class StackElement
  {
    public StackElement()
    {
    }
    public GenericData Value { get; set; }
    public Symbol Symbol { get; set; }
  }
}
