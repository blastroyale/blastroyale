using UnityEngine;
using UnityEngine.UI;

namespace Best.TLSSecurity.Examples.BatchedTest
{
    public class KeyValueLine : MonoBehaviour
    {
        public Text KeyText;
        public Text ValueText;

        public Color ColorSuccess;
        public Color ColorFail;
        public Color ColorWarning;

        public void Init(string key, string value)
        {
            this.KeyText.text = key;
            this.ValueText.text = value;
        }

        public void SetSuccess(string value)
        {
            this.ValueText.color = this.ColorSuccess;
            this.ValueText.text = value;
        }

        public void SetFailed(string value)
        {
            this.KeyText.color = this.ValueText.color = this.ColorFail;

            this.ValueText.text = value;
        }

        public void SetWarning(string value)
        {
            this.ValueText.color = this.ColorWarning;
            this.ValueText.text = value;
        }

        public void SetAsHeader(string value)
        {
            this.KeyText.text = value;
            this.KeyText.alignment = TextAnchor.MiddleCenter;
            this.KeyText.fontSize = 24;

            Destroy(this.ValueText.gameObject);
        }
    }
}
