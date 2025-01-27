using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;

#nullable enable

namespace LysitheaVM
{
    public static partial class StandardArrayLibrary
    {
        #region Fields
        public static readonly IReadOnlyScope Scope = CreateScope();
        #endregion

        #region Methods
        public static Scope CreateScope()
        {
            var result = new Scope();

            var arrayFunctions = new Dictionary<string, IValue>
            {
                {"join", new BuiltinFunctionValue((vm, args) =>
                {
                    vm.PushStack(new ArrayValue(args.Value));
                }, "array.join")},

                {"length", new BuiltinFunctionValue((vm, args) =>
                {
                    var top = args.GetIndex<IArrayValue>(0);
                    vm.PushStack(top.ArrayValues.Count);
                }, "array.length")},

                {"set", new BuiltinFunctionValue((vm, args) =>
                {
                    var top = args.GetIndex<ArrayValue>(0);
                    var index = args.GetIndex<NumberValue>(1);
                    var value = args.GetIndex(2);
                    vm.PushStack(Set(top, index.IntValue, value));
                }, "array.set")},

                {"get", new BuiltinFunctionValue((vm, args) =>
                {
                    var top = args.GetIndex<IArrayValue>(0);
                    var index = args.GetIndex<NumberValue>(1);
                    if (top.TryGetIndex(index.IntValue, out var value))
                    {
                        vm.PushStack(value);
                    }
                    else
                    {
                        vm.PushStack(NullValue.Value);
                    }
                }, "array.get")},

                {"insert", new BuiltinFunctionValue((vm, args) =>
                {
                    var top = args.GetIndex<ArrayValue>(0);
                    var index = args.GetIndex<NumberValue>(1);
                    var value = args.GetIndex(2);
                    vm.PushStack(Insert(top, index.IntValue, value));
                }, "array.insert")},

                {"insertFlatten", new BuiltinFunctionValue((vm, args) =>
                {
                    var top = args.GetIndex<ArrayValue>(0);
                    var index = args.GetIndex<NumberValue>(1);
                    var value = args.GetIndex<IArrayValue>(2);
                    vm.PushStack(InsertFlatten(top, index.IntValue, value));
                }, "array.insertFlatten")},

                {"remove", new BuiltinFunctionValue((vm, args) =>
                {
                    var top = args.GetIndex<ArrayValue>(0);
                    var value = args.GetIndex(1);
                    vm.PushStack(Remove(top, value));
                }, "array.remove")},

                {"removeAt", new BuiltinFunctionValue((vm, args) =>
                {
                    var top = args.GetIndex<ArrayValue>(0);
                    var index = args.GetIndex<NumberValue>(1);
                    vm.PushStack(RemoveAt(top, index.IntValue));
                }, "array.removeAt")},

                {"removeAll", new BuiltinFunctionValue((vm, args) =>
                {
                    var top = args.GetIndex<ArrayValue>(0);
                    var value = args.GetIndex(1);
                    vm.PushStack(RemoveAll(top, value));
                }, "array.removeAll")},

                {"contains", new BuiltinFunctionValue((vm, args) =>
                {
                    var top = args.GetIndex<IArrayValue>(0);
                    var value = args.GetIndex(1);
                    vm.PushStack(Contains(top, value));
                }, "array.contains")},

                {"indexOf", new BuiltinFunctionValue((vm, args) =>
                {
                    var top = args.GetIndex<IArrayValue>(0);
                    var value = args.GetIndex(1);
                    vm.PushStack(IndexOf(top, value));
                }, "array.indexOf")},

                {"sublist", new BuiltinFunctionValue((vm, args) =>
                {
                    var top = args.GetIndex<ArrayValue>(0);
                    var index = args.GetIndex<NumberValue>(1);
                    var length = args.GetIndex<NumberValue>(2);
                    vm.PushStack(SubList(top, index.IntValue, length.IntValue));
                }, "array.sublist")},

                {"add", new BuiltinFunctionValue((vm, args) =>
                {
                    var top = args.GetIndex<IArrayValue>(0);
                    var arg2 = args.GetIndex(1);
                    if (arg2 is NumberValue numArg2)
                    {
                        var result = top.ArrayValues.Select(i => {
                            if (i is NumberValue n)
                            {
                                return new NumberValue(n.Value + numArg2.Value);
                            }
                            return i;
                        });
                        vm.PushStack(new ArrayValue(result));
                    }
                    else if (arg2 is IArrayValue arrArg2)
                    {
                        var max = Math.Max(arrArg2.Count(), top.Count());
                        var result = new List<IValue>(max);
                        for (var i = 0; i < max; i++)
                        {
                            var a = 0.0;
                            var b = 0.0;
                            if (i < arrArg2.Count())
                            {
                                a = arrArg2.GetIndexDouble(i);
                            }
                            if (i < top.Count())
                            {
                                b = top.GetIndexDouble(i);
                            }
                            result.Add(new NumberValue(a + b));
                        }
                        vm.PushStack(new ArrayValue(result));
                    }
                    else
                    {
                        throw new Exception("Unexpected value type");
                    }
                }, "array.add")},

                {"mul", new BuiltinFunctionValue((vm, args) =>
                {
                    var top = args.GetIndex<IArrayValue>(0);
                    var arg2 = args.GetIndex(1);
                    if (arg2 is NumberValue numArg2)
                    {
                        var result = top.ArrayValues.Select(i => {
                            if (i is NumberValue n)
                            {
                                return new NumberValue(n.Value * numArg2.Value);
                            }
                            return i;
                        });
                        vm.PushStack(new ArrayValue(result));
                    }
                    else
                    {
                        throw new Exception("Unexpected value type");
                    }
                }, "array.mul")}
            };

            result.TryDefine("array", new ObjectValue(arrayFunctions));

            return result;
        }

