using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

// ReSharper disable once CheckNamespace

namespace FirstLight.Services
{
	/// <summary>
	/// This interface provides the access to the player's save persistent data 
	/// </summary>
	public interface IDataProvider
	{
		/// <summary>
		/// Requests the player's data of <typeparamref name="T"/> type
		/// </summary>
		bool TryGetData<T>(out T dat) where T : class;
		
		/// <summary>
		/// Requests the player's data of <typeparamref name="T"/> type
		/// </summary>
		T GetData<T>() where T : class;
	}
	
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
		/// Adds the given <paramref name="data"/> to this logic state to be maintained in memory.
		/// If <paramref name="isLocal"/> then the given <paramref name="data"/> will be saved on the device HD.
		/// </summary>
		void AddData<T>(T data, bool isLocal = false) where T : class;
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
		public T GetData<T>() where T : class
		{
			return _data[typeof(T)].Data as T;
		}

		/// <inheritdoc />
		public void SaveData<T>() where T : class
		{
			var type = typeof(T);

			if (!_data[type].IsLocal)
			{
				return;
			}
			
			PlayerPrefs.SetString(type.Name, JsonConvert.SerializeObject(_data[type].Data));
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
				
				PlayerPrefs.SetString(data.Key.Name, JsonConvert.SerializeObject(data.Value.Data));
			}
			
			PlayerPrefs.Save();
		}

		/// <inheritdoc />
		public T LoadData<T>() where T : class
		{
			var json = PlayerPrefs.GetString(typeof(T).Name, "");
			var instance = string.IsNullOrEmpty(json) ? Activator.CreateInstance<T>() : JsonConvert.DeserializeObject<T>(json);
			
			AddData(instance, true);

			return instance;
		}

		/// <inheritdoc />
		public void AddData<T>(T data, bool isLocal = false) where T : class
		{
			_data.Add(typeof(T), new DataInfo { Data = data, IsLocal = isLocal });
		}

		private struct DataInfo
		{
			public object Data;
			public bool IsLocal;
		}
	}
}