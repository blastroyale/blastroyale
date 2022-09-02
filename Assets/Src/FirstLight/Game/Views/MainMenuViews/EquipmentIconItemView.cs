using FirstLight.Game.Ids;
using FirstLight.Game.Logic;
using FirstLight.Game.Utils;
using Sirenix.OdinInspector;
using UnityEngine;

namespace FirstLight.Game.Views.MainMenuViews
{
	/// <summary>
	/// This view is responsible to show the item icons and information
	/// </summary>
	public class EquipmentIconItemView : MonoBehaviour
	{
		[SerializeField, Required] private RemoteTextureView _remoteTextureView;

		private IGameDataProvider _gameDataProvider;

		private void Awake()
		{
			_gameDataProvider = MainInstaller.Resolve<IGameDataProvider>();
		}

		/// <summary>
		/// Sets the information for this view
		/// </summary>
		public void SetInfo(UniqueId uniqueId)
		{
			if (_gameDataProvider.EquipmentDataProvider.NftInventory.ContainsKey(uniqueId))
			{
				var url = _gameDataProvider.EquipmentDataProvider.GetNftInfo(uniqueId).SafeImageUrl;
				_remoteTextureView.LoadImage(url);
			}
			else
			{
				_remoteTextureView.LoadImage(null);
			}
		}
	}
}