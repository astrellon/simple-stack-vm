
using System;
using System.Linq;

namespace SimpleStackVM
{
    public static class StandardComparisonLibrary
    {
        #region Fields
        public static readonly Scope Scope = CreateScope();
        #endregion

        #region Methods
        public static Scope CreateScope()
        {
            var result = new Scope();

            var greater = new BuiltinProcedureValue(vm =>
            {
                var right = vm.PopStack();
                var left = vm.PopStack();
                vm.PushStack(new BoolValue(left.CompareTo(right) > 0));
            });
            result.Define(">", greater);
            result.Define("comp.greater", greater);

            var greaterEquals = new BuiltinProcedureValue(vm =>
            {
                var right = vm.PopStack();
                var left = vm.PopStack();
                vm.PushStack(new BoolValue(left.CompareTo(right) >= 0));
            });
            result.Define(">=", greaterEquals);
            result.Define("comp.greaterEquals", greaterEquals);

            var equals = new BuiltinProcedureValue(vm =>
            {
                var right = vm.PopStack();
                var left = vm.PopStack();
                vm.PushStack(new BoolValue(left.CompareTo(right) == 0));
            });
            result.Define("==", equals);
            result.Define("comp.equals", equals);

            var notEquals = new BuiltinProcedureValue(vm =>
            {
                var right = vm.PopStack();
                var left = vm.PopStack();
                vm.PushStack(new BoolValue(left.CompareTo(right) != 0));
            });
            result.Define("!=", notEquals);
            result.Define("comp.notEquals", notEquals);

            var less = new BuiltinProcedureValue(vm =>
            {
                var right = vm.PopStack();
                var left = vm.PopStack();
                vm.PushStack(new BoolValue(left.CompareTo(right) < 0));
            });
            result.Define("<", less);
            result.Define("comp.less", less);

            var lessEquals = new BuiltinProcedureValue(vm =>
            {
                var right = vm.PopStack();
                var left = vm.PopStack();
                vm.PushStack(new BoolValue(left.CompareTo(right) <= 0));
            });
            result.Define("<=", lessEquals);
            result.Define("comp.lessEquals", lessEquals);

            var not = new BuiltinProcedureValue(vm =>
            {
                var top = vm.PopStack<BoolValue>();
                vm.PushStack(new BoolValue(!top.Value));
            });
            result.Define("!", not);
            result.Define("comp.not", not);

            return result;
        }
        #endregion
    }
}