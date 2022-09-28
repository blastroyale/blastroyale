using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.ResourceProviders;

// ReSharper disable CheckNamespace

namespace FirstLight.UiService
{
	/// <inheritdoc />
	public class UiService : IUiServiceInit
	{
		private readonly IUiAssetLoader _assetLoader;
		private readonly IDictionary<Type, UiReference> _uiViews = new Dictionary<Type, UiReference>();
		private readonly IDictionary<Type, UiConfig> _uiConfigs = new Dictionary<Type, UiConfig>();
		private readonly IDictionary<int, UiSetConfig> _uiSets = new Dictionary<int, UiSetConfig>();
		private readonly IList<Type> _visibleUiList = new List<Type>();
		private readonly IList<GameObject> _layers = new List<GameObject>();
		private Type _loadingSpinnerType;

		public UiService(IUiAssetLoader assetLoader)
		{
			_assetLoader = assetLoader;
		}

		/// <inheritdoc />
		public void Init(UiConfigs configs)
		{
			var uiConfigs = configs.Configs;
			var sets = configs.Sets;
			
			foreach (var uiConfig in uiConfigs)
			{
				AddUiConfig(uiConfig);
			}
			
			foreach (var set in sets)
			{
				AddUiSet(set);
			}

			_loadingSpinnerType = configs.LoadingSpinnerType;
		}

		/// <inheritdoc />
		public int TotalLayers => _layers.Count;

		/// <inheritdoc />
		public GameObject AddLayer(int layer)
		{
			for(int i = _layers.Count; i <= layer; i++)
			{
				var newObj = new GameObject($"Layer {i.ToString()}");
				
				newObj.transform.position = Vector3.zero;
				_layers.Add(newObj);
			}

			return _layers[layer];
		}

		/// <inheritdoc />
		public GameObject GetLayer(int layer)
		{
			return _layers[layer];
		}

		/// <inheritdoc />
		public bool TryGetLayer(int layer, out GameObject layerObject)
		{
			if (layer >= TotalLayers)
			{
				layerObject = null;
				
				return false;
			}

			layerObject = GetLayer(layer);

			return true;
		}

		/// <inheritdoc />
		public void AddUiConfig(UiConfig config)
		{
			if (_uiConfigs.ContainsKey(config.UiType))
			{
				throw new ArgumentException($"The UiConfig {config.AddressableAddress} was already added");
			}

			_uiConfigs.Add(config.UiType, config);
		}

		/// <inheritdoc />
		public void AddUi<T>(T uiPresenter, int layer, bool openAfter = false) where T : UiPresenter
		{
			var type = uiPresenter.GetType().UnderlyingSystemType;
			
			if (HasUiPresenter(type))
			{
				throw new ArgumentException($"The Ui {type} was already added");
			}
			
			var reference = new UiReference
			{
				UiType = type,
				Layer = layer,
				Presenter = uiPresenter
			};
			
			_uiViews.Add(reference.UiType, reference);
			uiPresenter.Init(this);

			if (openAfter)
			{
				OpenUi(type);
			}
		}

		/// <inheritdoc />
		public void RemoveUi<T>() where T : UiPresenter
		{
			RemoveUi(typeof(T));
		}

		/// <inheritdoc />
		public void RemoveUi(Type type)
		{
			CloseUi(type);
			
			_uiViews.Remove(type);
			_visibleUiList.Remove(type);
		}

		/// <inheritdoc />
		public void RemoveUi<T>(T uiPresenter) where T : UiPresenter
		{
			RemoveUi(uiPresenter.GetType().UnderlyingSystemType);
		}

		/// <inheritdoc />
		public async Task<T> LoadUiAsync<T>(bool openAfter = false) where T : UiPresenter
		{
			var uiPresenter = await LoadUiAsync(typeof(T), openAfter);
			
			return uiPresenter as T;
		}

