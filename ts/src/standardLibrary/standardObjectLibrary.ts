import Scope, { IReadOnlyScope } from "../scope";
import { Editable } from "../types";
import ArrayValue from "../values/arrayValue";
import BuiltinFunctionValue from "../values/builtinFunctionValue";
import { IValue } from "../values/ivalues";
import NullValue from "../values/nullValue";
import ObjectValue, { isObjectValue, ObjectValueMap } from "../values/objectValue";
import StringValue from "../values/stringValue";

export const objectScope: IReadOnlyScope = createObjectScope();

export function createObjectScope()
{
    const result = new Scope();

    const objectFunctions: ObjectValueMap =
    {
        set: new BuiltinFunctionValue((vm, args) =>
        {
            const top = args.getCast(0, isObjectValue);
            const key = args.getString(1);
            const value = args.get(2);
            vm.pushStack(set(top, key, value));
        }),

        get: new BuiltinFunctionValue((vm, args) =>
        {
            const top = args.getCast(0, isObjectValue);
            const key = args.getString(1);
            const found = get(top, key);
            if (found !== undefined)
            {
                vm.pushStack(found);
            }
            else
            {
                vm.pushStack(NullValue.Value);
            }
        }),

        removeKey: new BuiltinFunctionValue((vm, args) =>
        {
            const obj = args.getCast(0, isObjectValue);
            const key = args.getString(1);
            vm.pushStack(removeKey(obj, key));
        }),

        removeValues: new BuiltinFunctionValue((vm, args) =>
        {
            const obj = args.getCast(0, isObjectValue);
            const values = args.get(1);
            vm.pushStack(removeValues(obj, values));
        }),

        keys: new BuiltinFunctionValue((vm, args) =>
        {
            const top = args.getCast(0, isObjectValue);
            vm.pushStack(keys(top));
        }),

        values: new BuiltinFunctionValue((vm, args) =>
        {
            const top = args.getCast(0, isObjectValue);
            vm.pushStack(values(top));
        }),

        length: new BuiltinFunctionValue((vm, args) =>
        {
            const top = args.getCast(0, isObjectValue);
            vm.pushStackNumber(length(top));
        })
    }

    result.define('object', new ObjectValue(objectFunctions));

    return result;
}

export function set(target: ObjectValue, key: string, value: IValue)
{
    return new ObjectValue({ ...target.value, [key]: value });
}

export function get(target: ObjectValue, key: string) : IValue | undefined
{
    const result = target.value[key];
    return result != null ? result : undefined;
}

export function keys(target: ObjectValue): ArrayValue
{
    return new ArrayValue(target.keys.map(s => new StringValue(s)));
}

export function removeKey(target: ObjectValue, key: string): ObjectValue
{
    if (!target.hasOwnProperty(key))
    {
        return target;
    }

    const result = { ...target.value };
    delete result[key];
    return new ObjectValue(result);
}

export function removeValues(target: ObjectValue, values: IValue): ObjectValue
{
    const result: Editable<ObjectValueMap> = {};
    for (const key in target)
    {
        if (target.value[key].compareTo(values) !== 0)
        {
            result[key] = target.value[key];
        }
    }

    return new ObjectValue(result);
}

export function values(target: ObjectValue): ArrayValue
{
    return new ArrayValue(Object.values(target.value));
}

export function length(target: ObjectValue)
{
    return target.keys.length;
}