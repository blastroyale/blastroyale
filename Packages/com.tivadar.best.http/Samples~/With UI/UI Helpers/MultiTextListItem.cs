using UnityEngine;
using UnityEngine.UI;

namespace Best.HTTP.Examples.Helpers
{
    class MultiTextListItem : TextListItem
    {
#pragma warning disable 0649
        [SerializeField]
        protected Text _statusText;
#pragma warning restore

        public new MultiTextListItem SetText(string text)
        {
            this._text.text = text;
            return this;
        }

        public MultiTextListItem SetStatusText(string text)
        {
            this._statusText.text = text;
            return this;
        }
    }
}
