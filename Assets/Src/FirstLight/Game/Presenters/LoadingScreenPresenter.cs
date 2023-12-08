using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using FirstLight.UiService;
using UnityEngine.UIElements;

namespace FirstLight.Game.Presenters
{
	/// <summary>
	/// This Presenter handles the Loading Screen UI by:
	/// - Showing the loading status
	/// </summary>
	public class LoadingScreenPresenter : UiToolkitPresenter
	{
		protected override void QueryElements(VisualElement root)
		{
			var labelsContainer = root.Q("LabelsContainer").Required();
			labelsContainer.Clear();

#if UNITY_EDITOR || DEVELOPMENT_BUILD

			var services = MainInstaller.Resolve<IGameServices>();

			labelsContainer.Add(new Label(services.GameBackendService.CurrentEnvironmentData.EnvironmentID.ToString()));

			var config = FeatureFlags.GetLocalConfiguration();
			if (config.UseLocalServer)
			{
				labelsContainer.Add(new Label("Local Server"));
			}

			if (config.Tutorial != FlagOverwrite.None)
			{
				labelsContainer.Add(new Label($"Tutorial: {config.Tutorial.Bool()}"));
			}

			if (config.ForceHasNfts)
			{
				labelsContainer.Add(new Label("Have NFTs"));
			}

			if (config.IgnoreEquipmentRequirementForRanked)
			{
				labelsContainer.Add(new Label("Ranked w/o Equip"));
			}
#endif

			labelsContainer.Add(new Label($"v{VersionUtils.VersionExternal}"));
		}
	}
}