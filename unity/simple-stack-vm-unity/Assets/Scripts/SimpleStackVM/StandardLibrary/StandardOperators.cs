
using System;
using System.Linq;

namespace SimpleStackVM
{
    public static class StandardOperators
    {
        #region Fields
        public static readonly IReadOnlyScope Scope = CreateScope();
        #endregion

        #region Methods
        public static Scope CreateScope()
        {
            var result = new Scope();

            result.Define(">", (vm, numArgs) =>
            {
                var right = vm.PopStack();
                var left = vm.PopStack();
                vm.PushStack(left.CompareTo(right) > 0);
            });

            result.Define(">=", (vm, numArgs) =>
            {
                var right = vm.PopStack();
                var left = vm.PopStack();
                vm.PushStack(left.CompareTo(right) >= 0);
            });

            result.Define("==", (vm, numArgs) =>
            {
                var right = vm.PopStack();
                var left = vm.PopStack();
                vm.PushStack(left.CompareTo(right) == 0);
            });

            result.Define("!=", (vm, numArgs) =>
            {
                var right = vm.PopStack();
                var left = vm.PopStack();
                vm.PushStack(left.CompareTo(right) != 0);
            });

            result.Define("<", (vm, numArgs) =>
            {
                var right = vm.PopStack();
                var left = vm.PopStack();
                vm.PushStack(left.CompareTo(right) < 0);
            });

            result.Define("<=", (vm, numArgs) =>
            {
                var right = vm.PopStack();
                var left = vm.PopStack();
                vm.PushStack(left.CompareTo(right) <= 0);
            });

            result.Define("!", (vm, numArgs) =>
            {
                var top = vm.PopStack<BoolValue>();
                vm.PushStack(!top.Value);
            });

            result.Define("+", (vm, numArgs) =>
            {
                if (numArgs == 0)
                {
                    return;
                }

                var args = vm.GetArgs(numArgs);
                if (args[0] is StringValue)
                {
                    var result = string.Join("", args.Value);
                    vm.PushStack(result);
                }
                else
                {
                    var result = 0.0;
                    for (var i = 0; i < args.Length; i++)
                    {
                        if (args[i] is NumberValue argNum)
                        {
                            result += argNum.Value;
                        }
                        else
                        {
                            throw new Exception("Add only works on numbers and strings");
                        }
                    }
                    vm.PushStack(result);
                }
            });

            result.Define("-", (vm, numArgs) =>
            {
                var right = vm.PopStack<NumberValue>();
                var left = vm.PopStack<NumberValue>();
                vm.PushStack(left.Value - right.Value);
            });

            result.Define("*", (vm, numArgs) =>
            {
                if (numArgs < 2)
                {
                    throw new Exception("Multiple operator expects more than 1 input");
                }

                var total = vm.PopStack<NumberValue>().Value;
                for (var i = numArgs - 2; i >= 0; i--)
                {
                    total *= vm.PopStack<NumberValue>().Value;
                }

                vm.PushStack(total);
            });

            result.Define("/", (vm, numArgs) =>
            {
                var right = vm.PopStack<NumberValue>();
                var left = vm.PopStack<NumberValue>();
                vm.PushStack(left.Value / right.Value);
            });

            result.Define("%", (vm, numArgs) =>
            {
                var right = vm.PopStack<NumberValue>();
                var left = vm.PopStack<NumberValue>();
                vm.PushStack(left.Value % right.Value);
            });

            return result;
        }
        #endregion
    }
}