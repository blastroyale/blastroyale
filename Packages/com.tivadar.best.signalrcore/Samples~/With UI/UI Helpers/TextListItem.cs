using UnityEngine;
using UnityEngine.UI;

namespace Best.SignalR.Examples.Helpers
{
    class TextListItem : MonoBehaviour
    {
#pragma warning disable 0649
        [SerializeField]
        private Text _text;
#pragma warning restore

        public void SetText(string text)
        {
            this._text.text = text;
        }

        public void AddLeftPadding(int padding)
        {
            this.GetComponent<LayoutGroup>().padding.left += padding;
        }
    }
}
