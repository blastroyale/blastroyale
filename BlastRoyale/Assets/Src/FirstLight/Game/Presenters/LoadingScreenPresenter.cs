using FirstLight.Game.Utils;
using UnityEngine;
using UnityEngine.UIElements;

namespace FirstLight.Game.Presenters
{
	/// <summary>
	/// This Presenter handles the Loading Screen UI by:
	/// - Showing the loading status
	///
	/// This is intentionally not a UIPresenter as it's shown before services.
	/// </summary>
	public class LoadingScreenPresenter : MonoBehaviour
	{
		[SerializeField] private UIDocument _document;

		private static GameObject Instance;

		private void Start()
		{
			Instance = gameObject;
			var root = _document.rootVisualElement;

			var labelsContainer = root.Q("LabelsContainer").Required();
			labelsContainer.Clear();

#if UNITY_EDITOR || DEVELOPMENT_BUILD
			labelsContainer.Add(new Label(FLEnvironment.Current.Name));

			var config = FeatureFlags.GetLocalConfiguration();
			if (config.UseLocalServer)
			{
				labelsContainer.Add(new Label("Local Server"));
			}

			if (config.Tutorial != FlagOverwrite.None)
			{
				labelsContainer.Add(new Label($"Tutorial: {config.Tutorial.Bool()}"));
			}

#endif

			labelsContainer.Add(new Label($"v{VersionUtils.VersionExternal}"));
		}

		public static void Destroy()
		{
			if (Instance != null)
			{
				Destroy(Instance);
				Instance = null;
			}
		}
	}
}