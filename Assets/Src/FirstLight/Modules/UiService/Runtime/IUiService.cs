using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
#pragma warning disable CS0612 // Type or member is obsolete

// ReSharper disable CheckNamespace

namespace FirstLight.UiService
{
	/// <summary>
	/// This service provides an abstraction layer to interact with the game's <seealso cref="UiPresenter"/>
	/// The Ui Service is organized by layers. The higher the layer the more close is to the camera viewport
	/// </summary>
	public interface IUiService
	{ 
		event Action<Type> ScreenStartOpening;
		
		/// <summary>
		/// Requests the total amount of layers available in the UI
		/// </summary>
		int TotalLayers { get; }
		
		/// <summary>
		/// Adds a new <paramref name="layer"/> and all layers in between the given <paramref name="layer"/>
		/// Returns the root <see cref="GameObject"/> of the given <paramref name="layer"/>
		/// </summary>
		GameObject AddLayer(int layer);
		
		/// <summary>
		/// Requests the root <see cref="GameObject"/> of the given <paramref name="layer"/>
		/// </summary>
		GameObject GetLayer(int layer);
		
		/// <summary>
		/// Requests the root <see cref="GameObject"/> of the given <paramref name="layer"/> if available.
		/// Returns true if the given <paramref name="layer"/> is available, false otherwise.
		/// </summary>
		bool TryGetLayer(int layer, out GameObject layerObject);
		
		/// <summary>
		/// Adds the given UI <paramref name="config"/> to the service
		/// </summary>
		/// <exception cref="ArgumentException">
		/// Thrown if the service already contains the given <paramref name="config"/>
		/// </exception>
		void AddUiConfig(UiConfig config);
		
		/// <summary>
		/// Adds the given <paramref name="uiPresenter"/> to the service and to be included inside the given <paramref name="layer"/>.
		/// If the given <paramref name="openAfter"/> is true, will open the <see cref="UiPresenter"/> after adding it to the service
		/// </summary>
		/// <exception cref="ArgumentException">
		/// Thrown if the service already contains the given <paramref name="uiPresenter"/>
		/// </exception>
		void AddUi<T>(T uiPresenter, int layer, bool openAfter = false) where T : UiPresenter;
		
		/// <summary>
		/// Removes and returns the UI of the given type <typeparamref name="T"/> without unloading it from the service
		/// </summary>
		/// <exception cref="KeyNotFoundException">
		/// Thrown if the service does NOT contain an <see cref="UiPresenter"/> of the given type <typeparamref name="T"/>
		/// </exception>
		void RemoveUi<T>() where T : UiPresenter;

		/// <summary>
		/// Removes and returns the UI of the given <paramref name="type"/> without unloading it from the service
		/// </summary>
		/// <exception cref="KeyNotFoundException">
		/// Thrown if the service does NOT contain an <see cref="UiPresenter"/> of the given <paramref name="type"/>
		/// </exception>
		void RemoveUi(Type type);

		/// <summary>
		/// Removes and returns the given <paramref name="uiPresenter"/> without unloading it from the service
		/// </summary>
		/// <exception cref="KeyNotFoundException">
		/// Thrown if the service does NOT contain an <see cref="UiPresenter"/>
		/// </exception>
		void RemoveUi<T>(T uiPresenter) where T : UiPresenter;
		
		/// <summary>
		/// Loads an UI asynchronously with the given <typeparamref name="T"/>.
		/// This method can be controlled in an async method and returns the UI loaded.
		/// If the given <paramref name="openAfter"/> is true, will open the <see cref="UiPresenter"/> after loading
		/// </summary>
		/// <exception cref="KeyNotFoundException">
		/// Thrown if the service does NOT contain a <see cref="UiConfig"/> of the given type <typeparamref name="T"/>.
		/// You need to call <seealso cref="AddUiConfig"/> or <seealso cref="AddUi{T}"/> or initialize the service first
		/// </exception>
		UniTask<T> LoadUiAsync<T>(bool openAfter = false) where T : UiPresenter;
		
