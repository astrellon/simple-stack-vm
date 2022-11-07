using UnityEngine;

namespace SimpleStackVM.Unity
{
    [CreateAssetMenu(fileName="DialogueScript", menuName="SimpleStackVM/DialogueScript")]
    public class DialogueScript : ScriptableObject
    {
        #region Fields
        public TextAsset CodeText;

        private Script script = Script.Empty;
        public Script Script
        {
            get
            {
                if (!this.script.Code.IsEmpty)
                {
                    return this.script;
                }

                var codeString = this.CodeText.text;
                this.script = DialogueVM.Instance.AssembleScript(codeString);
                return this.script;
            }
        }
        #endregion
    }
}