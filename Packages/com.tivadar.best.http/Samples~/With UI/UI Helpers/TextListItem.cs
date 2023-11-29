using UnityEngine;
using UnityEngine.UI;

namespace Best.HTTP.Examples.Helpers
{
    class TextListItem : MonoBehaviour
    {
#pragma warning disable 0649
        [SerializeField]
        protected Text _text;
#pragma warning restore

        public TextListItem SetText(string text)
        {
            this._text.text = text;
            return this;
        }

        public TextListItem AddLeftPadding(int padding)
        {
            this.GetComponent<LayoutGroup>().padding.left += padding;
            return this;
        }
    }
}
