using System;

namespace SimpleStackVM
{
    public static class StandardValueLibrary
    {
        #region Fields
        public static readonly IReadOnlyScope Scope = CreateScope();
        #endregion

        #region Methods
        public static Scope CreateScope()
        {
            var result = new Scope();

            result.Define("toString", vm =>
            {
                var top = vm.PeekStack();
                if (top is StringValue)
                {
                    return;
                }

                top = vm.PopStack();
                vm.PushStack(new StringValue(top.ToString()));
            });

            result.Define("typeof", vm =>
            {
                var top = vm.PopStack();
                vm.PushStack(new StringValue(GetTypeOf(top)));
            });

            result.Define("compareTo", vm =>
            {
                var right = vm.PopStack();
                var left = vm.PopStack();
                vm.PushStack(new NumberValue(left.CompareTo(right)));
            });

            return result;
        }

        public static string GetTypeOf(IValue input)
        {
            if (input is StringValue)
            {
                return "string";
            }
            if (input is NumberValue)
            {
                return "number";
            }
            if (input is BoolValue)
            {
                return "bool";
            }
            if (input is ArrayValue)
            {
                return "array";
            }
            if (input is ObjectValue)
            {
                return "object";
            }
            if (input is NullValue)
            {
                return "null";
            }
            if (input is AnyValue)
            {
                return "any";
            }
            if (input is BuiltinProcedureValue)
            {
                return "builtin-proc";
            }
            if (input is ProcedureValue)
            {
                return "proc";
            }

            return "unknown";
        }
        #endregion
    }
}