		/// <summary>
		/// Loads an UI asynchronously with the given <paramref name="type"/>.
		/// This method can be controlled in an async method and returns the UI loaded.
		/// If the given <paramref name="openAfter"/> is true, will open the <see cref="UiPresenter"/> after loading
		/// </summary>
		/// <exception cref="KeyNotFoundException">
		/// Thrown if the service does NOT contain a <see cref="UiConfig"/> of the given <paramref name="type"/>
		/// You need to call <seealso cref="AddUiConfig"/> or <seealso cref="AddUi{T}"/> or initialize the service first
		/// </exception>
		UniTask<UiPresenter> LoadUiAsync(Type type, bool openAfter = false);
		
		/// <summary>
		/// Unloads the UI of the given type <typeparamref name="T"/>
		/// </summary>
		/// <exception cref="KeyNotFoundException">
		/// Thrown if the service does NOT contain an <see cref="UiPresenter"/> of the given type <typeparamref name="T"/>
		/// </exception>
		void UnloadUi<T>() where T : UiPresenter;
		
		/// <summary>
		/// Unloads the UI of the given <paramref name="type"/>
		/// </summary>
		/// <exception cref="KeyNotFoundException">
		/// Thrown if the service does NOT contain an <see cref="UiPresenter"/> of the given <paramref name="type"/>
		/// </exception>
		void UnloadUi(Type type);

		/// <summary>
		/// Unloads the UI of the given <paramref name="uiPresenter"/>
		/// </summary>
		/// <exception cref="KeyNotFoundException">
		/// Thrown if the service does NOT contain an <see cref="UiPresenter"/> of the given <paramref name="type"/>
		/// </exception>
		void UnloadUi<T>(T uiPresenter) where T : UiPresenter;
		
		/// <summary>
		/// Checks if the service contains <seealso cref="UiPresenter"/> of the given <typeparamref name="T"/>
		/// </summary>
		bool HasUiPresenter<T>() where T : UiPresenter;
		
		/// <summary>
		/// Checks if the service contains <seealso cref="UiPresenter"/> of the given <paramref name="type"/> is loaded or not 
		/// </summary>
		bool HasUiPresenter(Type type);
		
		/// <inheritdoc cref="GetUi{T}"/>
		/// <remarks>
		/// Executes the call asynchronously while loading the UI asset
		/// </remarks>
		UniTask<T> GetUiAsync<T>() where T : UiPresenter;
		
		/// <summary>
		/// Requests the UI of given type <typeparamref name="T"/>
		/// </summary>
		/// <exception cref="KeyNotFoundException">
		/// Thrown if the service does NOT contain an <see cref="UiPresenter"/> of the given <typeparamref name="T"/>
		/// </exception>
		T GetUi<T>() where T : UiPresenter;
		
		/// <inheritdoc cref="GetUi"/>
		/// <remarks>
		/// Executes the call asynchronously while loading the UI asset
		/// </remarks>
		UniTask<UiPresenter> GetUiAsync(Type type);
		
		/// <summary>
		/// Requests the UI of given <paramref name="type"/>
		/// </summary>
		/// <exception cref="KeyNotFoundException">
		/// Thrown if the service does NOT contain an <see cref="UiPresenter"/> of the given <paramref name="type"/>
		/// </exception>
		UiPresenter GetUi(Type type);

		/// <summary>
		/// Requests the list all the visible UIs' <seealso cref="Type"/> on the screen
		/// </summary>
		List<Type> GetAllVisibleUi();

		/// <inheritdoc cref="OpenUi{T}(bool)"/>
		/// <remarks>
		/// Executes the call asynchronously while loading the UI asset
		/// </remarks>
		UniTask<T> OpenUiAsync<T>(bool openedException = false) where T : UiPresenter;

		/// <summary>
		/// Checks if a given UI is open
		/// </summary>
		bool IsOpen<T>() where T : UiPresenter;

