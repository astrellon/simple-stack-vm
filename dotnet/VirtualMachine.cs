using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace SimpleStackVM
{
    public class VirtualMachine
    {
        private struct ScopeFrame
        {
            public readonly int LineCounter;
            public readonly Scope Scope;

            public ScopeFrame(int lineCounter, Scope scope)
            {
                this.LineCounter = lineCounter;
                this.Scope = scope;
            }
        }

        #region Fields
        public delegate IValue GetVariableHandler(string name, VirtualMachine vm);
        public delegate void RunCommandHandler(string command, VirtualMachine vm);

        private readonly Stack<IValue> stack;
        private readonly Stack<ScopeFrame> callStack;

        private readonly Dictionary<string, Scope> scopes;
        private Scope currentScope;

        private int programCounter;
        private bool running;
        private bool paused;

        public event GetVariableHandler? OnGetVariable;
        public event RunCommandHandler? OnRunCommand;

        public bool IsRunning => this.running;
        public int ProgramCounter => this.programCounter;
        #endregion

        #region Constructor
        public VirtualMachine(int stackSize = 64)
        {
            this.scopes = new Dictionary<string, Scope>();
            this.programCounter = 0;
            this.stack = new Stack<IValue>(stackSize);
            this.callStack = new Stack<ScopeFrame>(stackSize);
        }
        #endregion

        #region Methods
        public void Stop()
        {
            this.running = false;
        }

        public void Run()
        {
            this.running = true;
            this.paused = false;
            while (this.running && !this.paused)
            {
                this.Step();
            }
        }

        public void SetPause(bool paused)
        {
            this.paused = paused;
        }

        public void Step()
        {
            if (this.programCounter >= this.currentScope.Code.Count)
            {
                this.Stop();
                return;
            }

            var codeLine = this.currentScope.Code[this.programCounter++];

            switch (codeLine.Operator)
            {
                case Operator.Push:
                    {
                        if (codeLine.Input == null)
                        {
                            throw new Exception("Push requires input");
                        }
                        this.stack.Push(this.EvaluateLine(codeLine.Input));
                        break;
                    }
                case Operator.Pop:
                    {
                        this.stack.Pop();
                        break;
                    }
                case Operator.JumpFalse:
                    {
                        var top = this.stack.Pop();
                        if (codeLine.Input != null)
                        {
                            if (top.Equals(false))
                            {
                                this.JumpToLabel(this.EvaluateLineString(codeLine.Input));
                            }
                        }
                        else
                        {
                            var label = this.PopStackString();
                            if (top.Equals(false))
                            {
                                this.JumpToLabel(label);
                            }
                        }
                        break;
                    }
                case Operator.JumpTrue:
                    {
                        var top = this.stack.Pop();
                        if (codeLine.Input != null)
                        {
                            if (top.Equals(true))
                            {
                                this.JumpToLabel(this.EvaluateLineString(codeLine.Input));
                            }
                        }
                        else
                        {
                            var label = this.PopStackString();
                            if (top.Equals(true))
                            {
                                this.JumpToLabel(label);
                            }
                        }
                        break;
                    }
                case Operator.Jump:
                    {
                        if (codeLine.Input != null)
                        {
                            this.JumpToLabel(this.EvaluateLineString(codeLine.Input));
                        }
                        else
                        {
                            var label = this.PopStackString();
                            this.JumpToLabel(label);
                        }
                        break;
                    }
                case Operator.Call:
                    {
                        if (codeLine.Input != null)
                        {
                            this.CallToLabel(this.EvaluateLineString(codeLine.Input));
                        }
                        else
                        {
                            var label = this.PopStackString();
                            this.CallToLabel(label);
                        }
                        break;
                    }
                case Operator.Return:
                    {
                        this.Return();
                        break;
                    }
                case Operator.Run:
                    {
                        if (codeLine.Input != null)
                        {
                            var commandName = this.EvaluateLineString(codeLine.Input);
                            if (this.OnRunCommand == null)
                            {
                                throw new Exception($"Cannot run command {commandName}, no listener set");
                            }

                            this.OnRunCommand.Invoke(commandName, this);
                        }
                        else
                        {
                            var top = this.PopStackString();
                            if (this.OnRunCommand == null)
                            {
                                throw new Exception($"Cannot run command {top}, no listener set");
                            }

                            this.OnRunCommand.Invoke(top, this);
                        }
                        break;
                    }
                case Operator.Log:
                    {
                        if (codeLine.Input != null)
                        {
                            Console.WriteLine($"[{this.programCounter}]: {this.EvaluateLineString(codeLine.Input)}");
                        }
                        break;
                    }
            }

        }

        public string EvaluateLineString(IValue input)
        {
            var result = this.EvaluateLine(input);
            return result.ToString() ?? "<null>";
        }

        public IValue EvaluateLine(IValue input)
        {
            if (input is StringValue str)
            {
                var strValue = str.Value;
                if (string.IsNullOrEmpty(strValue))
                {
                    return StringValue.Empty;
                }

                if (strValue.First() == '{' && strValue.Last() == '}')
                {
                    IValue result = NullValue.Value;
                    var varName = strValue.Substring(1, strValue.Length - 2);
                    var path = ObjectPath.Create(varName);
                    if (path.Path.First() == "TOP")
                    {
                        result = this.stack.Peek();
                    }
                    else
                    {
                        if (this.OnGetVariable == null)
                        {
                            throw new Exception($"Cannot get variable {varName}, no listener set");
                        }

                        result = this.OnGetVariable.Invoke(path.Current, this);
                    }

                    if (path.HasMorePath && result is ObjectValue objectValue)
                    {
                        if (objectValue.TryGetValue(path.NextIndex(), out var objectResult))
                        {
                            return objectResult;
                        }

                        throw new Exception($"Unable to get path from variable {path.Path.ToString()}");
                    }

                    return result;
                }

                return str;
            }

            return input;
        }

        public void CallToLabel(string label)
        {
            this.callStack.Push(new ScopeFrame(this.programCounter + 1, this.currentScope));
            this.JumpToLabel(label);
        }

        public void Return()
        {
            if (!this.callStack.Any())
            {
                throw new Exception("Unable to return, call stack empty");
            }

            var scopeFrame = this.callStack.Pop();
            this.currentScope = scopeFrame.Scope;
            this.programCounter = scopeFrame.LineCounter;
        }

        public void JumpToLabel(string label)
        {
            if (this.currentScope.Labels.TryGetValue(label, out var line))
            {
                this.programCounter = line;
            }
            else
            {
                throw new Exception($"Unable to jump to label: {label}");
            }
        }

        public IValue PopObject()
        {
            if (!this.stack.TryPop(out var obj))
            {
                throw new Exception("Unable to pop stack, empty");
            }

            return obj;
        }

        public double PopStackDouble()
        {
            var obj = this.PopObject();

            if (obj is NumberValue num)
            {
                return num.Value;
            }

            throw new Exception($"Top of stack not a double: {obj}");
        }

        public int PopStackInt()
        {
            var obj = this.PopObject();

            if (obj is NumberValue num)
            {
                return (int)num.Value;
            }

            throw new Exception($"Top of stack not an int: {obj}");
        }

        public string PopStackString()
        {
            var obj = this.PopObject();
            return obj.ToString() ?? "<null>";
        }

        public void PushStack(IValue value)
        {
            this.stack.Push(value);
        }
        #endregion
    }
}