		/// <inheritdoc />
		public async Task<UiPresenter> LoadUiAsync(Type type, bool openAfter = false)
		{
			if (!_uiConfigs.TryGetValue(type, out var config))
			{
				throw new KeyNotFoundException($"The UiConfig of type {type} was not added to the service. Call {nameof(AddUiConfig)} first");
			}

			if (HasUiPresenter(type))
			{
				var ui = await GetUiAsync(type);
				
				ui.gameObject.SetActive(openAfter);

				return ui;
			}

			var layer = AddLayer(config.Layer);


			GameObject gameObject;
			if (Attribute.IsDefined(type, typeof(LoadSynchronouslyAttribute)))
			{
				gameObject = _assetLoader.InstantiatePrefab(config.AddressableAddress, layer.transform, false);
			}
			else
			{
				gameObject =
					await _assetLoader.InstantiatePrefabAsync(config.AddressableAddress, layer.transform, false);
			}

			// Double check if the same UiPresenter was already loaded. This can happen if the coder spam calls LoadUiAsync
			if (HasUiPresenter(type))
			{
				var ui = await GetUiAsync(type);
				
				_assetLoader.UnloadAsset(gameObject);
				ui.gameObject.SetActive(openAfter);

				return ui;
			}
			
			var uiPresenter = gameObject.GetComponent<UiPresenter>();
			
			gameObject.SetActive(false);
			AddUi(uiPresenter, config.Layer, openAfter);

			return uiPresenter;
		}

		/// <inheritdoc />
		public void UnloadUi<T>() where T : UiPresenter
		{
			UnloadUi(typeof(T));
		}

		/// <inheritdoc />
		public void UnloadUi(Type type)
		{
			var ui = GetUi(type);
			
			RemoveUi(type);
			
			_assetLoader.UnloadAsset(ui.gameObject);
		}

		/// <inheritdoc />
		public void UnloadUi<T>(T uiPresenter) where T : UiPresenter
		{
			UnloadUi(uiPresenter.GetType().UnderlyingSystemType);
		}

		/// <inheritdoc />
		public bool HasUiPresenter<T>() where T : UiPresenter
		{
			return HasUiPresenter(typeof(T));
		}

		/// <inheritdoc />
		public bool HasUiPresenter(Type type)
		{
			return _uiViews.ContainsKey(type);
		}

		/// <inheritdoc />
		public async Task<T> GetUiAsync<T>() where T : UiPresenter
		{
			var presenter = await GetUiAsync(typeof(T));
			
			return presenter as T;
		}

		/// <inheritdoc />
		public T GetUi<T>() where T : UiPresenter
		{
			return GetUi(typeof(T)) as T;
		}

		/// <inheritdoc />
		public async Task<UiPresenter> GetUiAsync(Type type)
		{
			var presenter = await GetReferenceAsync(type);

			return presenter.Presenter;
		}
		
		/// <inheritdoc />
		public UiPresenter GetUi(Type type)
		{
			var presenter = GetReference(type);

			return presenter.Presenter;
		}

		/// <inheritdoc />
		public List<Type> GetAllVisibleUi()
		{
			return new List<Type>(_visibleUiList);
		}

		/// <inheritdoc />
		public async Task<T> OpenUiAsync<T>(bool openedException = false) where T : UiPresenter
		{
			await GetUiAsync<T>();

			return OpenUi<T>(openedException);
		}

		/// <inheritdoc />
		public T OpenUi<T>(bool openedException = false) where T : UiPresenter
		{
			return OpenUi(typeof(T), openedException) as T;
		}

		/// <inheritdoc />
		public async Task<UiPresenter> OpenUiAsync(Type type, bool openedException = false)
		{
			await GetUiAsync(type);
			
			return OpenUi(type, openedException);
		}
		
		/// <inheritdoc />
		public UiPresenter OpenUi(Type type, bool openedException = false)
		{
			var ui = GetUi(type);
			
			if (!_visibleUiList.Contains(type))
			{
				ui.InternalOpen();
				_visibleUiList.Add(type);
			}
			else if(openedException)
			{
				throw new InvalidOperationException($"Is trying to open the {type.Name} ui but is already open");
			}
			
			return ui;
		}

		/// <inheritdoc />
		public T OpenUi<T, TData>(TData initialData, bool openedException = false) 
			where T : class, IUiPresenterData 
			where TData : struct
		{
			return OpenUi(typeof(T), initialData, openedException) as T;
		}
		
		/// <inheritdoc />
		public async Task<T> OpenUiAsync<T, TData>(TData initialData, bool openedException = false) 
			where T : class, IUiPresenterData 
			where TData : struct
		{
			return await OpenUiAsync(typeof(T), initialData, openedException) as T;
		}

