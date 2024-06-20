using FirstLight.Game.Ids;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace FirstLight.Game.MonoComponent
{
	/// <summary>
	/// This mono component is responsible to dynamically load/unload the defined <see cref="AddressableId"/> at runtime
	/// </summary>
	public class AddressableMonoComponent : MonoBehaviour
	{
		[SerializeField] private AddressableId _addressable;

		private GameObject _gameObject;

		private async void Awake()
		{
			_gameObject = await Addressables.InstantiateAsync(_addressable.GetConfig().Address, transform).Task;
		}

		private void OnDestroy()
		{
			if (_gameObject != null)
			{
				Addressables.ReleaseInstance(_gameObject);
			}
		}
	}
}