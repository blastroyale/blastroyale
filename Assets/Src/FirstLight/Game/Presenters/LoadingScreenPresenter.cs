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
		
		/// <inheritdoc />
		protected override void OnOpened()
		{
			_animation.Rewind();
			_animation.Play();
			_versionText.text = $"v{VersionUtils.VersionExternal}";
			
#if UNITY_EDITOR || DEVELOPMENT_BUILD
			var y = 3;
			var services = MainInstaller.Resolve<IGameServices>();
			AddTextBar(y, services.GameBackendService.CurrentEnvironmentData.EnvironmentID.ToString());
			var config = FeatureFlags.GetLocalConfiguration();
			if (config.UseLocalServer)
			{
				y += 3;
				AddTextBar(y, "Local Server");
			}

			if (config.Tutorial!=FlagOverwrite.None)
			{
				y += 3;
				AddTextBar(y, "Tuto: "+config.Tutorial.Bool());
			}

			if (config.ForceHasNfts)
			{
				y += 3;
				AddTextBar(y,"Have NFTs");
			}
			if (config.IgnoreEquipmentRequirementForRanked)
			{
				y += 3;
				AddTextBar(y,"Ranked w/o Equip");
			}
#endif
		}

		private void AddTextBar(int heightMod, string text)
		{
			var original = _versionText.transform.parent.gameObject;
			var parent = original.transform.parent;
			
			var newBar = Instantiate(original, parent);
			var position = newBar.transform.position;
			position = new Vector3(position.x, position.y+ heightMod,
				position.z);
			newBar.transform.position = position;
			newBar.GetComponentInChildren<TextMeshProUGUI>().text = text;
		}
		
		/// <inheritdoc />
		protected override Task OnClosed()
		{
			return Task.CompletedTask;
		}
	}
}