		/// <inheritdoc />
		public UiPresenter OpenUi<TData>(Type type, TData initialData, bool openedException = false) where TData : struct
		{
			var uiPresenterData = GetUi(type) as UiPresenterData<TData>;

			if (uiPresenterData == null)
			{
				throw new ArgumentException($"The UiPresenter {type} is not of a {nameof(UiPresenterData<TData>)}");
			}
			
			uiPresenterData.InternalSetData(initialData);

			return OpenUi(type, openedException);
		}
		
		/// <inheritdoc />
		public async Task<UiPresenter> OpenUiAsync<TData>(Type type, TData initialData, bool openedException = false) where TData : struct
		{
			await GetUiAsync(type);

			return OpenUi(type, initialData, openedException);
		}

		/// <inheritdoc />
		public void CloseUi<T>(bool closedException = false, bool destroy = false) where T : UiPresenter
		{
			CloseUi(typeof(T), closedException, destroy);
		}

		/// <inheritdoc />
		public void CloseUi(Type type, bool closedException = false, bool destroy = false)
		{
			if (_visibleUiList.Contains(type) || destroy)
			{
				_visibleUiList.Remove(type);
				
				var ui = GetUi(type);

				ui.InternalClose(destroy);
			}
			else if(closedException)
			{
				throw new InvalidOperationException($"Is trying to close the {type.Name} ui but is not open");
			}
		}

		/// <inheritdoc />
		public void CloseUi<T>(T uiPresenter, bool closedException = false, bool destroy = false) where T : UiPresenter
		{
			CloseUi(uiPresenter.GetType().UnderlyingSystemType, closedException, destroy);
		}

		/// <inheritdoc />
		public void CloseAllUi()
		{
			for (int i = 0; i < _visibleUiList.Count; i++)
			{
				GetUi(_visibleUiList[i]).InternalClose(false);
				_visibleUiList.Remove(_visibleUiList[i]);
			}
			
			_visibleUiList.Clear();
		}

		/// <inheritdoc />
		public void CloseUiAndAllInFront<T>(params int[] excludeLayers) where T : UiPresenter
		{
			var layers = new List<int>(excludeLayers);
			
			for (int i = GetReference(typeof(T)).Layer; i <= _layers.Count; i++)
			{
				if (layers.Contains(i))
				{
					continue;
				}
				
				CloseAllUi(i);
			}
		}

		/// <inheritdoc />
		public void CloseAllUi(int layer)
		{
			for (int i = 0; i < _visibleUiList.Count; i++)
			{
				var reference = GetReference(_visibleUiList[i]);
				if (reference.Layer == layer)
				{
					reference.Presenter.InternalClose(false);
					_visibleUiList.Remove(reference.UiType);
				}
			}
		}

		/// <inheritdoc />
		public void AddUiSet(UiSetConfig uiSet)
		{
			if (_uiSets.ContainsKey(uiSet.SetId))
			{
				throw new ArgumentException($"The Ui Configuration with the id {uiSet.SetId.ToString()} was already added");
			}
			
			_uiSets.Add(uiSet.SetId, uiSet);
		}

		/// <inheritdoc />
		public List<UiPresenter> RemoveUiPresentersFromSet(int setId)
		{
			var set = GetUiSet(setId);
			var list = new List<UiPresenter>();

			for (int i = 0; i < set.UiConfigsType.Count; i++)
			{
				Type uiType = set.UiConfigsType[i];
				
				if (!HasUiPresenter(uiType))
				{
					continue;
				}

				RemoveUi(uiType);

				list.Add(GetUi(uiType));
			}

			return list;
		}

		/// <inheritdoc />
		public Task<Task<UiPresenter>>[] LoadUiSetAsync(int setId)
		{
			var set = GetUiSet(setId);
			var uiTasks = new List<Task<UiPresenter>>();

			for (int i = 0; i < set.UiConfigsType.Count; i++)
			{
				if (HasUiPresenter(set.UiConfigsType[i]))
				{
					continue;
				}
				
				uiTasks.Add(LoadUiAsync(set.UiConfigsType[i]));
			}

			return Interleaved(uiTasks);
		}

