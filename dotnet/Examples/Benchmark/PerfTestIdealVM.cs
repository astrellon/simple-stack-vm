using System;
using System.Collections.Generic;

namespace LysitheaVM
{
    public class PerfTestIdealVM
    {
        public static readonly Script IdealScript = MakeIdealScript();
        #region Methods
        public static Script MakeIdealScript()
        {
            var incFunc = new BuiltinFunctionValue((vm, args) =>
            {
                vm.PushStack(args.GetIndexDouble(0) + 1);
            });

            var zeroNum = new NumberValue(0);
            var oneNum = new NumberValue(1);
            var twoNum = new NumberValue(2);
            var stepFuncCode = new List<CodeLine>
            {
                new CodeLine(Operator.CallDirect, new ArrayValue(new IValue[] { PerfTestVMFunctions.FuncRand, zeroNum})),
                new CodeLine(Operator.CallDirect, new ArrayValue(new IValue[] { PerfTestVMFunctions.FuncRand, zeroNum})),
                new CodeLine(Operator.Add, null),
                new CodeLine(Operator.Return, null)
            };

            var stepFunc = new Function(stepFuncCode, Function.EmptyParameters, Function.EmptyLabels, "step");
            var stepFuncValue = new FunctionValue(stepFunc);

            var mainFuncCode = new List<CodeLine>
            {
                new CodeLine(Operator.Push, zeroNum),
                new CodeLine(Operator.Define, new StringValue("total")),
                new CodeLine(Operator.Push, zeroNum),
                new CodeLine(Operator.Define, new StringValue("counter")),

                // Before Loop line 4
                // Comparison
                new CodeLine(Operator.Get, new StringValue("counter")),
                new CodeLine(Operator.Push, new NumberValue(1_000_000)),
                new CodeLine(Operator.LessThan, null),
                new CodeLine(Operator.JumpFalse, new StringValue(":EndLoop")),

                // Loop body line 8
                new CodeLine(Operator.CallDirect, new ArrayValue(new IValue[] { stepFuncValue, zeroNum })),
                new CodeLine(Operator.AddTo, new StringValue("total")),
                new CodeLine(Operator.Inc, new StringValue("counter")),
                new CodeLine(Operator.Jump, new StringValue(":StartLoop")),

                // After Loop line 12
                new CodeLine(Operator.Push, new StringValue("Done: ")),
                new CodeLine(Operator.Get, new StringValue("total")),
                new CodeLine(Operator.CallDirect, new ArrayValue(new IValue[] { PerfTestVMFunctions.FuncPrint, twoNum})),
            };

            var mainLabels = new Dictionary<string, int>
            {
                { ":StartLoop", 4 },
                { ":EndLoop", 12 },
            };
            var mainFunc = new Function(mainFuncCode, Function.EmptyParameters, mainLabels, "main");
            var mainFuncValue = new FunctionValue(mainFunc);

            var globalFuncCode = new List<CodeLine>
            {
                new CodeLine(Operator.Push, mainFuncValue),
                new CodeLine(Operator.Define, new StringValue("main")),
                new CodeLine(Operator.Push, stepFuncValue),
                new CodeLine(Operator.Define, new StringValue("step")),
                new CodeLine(Operator.CallDirect, new ArrayValue(new IValue[] { mainFuncValue, zeroNum}))
            };
            var globalFunc = new Function(globalFuncCode, Function.EmptyParameters, Function.EmptyLabels, "global");

            return new Script(Scope.Empty, globalFunc);
        }
        #endregion
    }
}