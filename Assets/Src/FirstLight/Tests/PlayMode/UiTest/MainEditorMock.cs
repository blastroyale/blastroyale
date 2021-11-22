using FirstLight.Game;
using FirstLight.UiService;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace FirstLight.Tests.PlayMode
{
	/// <summary>
	/// This object is only to mock the main scene to test different solo scenarios
	/// </summary>
	public class MainEditorMock : Main
	{
		[HideInInspector]
		public int IndexConfig;
		public UiConfigs Configs;
		
#pragma warning disable 109
		private new void Start() {}
#pragma warning restore 109

		public async void Open()
		{
			var config = Configs.Configs[IndexConfig];
			var ui = await Addressables.InstantiateAsync(config.AddressableAddress).Task;

			ui.GetComponent(config.UiType).SendMessage("OnOpened");
		}

		public void Close(UiPresenter presenter)
		{
			presenter.SendMessage("OnClosed");
		}
	}
	
	#if UNITY_EDITOR
	[UnityEditor.CustomEditor(typeof(MainEditorMock), true)]
	public class MainEditorMockEditor : UnityEditor.Editor
	{
		public override void OnInspectorGUI()
		{
			DrawDefaultInspector();
			
			var helper = (MainEditorMock)target;
			var options = new string[helper.Configs.Configs.Count];

			for (var i = 0; i < helper.Configs.Configs.Count; i++)
			{
				options[i] = helper.Configs.Configs[i].UiType.Name;
			}

			helper.IndexConfig = UnityEditor.EditorGUILayout.Popup(helper.IndexConfig, options);
			
			if (GUILayout.Button("Open Selected UiConfig"))
			{
				helper.Open();
			}
			if (GUILayout.Button("Close Opened UiConfig"))
			{
				helper.Close(FindObjectOfType<UiPresenter>());
			}
		}
	}
	#endif
}