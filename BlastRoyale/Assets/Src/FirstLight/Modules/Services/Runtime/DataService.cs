using System;
using System.Collections.Generic;
using FirstLight.Server.SDK.Models;
using Newtonsoft.Json;
using UnityEngine;

// ReSharper disable once CheckNamespace

namespace FirstLight.Services
{
	/// <summary>
	/// This interface provides the possibility to the current memory data to disk
	/// </summary>
	public interface IDataSaver
	{
		/// <summary>
		/// Saves the game's given <typeparamref name="T"/> data to disk
		/// </summary>
		void SaveData<T>() where T : class;

		/// <summary>
		/// Saves all game's data to disk
		/// </summary>
		void SaveAllData();
	}

	/// <summary>
	/// This interface provides the possibility to load data from disk
	/// </summary>
	public interface IDataLoader
	{
		/// <summary>
		/// Loads the game's given <typeparamref name="T"/> data from disk and returns it
		/// </summary>
		T LoadData<T>() where T : class;
	}

	/// <summary>
	/// This service allows to manage all the persistent data in the game.
	/// Data are strictly reference types to guarantee that there is no boxing/unboxing and lost of referencing when changing it's data.
	/// </summary>
	public interface IDataService : IDataProvider, IDataSaver, IDataLoader
	{
		/// <summary>
		/// Generic wrapper of <see cref="AddData"/>
		/// </summary>
		void AddData<T>(T data, bool isLocal = false) where T : class;

		/// <summary>
		/// Adds the given <paramref name="data"/> to this logic state to be maintained in memory.
		/// If <paramref name="isLocal"/> then the given <paramref name="data"/> will be saved on the device HD.
		/// </summary>
		void AddData(Type type, object data, bool isLocal = false);
	}

	/// <inheritdoc />
	public class DataService : IDataService
	{
		private readonly IDictionary<Type, DataInfo> _data = new Dictionary<Type, DataInfo>();

		/// <inheritdoc />
		public bool TryGetData<T>(out T data) where T : class
		{
			var ret = _data.TryGetValue(typeof(T), out var dataInfo);

			data = dataInfo.Data as T;

			return ret;
		}

		/// <inheritdoc />
		public bool TryGetData(Type type, out object dat)
		{
			var ret = _data.TryGetValue(type, out var dataInfo);

			dat = dataInfo.Data;

			return ret;
		}

		/// <inheritdoc />
		public object GetData(Type type)
		{
			return _data[type].Data;
		}

		/// <inheritdoc />
		public IEnumerable<Type> GetKeys()
		{
			return _data.Keys;
		}

		/// Obtains the client data stored in memory.
		/// Always created the data if its not present using the default constructor.
		public T GetData<T>() where T : class
		{
			return GetDataOrCreateIfNeeded<T>();
		}

		/// <inheritdoc />
		public void SaveData<T>() where T : class
		{
			var type = typeof(T);

			if (!_data[type].IsLocal)
			{
				return;
			}

			var jsonData = JsonConvert.SerializeObject(_data[type].Data);
			PlayerPrefs.SetString(ConvertKey(type.Name), jsonData);
			PlayerPrefs.Save();
		}

		/// <inheritdoc />
		public void SaveAllData()
		{
			foreach (var data in _data)
			{
				if (!data.Value.IsLocal)
				{
					continue;
				}

				PlayerPrefs.SetString(ConvertKey(data.Key.Name), JsonConvert.SerializeObject(data.Value.Data));
			}

			PlayerPrefs.Save();
		}

		/// <inheritdoc />
		public T LoadData<T>() where T : class
		{
			var json = PlayerPrefs.GetString(ConvertKey(typeof(T).Name), "");
			var instance = string.IsNullOrEmpty(json) ? Activator.CreateInstance<T>() : JsonConvert.DeserializeObject<T>(json);

			AddData(instance, true);

			return instance;
		}

		/// <inheritdoc />
		public void AddData<T>(T data, bool isLocal = false) where T : class
		{
			_data[typeof(T)] = new DataInfo {Data = data, IsLocal = isLocal};
		}

		/// <inheritdoc />
		public void AddData(Type type, object data, bool isLocal = false)
		{
			_data[type] = new DataInfo {Data = data, IsLocal = isLocal};
		}

		private T GetDataOrCreateIfNeeded<T>() where T : class
		{
			if (!TryGetData<T>(out var data))
			{
				data = Activator.CreateInstance<T>();
				_data[typeof(T)] = new DataInfo {Data = data, IsLocal = false};
				;
			}

			return data;
		}

		/// <summary>
		/// Convert a PlayerPref keys to take ParellSync Clones into account
		/// </summary>
		private String ConvertKey(String key)
		{
#if UNITY_EDITOR
			if (ParrelSync.ClonesManager.IsClone())
			{
				return key + "_clone_" + ParrelSync.ClonesManager.GetArgument();
			}
#endif
			return key;
		}

		private struct DataInfo
		{
			public object Data;
			public bool IsLocal;
		}
	}
}