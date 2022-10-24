using System;
using System.Collections.Generic;
using UnityEngine;

namespace SimpleStackVM.Unity
{
    public class DialogueVM : MonoBehaviour
    {
        public enum SectionType
        {
            NewLine, DialogueEnded, ForChoices, ToContinue
        }

        [Serializable]
        public class ActorPair
        {
            public string ScriptId;
            public DialogueActor Actor;
        }

        public delegate void TextSegmentHandler(string text);
        public delegate void ShowChoiceHandler(string text, int index);
        public delegate void SectionHandler(SectionType sectionType);
        public delegate void EmotionHandler(string emotion);

        public event TextSegmentHandler OnTextSegment;
        public event ShowChoiceHandler OnShowChoice;
        public event SectionHandler OnSectionChange;
        public event EmotionHandler OnEmotion;

        public static DialogueVM Instance;

        public DialogueActor CurrentActor { get; private set; }
        public VMRunner VMRunner;
        public List<ActorPair> Actors;

        private VirtualMachineAssembler assembler;
        private VirtualMachine vm => this.VMRunner.VM;
        private readonly List<IFunctionValue> choiceBuffer = new List<IFunctionValue>();

        // Start is called before the first frame update
        void Awake()
        {
            Instance = this;

            this.assembler = this.CreateAssembler();
            this.VMRunner.Init(32);
            this.VMRunner.OnComplete += (runner) =>
            {
                this.OnSectionChange?.Invoke(SectionType.DialogueEnded);
            };
        }

        public Script AssembleScript(string text)
        {
            return this.assembler.ParseFromText(text);
        }

        public void StartDialogue(DialogueScript dialogue, DialogueActor selfActor)
        {
            this.vm.GlobalScope.Clear();
            foreach (var actorPair in this.Actors)
            {
                this.vm.GlobalScope.Define(actorPair.ScriptId, new DialogueActorValue(actorPair.Actor));
            }
            this.vm.GlobalScope.Define("SELF", new DialogueActorValue(selfActor));

            this.VMRunner.StartScript(dialogue.Script);
        }

        public void Continue()
        {
            if (choiceBuffer.Count > 0)
            {
                Debug.Log("Cannot continue, needs to make a choice.");
                return;
            }

            vm.Paused = false;
        }

        public void SelectChoice(int index)
        {
            if (index < 0 || index >= this.choiceBuffer.Count)
            {
                return;
            }

            var choiceFunc = this.choiceBuffer[index];
            Debug.Log($"Selecting choice: {index}, {choiceFunc.ToString()}");
            this.vm.CallFunction(choiceFunc, 0, false);
            this.vm.Paused = false;
        }

        public void CreateChoice(string choiceLabel, IFunctionValue choiceValue)
        {
            var index = this.choiceBuffer.Count;
            this.choiceBuffer.Add(choiceValue);
            this.OnShowChoice?.Invoke(choiceLabel, index);
        }

        private VirtualMachineAssembler CreateAssembler()
        {
            var assembler = new VirtualMachineAssembler();
            StandardLibrary.AddToScope(assembler.BuiltinScope);
            assembler.BuiltinScope.CombineScope(UnityLibrary.Scope);

            assembler.BuiltinScope.Define("actor", new BuiltinFunctionValue(this.ActorFunc));
            assembler.BuiltinScope.Define("emotion", new BuiltinFunctionValue(this.EmotionFunc));
            assembler.BuiltinScope.Define("beginLine", new BuiltinFunctionValue(this.BeginLineFunc));
            assembler.BuiltinScope.Define("text", new BuiltinFunctionValue(this.TextFunc));
            assembler.BuiltinScope.Define("endLine", new BuiltinFunctionValue(this.EndLineFunc));
            assembler.BuiltinScope.Define("choice", new BuiltinFunctionValue(this.ChoiceFunc));
            assembler.BuiltinScope.Define("wait", new BuiltinFunctionValue(this.WaitFunc));
            assembler.BuiltinScope.Define("moveTo", new BuiltinFunctionValue(this.MoveToFunc));

            return assembler;
        }

        public void MoveToFunc(VirtualMachine vm, ArgumentsValue args)
        {
            var proc = args.Get<IFunctionValue>(0);
            var label = args.Get(1);

            vm.CallFunction(proc, 0, false);
            vm.Jump(label.ToString());
        }

        public void BeginLineFunc(VirtualMachine vm, ArgumentsValue args)
        {
            this.choiceBuffer.Clear();
            this.OnSectionChange?.Invoke(SectionType.NewLine);
        }

        public void EndLineFunc(VirtualMachine vm, ArgumentsValue args)
        {
            vm.Paused = true;
            this.OnSectionChange?.Invoke(this.choiceBuffer.Count > 0 ? SectionType.ForChoices : SectionType.ToContinue);
        }

        private void TextFunc(VirtualMachine vm, ArgumentsValue args)
        {
            var text = string.Join("", args.Value);
            this.OnTextSegment?.Invoke(text);
        }

        private void ActorFunc(VirtualMachine vm, ArgumentsValue args)
        {
            this.CurrentActor = args.Get<DialogueActorValue>(0).Value;
            this.OnEmotion?.Invoke("idle");
        }

        private void ChoiceFunc(VirtualMachine vm, ArgumentsValue args)
        {
            var choiceLabel = args.Get<StringValue>(0);
            var choiceValue = args.Get<IFunctionValue>(1);
            this.CreateChoice(choiceLabel.ToString(), choiceValue);
        }

        private void EmotionFunc(VirtualMachine vm, ArgumentsValue args)
        {
            var emotion = args.Get(0).ToString();
            this.OnEmotion?.Invoke(emotion);
        }

        private void WaitFunc(VirtualMachine vm, ArgumentsValue args)
        {
            var waitTime = TimeSpan.FromMilliseconds(args.Get<NumberValue>(0).Value);
            this.VMRunner.Wait(waitTime);
        }
    }
}
