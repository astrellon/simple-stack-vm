using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace SimpleStackVM
{
    public class VirtualMachine
    {
        public struct ScopeFrame
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
        public delegate void RunCommandHandler(IValue command, VirtualMachine vm);

        public readonly int StackSize;
        public bool DebugMode = false;

        private readonly List<IValue> stack;
        private readonly List<ScopeFrame> stackTrace;

        private readonly Dictionary<string, Scope> scopes;
        private Scope currentScope;

        private int programCounter;
        private bool running;
        private bool paused;

        public event RunCommandHandler? OnRunCommand;

        public bool IsRunning => this.running;
        public int ProgramCounter => this.programCounter;
        public Scope CurrentScope => this.currentScope;
        public IReadOnlyDictionary<string, Scope> Scopes => this.scopes;
        #endregion

        #region Constructor
        public VirtualMachine(int stackSize = 64)
        {
            this.StackSize = stackSize;

            this.scopes = new Dictionary<string, Scope>();
            this.programCounter = 0;
            this.stack = new List<IValue>(stackSize);
            this.stackTrace = new List<ScopeFrame>(stackSize);
        }
        #endregion

        #region Methods
        public void AddScope(Scope scope)
        {
            this.scopes[scope.ScopeName] = scope;
        }

        public void AddScopes(IEnumerable<Scope> scopes)
        {
            foreach (var scope in scopes) { this.AddScope(scope); }
        }

        public void Stop()
        {
            this.running = false;
        }

        public void Run(string? startScopeName = null)
        {
            if (startScopeName != null)
            {
                if (this.scopes.TryGetValue(startScopeName, out var startScope))
                {
                    this.currentScope = startScope;
                }
                else
                {
                    throw new ScopeException(this.CreateStackTrace(), $"Unable to find start scope: {startScopeName}");
                }
            }
            else if (this.currentScope.IsEmpty)
            {
                throw new Exception("Cannot run virtual machine with an empty scope");
            }

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

            if (this.DebugMode)
            {
                var debugLine = DebugScopeLine(this.currentScope, this.programCounter - 1);
                Console.WriteLine($"- {debugLine}");
            }

            switch (codeLine.Operator)
            {
                default:
                    {
                        throw new UnknownOperatorException(this.CreateStackTrace(), $"Unknown operator: {codeLine.Operator}");
                    }
                case Operator.Push:
                    {
                        if (codeLine.Input == null)
                        {
                            throw new OperatorException(this.CreateStackTrace(), "Push requires input");
                        }
                        this.stack.Add(codeLine.Input);
                        break;
                    }
                case Operator.Pop:
                    {
                        this.PopStack();
                        break;
                    }
                case Operator.JumpFalse:
                    {
                        var label = this.PopStack();
                        var top = this.PopStack();
                        if (top.Equals(BoolValue.False))
                        {
                            this.JumpToLabel(label);
                        }
                        break;
                    }
                case Operator.JumpTrue:
                    {
                        var label = this.PopStack();
                        var top = this.PopStack();
                        if (top.Equals(BoolValue.True))
                        {
                            this.JumpToLabel(label);
                        }
                        break;
                    }
                case Operator.Jump:
                    {
                        var label = this.PopStack();
                        this.JumpToLabel(label);
                        break;
                    }
                case Operator.Call:
                    {
                        var label = this.PopStack();
                        this.CallToLabel(label);
                        break;
                    }
                case Operator.Return:
                    {
                        this.Return();
                        break;
                    }
                case Operator.Run:
                    {
                        var top = this.PopStack();
                        if (this.OnRunCommand == null)
                        {
                            throw new OperatorException(this.CreateStackTrace(), $"Cannot run command {top}, no listener set");
                        }

                        this.OnRunCommand.Invoke(top, this);
                        break;
                    }
            }
        }

        public void CallToLabel(IValue label)
        {
            this.stackTrace.Add(new ScopeFrame(this.programCounter, this.currentScope));
            this.JumpToLabel(label);
        }

        public void Return()
        {
            if (!this.stackTrace.Any())
            {
                throw new StackException(this.CreateStackTrace(), "Unable to return, call stack empty");
            }

            var scopeFrame = this.stackTrace.Last();
            this.stackTrace.Pop();
            this.currentScope = scopeFrame.Scope;
            this.programCounter = scopeFrame.LineCounter;
        }

        public void JumpToLabel(IValue jumpTo)
        {
            if (jumpTo is StringValue stringValue)
            {
                this.JumpToLabel(stringValue.Value, null);
            }
            else if (jumpTo is ArrayValue arrayValue)
            {
                if (arrayValue.Value.Count == 0)
                {
                    throw new OperatorException(this.CreateStackTrace(), "Cannot jump to an empty array");
                }
                string? scopeName = null;
                var label = arrayValue.Value[0].ToString();
                if (arrayValue.Value.Count > 1)
                {
                    scopeName = arrayValue.Value[1].ToString();
                }

                this.JumpToLabel(label, scopeName);
            }
        }

        public void JumpToLabel(string label, string? scopeName)
        {
            if (!string.IsNullOrEmpty(scopeName))
            {
                if (this.scopes.TryGetValue(scopeName, out var scope))
                {
                    this.currentScope = scope;
                }
                else
                {
                    throw new ScopeException(this.CreateStackTrace(), $"Unable to find scope: {scopeName} for jump.");
                }
            }

            if (string.IsNullOrEmpty(label))
            {
                this.programCounter = 0;
                return;
            }

            if (this.currentScope.Labels.TryGetValue(label, out var line))
            {
                this.programCounter = line;
            }
            else
            {
                throw new OperatorException(this.CreateStackTrace(), $"Unable to jump to label: {label}");
            }
        }

        public IValue PopStack()
        {
            if (!this.stack.TryPop(out var obj))
            {
                throw new StackException(this.CreateStackTrace(), "Unable to pop stack, empty");
            }

            return obj;
        }

        public T PopStack<T>() where T : IValue
        {
            var obj = this.PopStack();
            if (obj.GetType() == typeof(T))
            {
                return (T)obj;
            }

            throw new StackException(this.CreateStackTrace(), "Unable to pop stack, type cast error");
        }

        public string PopStackString()
        {
            var obj = this.PopStack();
            return obj.ToString();
        }

        public void PushStack(IValue value)
        {
            this.stack.Add(value);
        }

        public IReadOnlyList<string> CreateStackTrace()
        {
            var result = new List<string>();

            result.Add(DebugScopeLine(this.currentScope, this.programCounter - 1));
            for (var i = this.stackTrace.Count - 1; i >= 0; i--)
            {
                var stackFrame = this.stackTrace[i];
                result.Add(DebugScopeLine(stackFrame.Scope, stackFrame.LineCounter));
            }

            return result;
        }

        private static string DebugScopeLine(Scope scope, int line)
        {
            var codeLine = scope.Code[line];
            var codeLineInput = codeLine.Input != null ? codeLine.Input.ToString() : "<empty>";
            return $"[{scope.ScopeName}]:{line - 1}:{codeLine.Operator}: [{codeLineInput}]";
        }
        #endregion
    }
}