		/// <summary>
		/// Opens and returns the UI of given type <typeparamref name="T"/>.
		/// If the given <paramref name="openedException"/> is true, then will throw an <see cref="InvalidOperationException"/>
		/// if the <see cref="UiPresenter"/> is already opened.
		/// </summary>
		/// <exception cref="KeyNotFoundException">
		/// Thrown if the service does NOT contain an <see cref="UiPresenter"/> of the given <typeparamref name="T"/>
		/// </exception>
		T OpenUi<T>(bool openedException = false) where T : UiPresenter;

		/// <inheritdoc cref="OpenUi"/>
		/// <remarks>
		/// Executes the call asynchronously while loading the UI asset
		/// </remarks>
		UniTask<UiPresenter> OpenUiAsync(Type type);
		
		/// <summary>
		/// Opens and returns the UI of given <paramref name="type"/>.
		/// If the given <paramref name="openedException"/> is true, then will throw an <see cref="InvalidOperationException"/>
		/// if the <see cref="UiPresenter"/> is already opened.
		/// </summary>
		/// <exception cref="KeyNotFoundException">
		/// Thrown if the service does NOT contain an <see cref="UiPresenter"/> of the given <paramref name="type"/>
		/// </exception>
		UiPresenter OpenUi(Type type);

		///<inheritdoc cref="OpenUi{T,TData}(TData, bool)"/>
		/// <remarks>
		/// Executes the call asynchronously while loading the UI asset
		/// </remarks>
		UniTask<T> OpenUiAsync<T, TData>(TData initialData) 
			where T : class, IUiPresenterData 
			where TData : struct;
		
		///<inheritdoc cref="OpenUi{T}(bool)"/>
		/// <remarks>
		/// It sets the given <paramref name="initialData"/> data BEFORE opening the UI
		/// </remarks>
		T OpenUi<T, TData>(TData initialData) 
			where T : class, IUiPresenterData 
			where TData : struct;

		///<inheritdoc cref="OpenUi{TData}(Type,TData,bool)"/>
		/// <remarks>
		/// Executes the call asynchronously while loading the UI asset
		/// </remarks>
		UniTask<UiPresenter> OpenUiAsync<TData>(Type type, TData initialData) where TData : struct;
		
		///<inheritdoc cref="OpenUi(Type, bool)"/>
		/// <exception cref="ArgumentException">
		/// Thrown if the the given <paramref name="type"/> is not of inhereting from <see cref="UiPresenterData{T}"/> class
		/// </exception>
		/// <remarks>
		/// It sets the given <paramref name="initialData"/> data BEFORE opening the UI
		/// </remarks>
		UiPresenter OpenUi<TData>(Type type, TData initialData) where TData : struct;

		/// <summary>
		/// Closes and returns the UI of given type <typeparamref name="T"/>.
		/// If the given <paramref name="closedException"/> is true, then will throw an <see cref="InvalidOperationException"/>
		/// if the <see cref="UiPresenter"/> is already closed.
		/// </summary>
		/// <exception cref="KeyNotFoundException">
		/// Thrown if the service does NOT contain an <see cref="UiPresenter"/> of the given type <typeparamref name="T"/>
		/// </exception>
		UniTask CloseUi<T>(bool destroy = false) where T : UiPresenter;

		/// <summary>
		/// Closes and returns the UI of given <paramref name="type"/>.
		/// If the given <paramref name="closedException"/> is true, then will throw an <see cref="InvalidOperationException"/>
		/// if the <see cref="UiPresenter"/> is already closed.
		/// </summary>
		/// <exception cref="KeyNotFoundException">
		/// Thrown if the service does NOT contain an <see cref="UiPresenter"/> of the given <paramref name="type"/>
		/// </exception>
		UniTask CloseUi(Type type, bool destroy = false);

