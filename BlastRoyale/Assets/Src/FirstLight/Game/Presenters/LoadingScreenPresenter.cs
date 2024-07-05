using FirstLight.Game.Services;
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
		private VisualElement _labelsContainer;
		private IVisualElementScheduledItem _environmentRedirectTask;

		private void Start()
		{
			Instance = gameObject;
			var root = _document.rootVisualElement;

			_labelsContainer = root.Q("LabelsContainer").Required();
			_labelsContainer.Clear();

			_environmentRedirectTask = _labelsContainer.schedule.Execute(CheckEnvironmentRedirect).Every(1000);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
			_labelsContainer.Add(new Label(FLEnvironment.Current.Name));

			var config = FeatureFlags.GetLocalConfiguration();
			if (config.UseLocalServer)
			{
				_labelsContainer.Add(new Label("Local Server"));
			}

			if (config.Tutorial != FlagOverwrite.None)
			{
				_labelsContainer.Add(new Label($"Tutorial: {config.Tutorial.Bool()}"));
			}

#endif

			_labelsContainer.Add(new Label($"v{VersionUtils.VersionExternal}"));
		}

		private void CheckEnvironmentRedirect(TimerState obj)
		{
			if (MainInstaller.TryResolve<IGameServices>(out var services))
			{
				if (services.GameBackendService.ForcedEnvironment)
				{
					var lbl = new Label("Release");
					lbl.style.color = Color.magenta;
					_labelsContainer.Add(lbl);
					_environmentRedirectTask.Pause();
				}
			}
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