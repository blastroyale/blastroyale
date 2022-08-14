using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using FirstLight.AddressablesExtensions;
using FirstLight.AssetImporter;
using FirstLight.FLogger;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace FirstLight.Game.Services
{
	/// <summary>
	/// Service to extend the behaviour to load assets into the project based on it's own custom needs
	/// </summary>
	public interface IAssetResolverService : IAssetLoader, ISceneLoader
	{
		/// <summary>
		/// Requests if exists the <see cref="AssetReference"/> representing the given <paramref name="id"/> of the
		/// given <typeparamref name="TAsset"/> type.
		/// </summary>
		bool TryGetAssetReference<TId, TAsset>(TId id, out AssetReference assetReference) where TId : struct;

		/// <summary>
		/// Requests the given <typeparamref name="TAsset"/> of the given <paramref name="id"/>.
		/// If <paramref name="loadAsynchronously"/> is true then will load asynchronously.
		/// It will also return the result in the provided <paramref name="onLoadCallback"/> when the loading is complete
		/// and will instantiate the asset if the given <paramref name="instantiate"/> is true
		/// </summary>
		Task<TAsset> RequestAsset<TId, TAsset>(TId id, bool loadAsynchronously = true, bool instantiate = true,
		                                       Action<TId, TAsset, bool> onLoadCallback = null)
			where TId : struct
			where TAsset : Object;

		/// <summary>
		/// Requests the given <typeparamref name="TAsset"/> of the given <paramref name="id"/> while passing the
		/// given <paramref name="data"/> to the <paramref name="onLoadCallback"/> call.
		/// If <paramref name="loadAsynchronously"/> is true then will load asynchronously.
		/// It will also return the result in the provided <paramref name="onLoadCallback"/> when the loading is complete
		/// and will instantiate the asset if the given <paramref name="instantiate"/> is true
		/// </summary>
		Task<TAsset> RequestAsset<TId, TAsset, TData>(TId id, TData data, bool loadAsynchronously = true,
		                                              bool instantiate = true,
		                                              Action<TId, TAsset, TData, bool> onLoadCallback = null)
			where TId : struct
			where TAsset : Object;

		/// <summary>
		/// Loads asynchronously a <see cref="Scene"/> mapped with the given <paramref name="id"/> and the given info.
		/// It will also return the result in the provided <paramref name="onLoadCallback"/> when the loading is complete
		/// </summary>
		Task<SceneInstance> LoadScene<TId>(TId id, LoadSceneMode loadMode = LoadSceneMode.Single,
		                                   bool activateOnLoad = true,
		                                   bool setActive = true, Action<TId, SceneInstance> onLoadCallback = null)
			where TId : struct;
		
		
		/// <summary>
		/// Loads all assets previously added from <seealso cref="IAssetAdderService.AddConfigs{TId,TAsset}(FirstLight.AssetImporter.AssetConfigsScriptableObject{TId,TAsset})"/>
		/// Has the option from the params to <paramref name="loadAsynchronously"/> and have a <paramref name="onLoadCallback"/>
		/// for each asset being loaded.
		/// Returns the full list of assets loaded into memory.
		/// </summary>
		/// <remarks>
		/// Will require to call <see cref="UnloadAssets{TId,TAsset}(bool,FirstLight.AssetImporter.AssetConfigsScriptableObject{TId,TAsset})"/>
		/// to clean the assets from memory again
		/// </remarks>
		Task<List<Pair<TId, TAsset>>> LoadAllAssets<TId, TAsset>(bool loadAsynchronously = true,
		                                                         Action<TId, TAsset> onLoadCallback = null)
			where TId : struct;

		/// <summary>
		/// Unloads asynchronously a <see cref="Scene"/> mapped with the given <paramref name="id"/>.
		/// It will also return the result in the provided <paramref name="onUnloadCallback"/> when the loading is complete
		/// </summary>
		Task UnloadScene<TId>(TId id, Action<TId, SceneInstance> onUnloadCallback = null)
			where TId : struct;

		/// <summary>
		/// Unloads all the asset reference of the given <typeparamref name="TId"/> type.
		/// If the given <paramref name="clearReferences"/> is true then will also removes any reference to the assets
		/// </summary>
		void UnloadAssets<TId, TAsset>(bool clearReferences)
			where TId : struct;

		/// <summary>
		/// Unloads the asset reference of the given <typeparamref name="TId"/> from the given <paramref name="assetConfigs"/>.
		/// If the given <paramref name="clearReferences"/> is true then will also removes any reference to the assets
		/// </summary>
		void UnloadAssets<TId, TAsset>(bool clearReferences, AssetConfigsScriptableObject<TId, TAsset> assetConfigs)
			where TId : struct;

		/// <summary>
		/// Unloads the asset reference of the given <typeparamref name="TId"/> type of the given <paramref name="ids"/>.
		/// If the given <paramref name="clearReferences"/> is true then will also removes any reference to the assets
		/// </summary>
		void UnloadAssets<TId, TAsset>(bool clearReferences, params TId[] ids)
			where TId : struct;
	}

	/// <inheritdoc />
	/// <remarks>
	/// It allows to add new asset references to the service
	/// </remarks>
	public interface IAssetAdderService : IAssetResolverService
	{
		/// <summary>
		/// Adds the given <paramref name="configs"/> to the asset reference list with <typeparamref name="TId"/> as
		/// the identifier type and <typeparamref name="TAsset"/> as asset type
		/// </summary>
		void AddConfigs<TId, TAsset>(AssetConfigsScriptableObject<TId, TAsset> configs)
			where TId : struct;
		
		/// <inheritdoc cref="AddConfigs{TId,TAsset}(FirstLight.AssetImporter.AssetConfigsScriptableObject{TId,TAsset})"/>
		void AddConfigs<TId, TAsset>(Type assetType, List<Pair<TId, AssetReference>> configs)
			where TId : struct;
		
		/// <summary>
		/// Adds the debug assets when errors occur
		/// </summary>
		void AddDebugConfigs(Sprite errorSprite, GameObject errorCube, Material errorMaterial, AudioClip errorClip);
	}

	/// <inheritdoc cref="IAssetResolverService" />
	public class AssetResolverService : AddressablesAssetLoader, IAssetAdderService
	{
		private readonly IDictionary<Type, IDictionary<Type, IDictionary>> _assetMap =
			new Dictionary<Type, IDictionary<Type, IDictionary>>();

		private Sprite _errorSprite;
		private GameObject _errorCube;
		private Material _errorMaterial;
		private AudioClip _errorClip;

		/// <inheritdoc />
		public bool TryGetAssetReference<TId, TAsset>(TId id, out AssetReference assetReference) where TId : struct
		{
			var dictionary = GetDictionary<TId, TAsset>();

			if (dictionary == null)
			{
				assetReference = default;

				return false;
			}

			return dictionary.TryGetValue(id, out assetReference);
		}

		/// <inheritdoc />
		public async Task<SceneInstance> LoadScene<TId>(TId id, LoadSceneMode loadMode = LoadSceneMode.Single,
		                                                bool activateOnLoad = true, bool setActive = true,
		                                                Action<TId, SceneInstance> onLoadCallback = null)
			where TId : struct
		{
			var dictionary = GetDictionary<TId, Scene>();

			if (dictionary == null || !dictionary.TryGetValue(id, out var assetReference))
			{
				throw new
					MissingMemberException($"The {nameof(AssetResolverService)} does not have the {nameof(AssetReference)}" +
					                       $"config to load the scene with the given {id} id of {typeof(TId)} type");
			}

			if (!assetReference.OperationHandle.IsValid())
			{
				FLog.Verbose($"Loading Scene: ID({typeof(TId).Name}.{id})");
				assetReference.LoadSceneAsync(loadMode, activateOnLoad);
			}

			if (!assetReference.IsDone)
			{
				await assetReference.OperationHandle.Task;
			}

			var sceneOperation = assetReference.OperationHandle.Convert<SceneInstance>();

			if (setActive)
			{
				SceneManager.SetActiveScene(sceneOperation.Result.Scene);
			}

			onLoadCallback?.Invoke(id, sceneOperation.Result);

			return sceneOperation.Result;
		}

		/// <inheritdoc />
		public async Task<List<Pair<TId, TAsset>>> LoadAllAssets<TId, TAsset>(bool loadAsynchronously = true, 
		                                                                      Action<TId, TAsset> onLoadCallback = null) 
			where TId : struct
		{
			var list = new List<Pair<TId, TAsset>>();
			var tasks = new List<Task<TAsset>>();
			var dictionary = GetDictionary<TId, TAsset>();

			foreach (var pair in dictionary)
			{
				var operation = pair.Value.LoadAssetAsync<TAsset>();
				var id = pair.Key;
				
				tasks.Add(operation.Task);

				operation.Completed += op => OnComplete(id, op);
				
				if (!loadAsynchronously)
				{
					operation.WaitForCompletion();
				}
			}

			await Task.WhenAll(tasks);
			
			foreach (var pair in dictionary)
			{
				list.Add(new Pair<TId, TAsset>(pair.Key, pair.Value.OperationHandle.Convert<TAsset>().Result));
			}

			return list;

			void OnComplete(TId id, AsyncOperationHandle<TAsset> asyncOperationHandle)
			{
				onLoadCallback?.Invoke(id, asyncOperationHandle.Result);
			}
		}

		/// <inheritdoc />
		public async Task<TAsset> RequestAsset<TId, TAsset>(TId id, bool loadAsynchronously = true,
		                                                    bool instantiate = true,
		                                                    Action<TId, TAsset, bool> onLoadCallback = null)
			where TId : struct
			where TAsset : Object
		{
			return await RequestAsset<TId, TAsset, int>(id, 0, loadAsynchronously, instantiate,
			                                            (arg1, arg2, _, arg4) =>
				                                            onLoadCallback?.Invoke(arg1, arg2, arg4));
		}

		/// <inheritdoc />
		public async Task<TAsset> RequestAsset<TId, TAsset, TData>(TId id, TData data, bool loadAsynchronously = true,
		                                                           bool instantiate = true,
		                                                           Action<TId, TAsset, TData, bool> onLoadCallback =
			                                                           null)
			where TId : struct
			where TAsset : Object
		{
			var dictionary = GetDictionary<TId, TAsset>();

			if (dictionary == null || !dictionary.TryGetValue(id, out var assetReference))
			{
				throw new MissingMemberException($"The {nameof(AssetResolverService)} does not have the " +
				                                 $"{nameof(AssetReference)} config to load the necessary asset for the " +
				                                 $"given {typeof(TAsset)} type with the given {id} id of {typeof(TId)} type");
			}

			if (!assetReference.OperationHandle.IsValid())
			{
				FLog.Verbose($"Loading Asset: ID({typeof(TId).Name}.{id}), Type({typeof(TAsset).Name}), DATA({data})");
				assetReference.LoadAssetAsync<TAsset>();
			}

			if (!loadAsynchronously)
			{
				assetReference.OperationHandle.WaitForCompletion();
			}

			if (!assetReference.IsDone)
			{
				await assetReference.OperationHandle.Task;
			}

			var asset = Convert<TAsset>(assetReference, instantiate);

			onLoadCallback?.Invoke(id, asset, data, instantiate);

			return asset;
		}

		/// <inheritdoc />
		public async Task UnloadScene<TId>(TId id, Action<TId, SceneInstance> onUnloadCallback = null)
			where TId : struct
		{
			var dictionary = GetDictionary<TId, Scene>();

			if (dictionary == null || !dictionary.TryGetValue(id, out var assetReference))
			{
				throw new
					MissingMemberException($"The {nameof(AssetResolverService)} does not have the {nameof(AssetReference)}" +
					                       $"config to load the scene with the given {id} id of {typeof(TId)} type");
			}

			var sceneOperation = assetReference.OperationHandle.Convert<SceneInstance>();

			await assetReference.UnLoadScene().Task;

			onUnloadCallback?.Invoke(id, sceneOperation.Result);
		}

		/// <inheritdoc />
		public void UnloadAssets<TId, TAsset>(bool clearReferences)
			where TId : struct
		{
			var idType = typeof(TId);
			var assetType = typeof(TAsset);
			var dictionary = GetDictionary<TId, TAsset>();

			foreach (var asset in dictionary)
			{
				if (asset.Value.IsValid())
				{
					asset.Value.ReleaseAsset();
				}
			}

			if (clearReferences)
			{
				_assetMap[assetType].Remove(idType);
			}
		}

		/// <inheritdoc />
		public void UnloadAssets<TId, TAsset>(bool clearReferences,
		                                      AssetConfigsScriptableObject<TId, TAsset> assetConfigs)
			where TId : struct
		{
			var dictionary = GetDictionary<TId, TAsset>();

			foreach (var pair in assetConfigs.Configs)
			{
				if (!dictionary.TryGetValue(pair.Key, out var asset))
				{
					continue;
				}

				if (asset.IsValid())
				{
					asset.ReleaseAsset();
				}

				if (clearReferences)
				{
					dictionary.Remove(pair.Key);
				}
			}
		}

		/// <inheritdoc />
		public void UnloadAssets<TId, TAsset>(bool clearReferences, params TId[] ids)
			where TId : struct
		{
			var dictionary = GetDictionary<TId, TAsset>();

			foreach (var id in ids)
			{
				if (!dictionary.TryGetValue(id, out var asset))
				{
					continue;
				}

				if (asset.IsValid())
				{
					asset.ReleaseAsset();
				}

				if (clearReferences)
				{
					dictionary.Remove(id);
				}
			}
		}

		/// <inheritdoc />
		public void AddConfigs<TId, TAsset>(AssetConfigsScriptableObject<TId, TAsset> configs)
			where TId : struct
		{
			AddConfigs<TId, TAsset>(configs.AssetType, configs.Configs);
		}

		/// <inheritdoc />
		public void AddConfigs<TId, TAsset>(Type assetType, List<Pair<TId, AssetReference>> configs) where TId : struct
		{
			var idType = typeof(TId);
			var assetReferences = new Dictionary<TId, AssetReference>(configs.Count);

			for (var i = 0; i < configs.Count; i++)
			{
				assetReferences.Add(configs[i].Key, configs[i].Value);
			}

			if (!_assetMap.TryGetValue(assetType, out var map))
			{
				map = new Dictionary<Type, IDictionary>();

				_assetMap.Add(assetType, map);
			}

			if (map.TryGetValue(idType, out var dictionary))
			{
				var assets = dictionary as Dictionary<TId, AssetReference>;

				// ReSharper disable once PossibleNullReferenceException
				foreach (var asset in assets)
				{
					assetReferences.Add(asset.Key, asset.Value);
				}

				map[idType] = assetReferences;
			}
			else
			{
				map.Add(idType, assetReferences);
			}
		}

		/// <inheritdoc />
		public void AddDebugConfigs(Sprite errorSprite, GameObject errorCube, Material errorMaterial,
		                            AudioClip errorClip)
		{
			_errorSprite = errorSprite;
			_errorCube = errorCube;
			_errorMaterial = errorMaterial;
			_errorClip = errorClip;
		}

		private TAsset Convert<TAsset>(AssetReference assetReference, bool instantiate)
			where TAsset : Object
		{
			var type = typeof(TAsset);
			var spriteType = typeof(Sprite);
			var materialType = typeof(Material);
			var clipType = typeof(AudioClip);
			var goType = typeof(GameObject);

			if (type == goType)
			{
				var asset = !assetReference.IsDone ? _errorCube : assetReference.Asset as GameObject;

				return instantiate ? Object.Instantiate(asset) as TAsset : asset as TAsset;
			}

			if (type == spriteType)
			{
				return !assetReference.IsDone ? _errorSprite as TAsset : assetReference.Asset as TAsset;
			}

			if (type == materialType)
			{
				var asset = !assetReference.IsDone ? _errorMaterial : assetReference.Asset as Material;

				return instantiate ? new Material(asset) as TAsset : asset as TAsset;
			}

			if (type == clipType)
			{
				return !assetReference.IsDone ? _errorClip as TAsset : assetReference.Asset as TAsset;
			}

			return assetReference.Asset as TAsset;
		}

		private Dictionary<TId, AssetReference> GetDictionary<TId, TAsset>()
			where TId : struct
		{
			var idType = typeof(TId);
			var assetType = typeof(TAsset);

			if (!_assetMap.TryGetValue(assetType, out var idMap))
			{
				return null;
			}

			if (!idMap.TryGetValue(idType, out var assetMap))
			{
				return null;
			}

			return assetMap as Dictionary<TId, AssetReference>;
		}
	}
}