		/// <summary>
		/// Closes and returns the same given <paramref name="uiPresenter"/>.
		/// If the given <paramref name="closedException"/> is true, then will throw an <see cref="InvalidOperationException"/>
		/// if the <see cref="UiPresenter"/> is already closed.
		/// </summary>
		/// <exception cref="KeyNotFoundException">
		/// Thrown if the service does NOT contain the given <paramref name="uiPresenter"/>
		/// </exception>
		UniTask CloseUi<T>(T uiPresenter, bool destroy = false) where T : UiPresenter;

		/// <summary>
		/// Closes all the visible <seealso cref="UiPresenter"/>
		/// </summary>
		UniTask CloseAllUi();

		/// <summary>
		/// Closes all the visible <seealso cref="UiPresenter"/> in the given <paramref name="layer"/>
		/// </summary>
		UniTask CloseAllUi(int layer);

		/// <summary>
		/// Closes all the visible <seealso cref="UiPresenter"/> in front or in the same layer of the given type <typeparamref name="T"/>
		/// It excludes any visible  <seealso cref="UiPresenter"/> present in layers of the given <paramref name="excludeLayers"/>
		/// </summary>
		/// <exception cref="KeyNotFoundException">
		/// Thrown if the service does NOT contain an <see cref="UiPresenter"/> of the given type <typeparamref name="T"/>
		/// </exception>
		UniTask CloseUiAndAllInFront<T>(params int[] excludeLayers) where T : UiPresenter;

		/// <summary>
		/// Adds the given <paramref name="uiSet"/> to the service
		/// </summary>
		/// <exception cref="ArgumentException">
		/// Thrown if the service already contains the given <paramref name="uiSet"/>
		/// </exception>
		void AddUiSet(UiSetConfig uiSet);

		/// <summary>
		/// Removes and returns all the <see cref="UiPresenter"/> from given <paramref name="setId "/> that are still present in the service
		/// </summary>
		/// <exception cref="KeyNotFoundException">
		/// Thrown if the service does NOT contain an <see cref="UiSetConfig"/> with the given <paramref name="setId"/>.
		/// You need to add it first by calling <seealso cref="AddUiSet"/>
		/// </exception>
		List<UiPresenter> RemoveUiPresentersFromSet(int setId);
		
		/// <summary>
		/// Loads asynchronously all the <see cref="UiPresenter"/> from given <paramref name="setId "/> and have not yet been loaded.
		/// This method can be controlled in an async method and returns every UI when completes loaded.
		/// This method can be controlled in a foreach loop and it will return the UIs in a first-load-first-return scheme 
		/// </summary>
		/// <exception cref="KeyNotFoundException">
		/// Thrown if the service does NOT contain an <see cref="UiSetConfig"/> with the given <paramref name="setId"/>.
		/// You need to add it first by calling <seealso cref="AddUiSet"/>
		/// </exception>
		UniTask<UiPresenter[]> LoadUiSetAsync(int setId);

		/// <summary>
		/// Unloads all the <see cref="UiPresenter"/> from given <paramref name="setId "/> that are still present in the service
		/// </summary>
		/// <exception cref="KeyNotFoundException">
		/// Thrown if the service does NOT contain an <see cref="UiSetConfig"/> with the given <paramref name="setId"/>.
		/// You need to add it first by calling <seealso cref="AddUiSet"/>
		/// </exception>
		void UnloadUiSet(int setId);

		/// <summary>
		/// Checks if the service contains or not the <seealso cref="UiSetConfig"/> of the given <paramref name="setId"/>
		/// </summary>
		bool HasUiSet(int setId);

		/// <summary>
		/// Checks if the service containers all the <seealso cref="UiPresenter"/> belonging in the given <paramref name="setId"/>
		/// </summary>
		/// <exception cref="KeyNotFoundException">
		/// Thrown if the service does NOT contain an <see cref="UiSetConfig"/> with the given <paramref name="setId"/>.
		/// You need to add it first by calling <seealso cref="AddUiSet"/>
		/// </exception>
		bool HasAllUiPresentersInSet(int setId);
		
