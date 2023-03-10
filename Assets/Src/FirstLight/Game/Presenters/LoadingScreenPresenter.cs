using System;
using System.Threading.Tasks;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using FirstLight.UiService;
using I2.Loc;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
			
#if !STORE_BUILD
			var bar = GameObject.Find("InfoText");
			bar.transform.localScale = new Vector3(1.5f, 1, 1);
			var config = FeatureFlags.GetLocalConfiguration();
			if (config.UseLocalServer)
			{
				_versionText.text += " [LOCAL SERVER]";
			}
			else
			{
				var services = MainInstaller.Resolve<IGameServices>();
				var env = services.GameBackendService.CurrentEnvironmentData.EnvironmentID;
				_versionText.text += $" [Env: {env}]";
				
			}
			
#endif
		}
		
		/// <inheritdoc />
		protected override Task OnClosed()
		{
			return Task.CompletedTask;
		}
	}
}