using System.Threading.Tasks;
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
		public async Task<T> LoadAssetAsync<T>(object key)
		{			
			var operation = Addressables.LoadAssetAsync<T>(key);

			await operation.Task;

			if (operation.Status != AsyncOperationStatus.Succeeded)
			{
				throw operation.OperationException;
			}
			
			return operation.Result;
		}
		
		/// <inheritdoc />
		public async Task<GameObject> InstantiateAsync(object key, Transform parent, bool instantiateInWorldSpace)
		{
			return await InstantiatePrefabAsync(key, new InstantiationParameters(parent, instantiateInWorldSpace));
		}

		/// <inheritdoc />
		public async Task<GameObject> InstantiateAsync(object key, Vector3 position, Quaternion rotation, Transform parent)
		{
			return await InstantiatePrefabAsync(key, new InstantiationParameters(position, rotation, parent));
		}

		/// <inheritdoc />
		public void UnloadAsset<T>(T asset)
		{
			Addressables.Release(asset);
		}

		/// <inheritdoc />
		public async Task<Scene> LoadSceneAsync(string path, LoadSceneMode loadMode = LoadSceneMode.Single, bool activateOnLoad = true)
		{
			var operation = Addressables.LoadSceneAsync(path, loadMode, activateOnLoad);
		
			await operation.Task;

			if (operation.Status != AsyncOperationStatus.Succeeded)
			{
				throw operation.OperationException;
			
			}
		
			return operation.Result.Scene;

		}

		/// <inheritdoc />
		public async Task UnloadSceneAsync(Scene scene)
		{
			var operation = SceneManager.UnloadSceneAsync(scene);
			
			await AsyncOperation(operation);
		}

		private async Task AsyncOperation(AsyncOperation operation)
		{
			while (!operation.isDone)
			{
				await Task.Yield();
			}
		}
	
		private async Task<GameObject> InstantiatePrefabAsync(object key, InstantiationParameters instantiateParameters = new InstantiationParameters())
		{
			var operation = Addressables.InstantiateAsync(key, instantiateParameters);

			await operation.Task;

			if (operation.Status != AsyncOperationStatus.Succeeded)
			{
				throw operation.OperationException;
			}
			
			return operation.Result;
		}
	}
}