		/// <summary>
		/// Requests the <seealso cref="UiSetConfig"/> of given type <paramref name="setId"/>
		/// </summary>
		/// <exception cref="KeyNotFoundException">
		/// Thrown if the service does NOT contain an <see cref="UiSetConfig"/> with the given <paramref name="setId"/>.
		/// You need to add it first by calling <seealso cref="AddUiSet"/>
		/// </exception>
		UiSetConfig GetUiSet(int setId);
		
		/// <summary>
		/// Opens all the <seealso cref="UiPresenter"/> that are part of the given <paramref name="setId"/>
		/// If the given <paramref name="closeVisibleUi"/> is set to true, will close the currently open <seealso cref="UiPresenter"/>
		/// that are not part of the given <paramref name="setId"/>
		/// </summary>
		/// <exception cref="KeyNotFoundException">
		/// Thrown if the service does NOT contain an <see cref="UiSetConfig"/> with the given <paramref name="setId"/>.
		/// You need to add it first by calling <seealso cref="AddUiSet"/>
		/// </exception>
		void OpenUiSet(int setId, bool closeVisibleUi);
		
		/// <summary>
		/// Closes all the <seealso cref="UiPresenter"/> that are part of the given <paramref name="setId"/>
		/// </summary>
		/// <exception cref="KeyNotFoundException">
		/// Thrown if the service does NOT contain an <see cref="UiSetConfig"/> with the given <paramref name="setId"/>.
		/// You need to add it first by calling <seealso cref="AddUiSet"/>
		/// </exception>
		void CloseUiSet(int setId);
		
		/// <summary>
		/// Opens and returns the screen of given <paramref name="type"/>. It keeps track of the current open screen and
		/// closes it if another one opens. This way it makes the new screen only open when the current one is finished closing.
		/// Useful to let the current screen make an out animation before being replaced by a new screen.
		/// </summary>
		void OpenScreen<T>() where T : UiPresenter;
		
		/// <summary>
		/// Opens and returns the screen of given <paramref name="type"/>. It keeps track of the current open screen and
		/// closes it if another one opens. This way it makes the new screen only open when the current one is finished closing.
		/// Useful to let the current screen make an out animation before being replaced by a new screen.
		/// </summary>
		UniTask<UiPresenter> OpenScreenAsync<T>() where T : UiPresenter;
		
		/// <summary>
		/// Executes the call asynchronously while loading the Screen asset if needed. It keeps track of the current open screen and
		/// closes it if another one opens. This way it makes the new screen only open when the current one is finished closing.
		/// Useful to let the current screen make an out animation before being replaced by a new screen.
		/// </summary>
		void OpenScreen<T, TData>(TData initialData) 
			where T : UiPresenter, IUiPresenterData 
			where TData : struct;
		
		/// <summary>
		/// Executes the call asynchronously while loading the Screen asset if needed. It keeps track of the current open screen and
		/// closes it if another one opens. This way it makes the new screen only open when the current one is finished closing.
		/// Useful to let the current screen make an out animation before being replaced by a new screen.
		/// </summary>
		UniTask<T> OpenScreenAsync<T, TData>(TData initialData) 
			where T : UiPresenter, IUiPresenterData 
			where TData : struct;
		
		/// <summary>
		/// It closes the current open screen that was opened by OpenScreen
		/// </summary>
		void CloseCurrentScreen();

		/// <summary>
		/// Gets the currently open screen
		/// </summary>
		UiPresenter GetCurrentOpenedScreen();
	}

	/// <inheritdoc />
	public interface IUiServiceInit : IUiService
	{
		/// <summary>
		/// Initialize the service with <paramref name="configs"/> that define the game's UI
		/// </summary>
		/// <remarks>
		/// To help configure the game's UI you need to create a UiConfigs Scriptable object by:
		/// - Right Click on the Project View > Create > ScriptableObjects > Configs > UiConfigs
		/// </remarks>
		/// <exception cref="ArgumentException">
		/// Thrown if any of the <see cref="UiConfig"/> in the given <paramref name="configs"/> is duplicated
		/// </exception>
		void Init(UiConfigs configs);
	}
}