using Best.HTTP.Examples.Helpers.SelectorUI;

using UnityEngine;

namespace Best.HTTP.Examples.Helpers
{
    abstract class SampleBase : MonoBehaviour
    {
#pragma warning disable 0649, 0169
        [Header("Common Properties")]
        public string Category;
        public string DisplayName;

        [TextArea]
        public string Description;
#pragma warning restore

        public RuntimePlatform[] BannedPlatforms = new RuntimePlatform[0];

        protected SampleSelectorUI sampleSelector;

        protected virtual void Start()
        {
#if UNITY_2023_1_OR_NEWER
            this.sampleSelector = FindAnyObjectByType<SampleSelectorUI>();
#else
            this.sampleSelector = FindObjectOfType<SampleSelectorUI>();
#endif
        }
    }
}