		/// <inheritdoc />
		public void UnloadUiSet(int setId)
		{
			var set = GetUiSet(setId);

			for (var i = 0; i < set.UiConfigsType.Count; i++)
			{
				if (HasUiPresenter(set.UiConfigsType[i]))
				{
					UnloadUi(set.UiConfigsType[i]);
				}
			}
		}

		/// <inheritdoc />
		public bool HasUiSet(int setId)
		{
			return _uiSets.ContainsKey(setId);
		}

		/// <inheritdoc />
		public bool HasAllUiPresentersInSet(int setId)
		{
			var set = GetUiSet(setId);

			for (var i = 0; i < set.UiConfigsType.Count; i++)
			{
				if (!HasUiPresenter(set.UiConfigsType[i]))
				{
					return false;
				}
			}

			return true;
		}

		/// <inheritdoc />
		public UiSetConfig GetUiSet(int setId)
		{
			if (!_uiSets.TryGetValue(setId, out UiSetConfig set))
			{
				throw new KeyNotFoundException($"The UiSet with the id {setId.ToString()} was not added to the service. Call {nameof(AddUiSet)} first");
			}

			return set;
		}

		/// <inheritdoc />
		public void OpenUiSet(int setId, bool closeVisibleUi)
		{
			var set = GetUiSet(setId);

			if (closeVisibleUi)
			{
				var list = new List<Type>(set.UiConfigsType);
				for (var i = 0; i < _visibleUiList.Count; i++)
				{
					if (list.Contains(_visibleUiList[i]))
					{
						continue;
					}

					CloseUi(_visibleUiList[i]);
				}
			}

			for (var i = 0; i < set.UiConfigsType.Count; i++)
			{
				if (_visibleUiList.Contains(set.UiConfigsType[i]))
				{
					continue;
				}
				
				OpenUi(set.UiConfigsType[i]);
			}
		}

		/// <inheritdoc />
		public void CloseUiSet(int setId)
		{
			var set = GetUiSet(setId);
			
			for (var i = 0; i < set.UiConfigsType.Count; i++)
			{
				CloseUi(set.UiConfigsType[i]);
			}
		}
		
		private UiReference GetReference(Type type)
		{
			UiReference uiReference;

			if (!_uiViews.TryGetValue(type, out uiReference))
			{
				throw new
					KeyNotFoundException($"The Ui {type} was not added to the service. Call {nameof(AddUi)} or {nameof(LoadUiAsync)} first");
			}

			return uiReference;
		}

		private async Task<UiReference> GetReferenceAsync(Type type)
		{
			if (!_uiViews.TryGetValue(type, out var uiReference))
			{
				OpenLoadingSpinner();
				await LoadUiAsync(type);
				CloseLoadingSpinner();
				
				if (!_uiViews.TryGetValue(type, out uiReference))
				{
					throw new
						KeyNotFoundException($"The Ui {type} was not added to the service. Call {nameof(AddUi)} or {nameof(LoadUiAsync)} first");
				}
			}

			return uiReference;
		}
		
		private Task<Task<T>>[] Interleaved<T>(IEnumerable<Task<T>> tasks)
		{
			var inputTasks = tasks.ToList();
			var buckets = new TaskCompletionSource<Task<T>>[inputTasks.Count];
			var results = new Task<Task<T>>[buckets.Length];
			var nextTaskIndex = -1;
			
			for (var i = 0; i < buckets.Length; i++) 
			{
				buckets[i] = new TaskCompletionSource<Task<T>>();
				results[i] = buckets[i].Task;
			}
			
			foreach (var inputTask in inputTasks)
			{
				inputTask.ContinueWith(Continuation, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
			}

			return results;

			// Local function
			void Continuation(Task<T> completed)
			{
				buckets[Interlocked.Increment(ref nextTaskIndex)].TrySetResult(completed);
			}
		}

		private void OpenLoadingSpinner()
		{
			if (_loadingSpinnerType == null)
			{
				return;
			}

			if (HasUiPresenter(_loadingSpinnerType))
			{
				OpenUi(_loadingSpinnerType);
			}
		}

		private void CloseLoadingSpinner()
		{
			if (_loadingSpinnerType == null)
			{
				return;
			}

			CloseUi(_loadingSpinnerType);
		}
		
		private struct UiReference
		{
			public Type UiType;
			public int Layer;
			public UiPresenter Presenter;
		}
	}
}