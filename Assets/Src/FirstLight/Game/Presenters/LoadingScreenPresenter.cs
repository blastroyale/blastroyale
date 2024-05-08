using FirstLight.Game.Utils;
using FirstLight.UIService;
using UnityEngine.UIElements;

namespace FirstLight.Game.Presenters
{
	/// <summary>
	/// This Presenter handles the Loading Screen UI by:
	/// - Showing the loading status
	/// </summary>
	public class LoadingScreenPresenter : UIPresenter
	{
		protected override void QueryElements()
		{
			var labelsContainer = Root.Q("LabelsContainer").Required();
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
	}
}