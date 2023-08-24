using System.Threading.Tasks;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using FirstLight.UiService;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;


namespace FirstLight.Game.Presenters
{
	/// <summary>
	/// This Presenter handles the Loading Screen UI by:
	/// - Showing the loading status
	/// </summary>
	public class LoadingScreenPresenter : UiPresenter
	{
		[SerializeField, Required] private Animation _animation;
		[SerializeField, Required] private TextMeshProUGUI _versionText;
		
		private readonly int _newBarMod = 25;
		private int _customBarsY = 0;
		
		/// <inheritdoc />
		protected override void OnOpened()
		{
			_animation.Rewind();
			_animation.Play();
			_versionText.text = $"v{VersionUtils.VersionExternal}";
#if UNITY_EDITOR || DEVELOPMENT_BUILD
			var services = MainInstaller.Resolve<IGameServices>();
			AddTextBar(services.GameBackendService.CurrentEnvironmentData.EnvironmentID.ToString());
			var config = FeatureFlags.GetLocalConfiguration();
			if (config.UseLocalServer)
			{
				AddTextBar("Local Server");
			}

			if (config.Tutorial!=FlagOverwrite.None)
			{
				AddTextBar("Tuto: "+config.Tutorial.Bool());
			}

			if (config.ForceHasNfts)
			{
				AddTextBar("Have NFTs");
			}
			if (config.IgnoreEquipmentRequirementForRanked)
			{
				AddTextBar("Ranked w/o Equip");
			}
#endif
			if (FeatureFlags.BETA_VERSION)
			{
				AddTextBar("BETA", 2);
			}
		}

		private void AddTextBar(string text, float size = 1)
		{
			var original = _versionText.transform.parent.gameObject;
			var parent = original.transform.parent;
			
			var newBar = Instantiate(original, parent);
			var position = newBar.transform.position;
			_customBarsY += _newBarMod; 
			position = new Vector3(position.x, position.y + (_customBarsY + (_newBarMod * (size - 1f))),
				position.z);
			newBar.transform.position = position;
			newBar.GetComponentInChildren<TextMeshProUGUI>().text = text;
			newBar.transform.localScale = new Vector3(size, size, size);
		}
		
		/// <inheritdoc />
		protected override Task OnClosed()
		{
			return Task.CompletedTask;
		}
	}
}