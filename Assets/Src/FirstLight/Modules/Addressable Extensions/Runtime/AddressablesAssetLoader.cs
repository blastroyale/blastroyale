using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using FirstLight.AssetImporter;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

// ReSharper disable once CheckNamespace

namespace FirstLight.AddressablesExtensions
{
	/// <summary>
	/// The asset loader to use with Addressables
	/// </summary>
	public class AddressablesAssetLoader : IAssetLoader, ISceneLoader
	{
		/// <inheritdoc />
		public async UniTask<T> LoadAssetAsync<T>(object key)
		{			
			var operation = Addressables.LoadAssetAsync<T>(key);
			await operation.ToUniTask();

			if (operation.Status != AsyncOperationStatus.Succeeded)
			{
				throw operation.OperationException;
			}
			
			return operation.Result;
		}
		
		/// <inheritdoc />
		public async UniTask<GameObject> InstantiateAsync(object key, Transform parent, bool instantiateInWorldSpace)
		{
			return await InstantiatePrefabAsync(key, new InstantiationParameters(parent, instantiateInWorldSpace));
		}

		/// <inheritdoc />
		public async UniTask<GameObject> InstantiateAsync(object key, Vector3 position, Quaternion rotation, Transform parent)
		{
			return await InstantiatePrefabAsync(key, new InstantiationParameters(position, rotation, parent));
		}

		/// <inheritdoc />
		public void UnloadAsset<T>(T asset)
		{
			Addressables.Release(asset);
		}

		/// <inheritdoc />
		public async UniTask<Scene> LoadSceneAsync(string path, LoadSceneMode loadMode = LoadSceneMode.Single, bool activateOnLoad = true)
		{
			var operation = Addressables.LoadSceneAsync(path, loadMode, activateOnLoad);
		
			await operation.Task.AsUniTask();

			if (operation.Status != AsyncOperationStatus.Succeeded)
			{
				throw operation.OperationException;
			
			}
		
			return operation.Result.Scene;

		}

		public async UniTask<Scene> LoadSceneAsync(AssetReferenceScene reference, LoadSceneMode loadMode = LoadSceneMode.Single, bool activateOnLoad = true)
		{
			var operation = Addressables.LoadSceneAsync(reference, loadMode, activateOnLoad);
		
			await operation.Task;

			if (operation.Status != AsyncOperationStatus.Succeeded)
			{
				throw operation.OperationException;
			
			}
		
			return operation.Result.Scene;
		}

		/// <inheritdoc />
		public async UniTask UnloadSceneAsync(Scene scene)
		{
			await SceneManager.UnloadSceneAsync(scene).ToUniTask();
		}

		private async UniTask<GameObject> InstantiatePrefabAsync(object key, InstantiationParameters instantiateParameters = new InstantiationParameters())
		{
			var operation = Addressables.InstantiateAsync(key, instantiateParameters);

			await operation.ToUniTask();

			if (operation.Status != AsyncOperationStatus.Succeeded)
			{
				throw operation.OperationException;
			}
			
			return operation.Result;
		}
	}
}