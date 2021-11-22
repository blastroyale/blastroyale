using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;

// ReSharper disable CheckNamespace

namespace FirstLight.UiService
{
	/// <summary>
	/// This interface allows to wrap the asset loading scheme into the UI memory
	/// </summary>
	public interface IUiAssetLoader
	{
		/// <summary>
		/// Loads and instantiates the prefab in the given <paramref name="path"/> with the given <paramref name="parent"/>
		/// and the given <paramref name="instantiateInWorldSpace"/> to preserve the instance transform relative to world
		/// space or relative to the parent.
		/// To help the execution of this method is recommended to request the asset path from an <seealso cref="AddressableConfig"/>.
		/// This method can be controlled in an async method and returns the prefab instantiated
		/// </summary>
		Task<GameObject> InstantiatePrefabAsync(string path, Transform parent, bool instantiateInWorldSpace);

		/// <summary>
		/// Unloads the given <paramref name="asset"/> from the game memory
		/// </summary>
		void UnloadAsset(GameObject asset);
	}

	/// <inheritdoc />
	public class UiAssetLoader : IUiAssetLoader
	{
		/// <inheritdoc />
		public async Task<GameObject> InstantiatePrefabAsync(string path, Transform parent, bool instantiateInWorldSpace)
		{
			var operation = Addressables.InstantiateAsync(path, new InstantiationParameters(parent, instantiateInWorldSpace));

			await operation.Task;

			if (operation.Status != AsyncOperationStatus.Succeeded)
			{
				throw operation.OperationException;
			}
			
			return operation.Result;
		}

		/// <inheritdoc />
		public void UnloadAsset(GameObject asset)
		{
			Addressables.ReleaseInstance(asset);
		}
	}
}