        public static ArrayValue Set(ArrayValue self, int index, IValue input)
        {
            var newValue = self.Value.ToList();
            newValue[self.CalcIndex(index)] = input;
            return new ArrayValue(newValue);
        }

        public static ArrayValue Insert(ArrayValue self, int index, IValue input)
        {
            var newValue = self.Value.ToList();
            newValue.Insert(self.CalcIndex(index), input);
            return new ArrayValue(newValue);
        }

        public static ArrayValue InsertFlatten(ArrayValue self, int index, IArrayValue input)
        {
            var newValue = self.Value.ToList();
            newValue.InsertRange(self.CalcIndex(index), input.ArrayValues);
            return new ArrayValue(newValue);
        }

        public static ArrayValue RemoveAt(ArrayValue self, int index)
        {
            var newValue = self.Value.ToList();
            newValue.RemoveAt(self.CalcIndex(index));
            return new ArrayValue(newValue);
        }

        public static ArrayValue Remove(ArrayValue self, IValue input)
        {
            var newValue = self.Value.ToList();
            newValue.Remove(input);
            return new ArrayValue(newValue);
        }

        public static ArrayValue RemoveAll(ArrayValue self, IValue input)
        {
            var newValue = self.Value.ToList();
            newValue.RemoveAll(v => v.CompareTo(input) == 0);
            return new ArrayValue(newValue);
        }

        public static bool Contains(IArrayValue self, IValue input)
        {
            return self.ArrayValues.Contains(input);
        }

        public static int IndexOf(IArrayValue self, IValue input)
        {
            var list = self.ArrayValues;
            for (var i = 0; i < list.Count; i++)
            {
                if (input.CompareTo(list[i]) == 0)
                {
                    return i;
                }
            }

            return -1;
        }

        public static ArrayValue SubList(ArrayValue self, int index, int length)
        {
            index = self.CalcIndex(index);
            if (length < 0)
            {
                length = self.Value.Count - index;
            }
            else
            {
                var diff = (index + length) - self.Value.Count;
                if (diff > 0)
                {
                    length -= diff;
                }
            }

            if (index == 0 && length >= self.Value.Count)
            {
                return self;
            }

            var result = new IValue[length];
            for (var i = 0; i < length; i++)
            {
                result[i] = self.Value[i + index];
            }
            return new ArrayValue(result);
        }

        public static int GeneralCompareTo(IArrayValue left, IValue? rightInput)
        {
            if (left == rightInput)
            {
                return 0;
            }

            if (left == null || !(rightInput is IArrayValue right) || left.GetType() != rightInput.GetType())
            {
                return 1;
            }

            var compareLength = left.ArrayValues.Count.CompareTo(right.ArrayValues.Count);
            if (compareLength != 0)
            {
                return compareLength;
            }

            for (var i = 0; i < left.ArrayValues.Count; i++)
            {
                var compare = left.ArrayValues[i].CompareTo(right.ArrayValues[i]);
                if (compare != 0)
                {
                    return compare;
                }
            }

            return 0;
        }

        public static string GeneralToString(IArrayValue input, bool serialise)
        {
            var result = new StringBuilder();
            result.Append('[');
            var first = true;
            foreach (var value in input.ArrayValues)
            {
                if (!first)
                {
                    result.Append(' ');
                }
                first = false;

                result.Append(serialise ? value.ToStringSerialise() : value.ToString());
            }

            result.Append(']');
            return result.ToString();
        }
        #endregion
    }
}