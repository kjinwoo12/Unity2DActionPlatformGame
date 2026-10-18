using UnityEngine;

namespace Bastion.Dialogue
{
    /// <summary>대화 한 묶음. 말하는 사람 이름과 차례로 보여 줄 문장들을 에셋으로 저장한다.</summary>
    [CreateAssetMenu(fileName = "DialogueData", menuName = "Bastion/Dialogue Data")]
    public sealed class DialogueData : ScriptableObject
    {
        public string SpeakerName;
        [TextArea(2, 4)] public string[] Lines;
    }
}
