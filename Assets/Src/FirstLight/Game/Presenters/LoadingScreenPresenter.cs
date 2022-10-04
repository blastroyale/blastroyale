using System;
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
		[SerializeField, Required] private Slider _loadingBar;
		[SerializeField, Required] private TextMeshProUGUI _loadingBarText;

		/// <summary>
		/// Requests the loading game percentage value
		/// </summary>
		public float LoadingPercentage => _loadingBar.value;

		private void Awake()
		{
			_loadingBarText.text = VersionUtils.VersionExternal;
		}

		/// <summary>
		/// Sets the loading screen to the given <paramref name="percentage"/>
		/// </summary>
		public void SetLoadingPercentage(float percentage)
		{
			_loadingBar.value = percentage;
		}

		/// <inheritdoc />
		protected override void OnOpened()
		{
			SetLoadingPercentage(0);
			_animation.Rewind();
			_animation.Play();

		}
		
		/// <inheritdoc />
		protected override void OnClosed()
		{
			SetLoadingPercentage(1f);
		}
	}
}