using Best.HTTP.Examples.Helpers;

using UnityEngine;
using UnityEngine.UI;

namespace Best.HTTP.Examples
{
    class TextWithImageListItem : MultiTextListItem
    {
#pragma warning disable 0649
        [SerializeField]
        protected RawImage _image;
#pragma warning restore

        public new TextWithImageListItem SetStatusText(string text)
        {
            this._statusText.text = text;
            return this;
        }

        public TextWithImageListItem SetImage(Texture2D texture)
        {
            this._image.texture = texture;
            return this;
        }
    }
}
