using System;
using System.Collections.Generic;
using UnityEngine;

namespace SimpleStackVM.Unity
{
    public static class DrawingLibrary
    {
        #region Fields
        public static readonly IReadOnlyScope Scope = CreateScope();
        #endregion

        #region Methods
        public static Scope CreateScope()
        {
            var result = new Scope();

            var drawingFunctions = new Dictionary<string, IValue>
            {
                {"element", new BuiltinFunctionValue((vm, args) =>
                {
                    var elementName = args.GetIndex<StringValue>(0);
                    var position = args.GetIndex(1, Vector3Value.Cast);
                    var colour = args.GetIndex(2, ColourValue.Cast);
                    var scale = args.GetIndex(3, Vector3Value.Cast);

                    DrawingContext.Instance.DrawElement(elementName.Value, position.Value, colour.Value, scale.Value);
                })},

                {"clear", new BuiltinFunctionValue((vm, numArgs) =>
                {
                    DrawingContext.Instance.ClearScene();
                })}
            };

            result.Define("draw", new ObjectValue(drawingFunctions));

            return result;
        }
        #endregion
    }
}