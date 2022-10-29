import Scope, { IReadOnlyScope } from "../scope";
import BuiltinFunctionValue from "../values/builtinFunctionValue";
import ObjectValue, { ObjectValueMap } from "../values/objectValue";

export const assertScope: IReadOnlyScope = createAssertScope();

export function createAssertScope()
{
    const result: Scope = new Scope();

    const assertFunctions: ObjectValueMap =
    {
        true: new BuiltinFunctionValue((vm, args) =>
        {
            const top = args.getBool(0);
            if (!top)
            {
                vm.running = false;
                console.error(vm.createStackTrace().join('\n'));
                console.error('Assert expected true');
            }
        }),

        false: new BuiltinFunctionValue((vm, args) =>
        {
            const top = args.getBool(0);
            if (top)
            {
                vm.running = false;
                console.error(vm.createStackTrace().join('\n'));
                console.error('Assert expected false');
            }
        }),

        equals: new BuiltinFunctionValue((vm, args) =>
        {
            const expected = args.get(0);
            const actual = args.get(1);
            if (expected.compareTo(actual) !== 0)
            {
                vm.running = false;
                console.error(vm.createStackTrace().join('\n'));
                console.error(`Assert expected equals:\nExpected: ${expected.toString()}\nActual: ${actual.toString()}`);
            }
        }),

        notEquals: new BuiltinFunctionValue((vm, args) =>
        {
            const expected = args.get(0);
            const actual = args.get(1);
            if (expected.compareTo(actual) === 0)
            {
                vm.running = false;
                console.error(vm.createStackTrace().join('\n'));
                console.error(`Assert expected not equals:\nActual: ${actual.toString()}`);
            }
        })
    };

    result.define('assert', new ObjectValue(assertFunctions));

    return result;
}