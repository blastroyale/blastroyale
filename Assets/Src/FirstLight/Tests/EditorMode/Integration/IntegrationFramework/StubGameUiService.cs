using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FirstLight.Game.Ids;
using FirstLight.Game.Presenters;
using FirstLight.Game.Services;
using FirstLight.Game.Timeline;
using FirstLight.UiService;
using UnityEngine;

namespace FirstLight.Tests.EditorMode
{
    public class StubGameUiService : IGameUiServiceInit
    {
        public int TotalLayers { get; set; }
        private GameObject _layer;

        public GameObject AddLayer(int layer)
        {
            return _layer;
        }

        public GameObject GetLayer(int layer)
        {
            if (_layer == null)
            {
                _layer = new GameObject("UI LAYER");
            }

            return _layer;
        }

        public bool TryGetLayer(int layer, out GameObject layerObject)
        {
            layerObject = GetLayer(0);
            return true;
        }

        public void AddUiConfig(UiConfig config)
        {
        }

        public void AddUi<T>(T uiPresenter, int layer, bool openAfter = false) where T : UiPresenter
        {
        
        }

        public void RemoveUi<T>() where T : UiPresenter
        {
            
        }

        public void RemoveUi(Type type)
        {
        
        }

        public void RemoveUi<T>(T uiPresenter) where T : UiPresenter
        {
            
        }

        public Task<T> LoadUiAsync<T>(bool openAfter = false) where T : UiPresenter
        {
            return Task.FromResult(default(T));
        }

        public Task<UiPresenter> LoadUiAsync(Type type, bool openAfter = false)
        {
            return Task.FromResult(default(UiPresenter));
        }

        public void UnloadUi<T>() where T : UiPresenter
        {
            
        }

        public void UnloadUi(Type type)
        {
            
        }

        public void UnloadUi<T>(T uiPresenter) where T : UiPresenter
        {
            
        }

        public bool HasUiPresenter<T>() where T : UiPresenter
        {
            return false;
        }

        public bool HasUiPresenter(Type type)
        {
            return false;
        }

        public T GetUi<T>() where T : UiPresenter
        {
            return default(T);
        }

        public Task<UiPresenter> GetUiAsync(Type type)
        {
            return Task.FromResult(default(UiPresenter));
        }

        public UiPresenter GetUi(Type type)
        {
            return null;
        }

        public List<Type> GetAllVisibleUi()
        {
            return new List<Type>();
        }

        public T OpenUi<T>(bool openedException = false) where T : UiPresenter
        {
            return default(T);
        }

        public Task<UiPresenter> OpenUiAsync(Type type, bool openedException = false)
        {
            return Task.FromResult(default(UiPresenter));
        }

        public UiPresenter OpenUi(Type type, bool openedException = false)
        {
            return default(UiPresenter);
        }

        public Task<T> OpenUiAsync<T, TData>(TData initialData, bool openedException = false) where T : class, IUiPresenterData where TData : struct
        {
            return Task.FromResult(default(T));
        }

        public T OpenUi<T, TData>(TData initialData, bool openedException = false) where T : class, IUiPresenterData where TData : struct
        {
            return default(T);
        }

        public Task<UiPresenter> OpenUiAsync<TData>(Type type, TData initialData, bool openedException = false) where TData : struct
        {
            return Task.FromResult(default(UiPresenter));
        }

        public UiPresenter OpenUi<TData>(Type type, TData initialData, bool openedException = false) where TData : struct
        {
            return default(UiPresenter);
        }

        public void CloseUi<T>(bool closedException = false, bool destroy = false) where T : UiPresenter
        {
             
        }

        public void CloseUi(Type type, bool closedException = false, bool destroy = false)
        {
             
        }

        public void CloseUi<T>(T uiPresenter, bool closedException = false, bool destroy = false) where T : UiPresenter
        {
             
        }

        public void CloseAllUi()
        {
             
        }

        public void CloseAllUi(int layer)
        {
             
        }

        public void CloseUiAndAllInFront<T>(params int[] excludeLayers) where T : UiPresenter
        {
             
        }

        public void AddUiSet(UiSetConfig uiSet)
        {
             
        }

        public List<UiPresenter> RemoveUiPresentersFromSet(int setId)
        {
            return new List<UiPresenter>();
        }

        public Task<Task<UiPresenter>>[] LoadUiSetAsync(int setId)
        {

            var task = Task.FromResult(default(UiPresenter));
            return new Task<Task<UiPresenter>>[] {Task.FromResult(task)}; // holy molly
        }

        public void UnloadUiSet(int setId)
        {
             
        }

        public bool HasUiSet(int setId)
        {
            return false;
        }

        public bool HasAllUiPresentersInSet(int setId)
        {
            return false;
        }

        public UiSetConfig GetUiSet(int setId)
        {
            return new UiSetConfig();
        }

        public void OpenUiSet(int setId, bool closeVisibleUi)
        {
            
        }

        public void CloseUiSet(int setId)
        {
            
        }

        public void Init(UiConfigs configs)
        {
            
        }

        public Task LoadGameUiSet(UiSetId uiSetId, float loadingCap)
        {
            return Task.FromResult(0);
        }
    }
}