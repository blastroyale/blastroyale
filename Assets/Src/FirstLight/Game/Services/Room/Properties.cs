using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ExitGames.Client.Photon;

namespace FirstLight.Game.Services.RoomService
{
	public interface IQuantumProperty
	{
		public string Key { get; }
		public bool Expose { get; }
		public bool HasValue { get; }

		void FromRaw(object value);
		object ToRaw();
	}

	public class QuantumProperty<T> : IQuantumProperty
	{
		public string Key { get; }

		/// <summary>
		/// Callback for when the local player changes a property
		/// </summary>
		public event Action<QuantumProperty<T>> OnLocalPlayerSet;

		/// <summary>
		/// Generic callback for when the value changes, it might be received an update from server, or local player changed it
		/// </summary>
		public event Action<QuantumProperty<T>> OnValueChanged;

		public bool Expose { get; private set; }
		public bool HasValue { get; private set; }

		public T Value
		{
			get => _currentValue;
			set
			{
				SetInternal(value);
				OnLocalPlayerSet?.Invoke(this);
			}
		}

		protected T _currentValue;

		public QuantumProperty(string key, bool expose = false)
		{
			Key = key;
			Expose = expose;
			HasValue = false;
		}


		public virtual void FromRaw(object value)
		{
			if (value == null)
			{
				SetInternal(default);
				return;
			}
			SetInternal((T) value);
		}

		public virtual object ToRaw()
		{
			return _currentValue;
		}

		protected void SetInternal(T value)
		{
			HasValue = true;
			var changed = !Equals(_currentValue,value);
			_currentValue = value;
			if (changed)
			{
				OnValueChanged?.Invoke(this);
			}
		}
	}

	public class EnumProperty<T> : QuantumProperty<T> where T : struct, Enum
	{
		public override void FromRaw(object value)
		{
			SetInternal(Enum.Parse<T>(value.ToString()));
		}

		public override object ToRaw()
		{
			return _currentValue.ToString();
		}

		public EnumProperty(string key, bool expose = false) : base(key, expose)
		{
		}
	}
	public class ListEnumQuantumProperty<T> : QuantumProperty<List<T>> where T : struct, Enum
	{
		public ListEnumQuantumProperty(string key, bool expose) : base(key, expose)
		{
		}

		public override void FromRaw(object value)
		{
			var str = (string) value;
			if (string.IsNullOrEmpty(str.Trim()))
			{
				SetInternal(new List<T>());
				return;
			}

			var list = str.Split(",");

			var convertedList = new List<T>();
			foreach (var enumKey in list)
			{
				if (!Enum.TryParse<T>(enumKey, out var enumValue)) throw new Exception("Enum value not found for key " + enumKey);
				convertedList.Add(enumValue);

			}

			SetInternal(convertedList);
		}

		public override object ToRaw()
		{
			return string.Join(",", _currentValue.Select(e => Enum.GetName(typeof(T), e)));
		}
	}

	public class ListQuantumProperty : QuantumProperty<List<string>>
	{
		public ListQuantumProperty(string key, bool expose) : base(key, expose)
		{
		}

		public override void FromRaw(object value)
		{
			var str = (string) value;
			if (string.IsNullOrEmpty(str.Trim()))
			{
				SetInternal(new List<string>());
				return;
			}

			var list = str.Split(",");
			SetInternal(list.ToList());
		}

		public override object ToRaw()
		{
			return string.Join(",", _currentValue);
		}
	}
	
	public class PropertiesHolder
	{
		private List<IQuantumProperty> _allProperties = new();
        

		public delegate void OnSetPropertyCallback(string key, object value);

		public event OnSetPropertyCallback OnLocalPlayerSetProperty;
        

		private void InitProperty<T>(QuantumProperty<T> property)
		{
			_allProperties.Add(property);
			property.OnLocalPlayerSet += OnLocalPlayerSetPropertyCallback;
		}

		protected QuantumProperty<T> Create<T>(string key, bool expose = false)
		{
			var property = new QuantumProperty<T>(key, expose);
			InitProperty(property);
			return property;
		}

		protected QuantumProperty<T> CreateEnum<T>(string key, bool expose = false) where T : struct, Enum
		{
			var property = new EnumProperty<T>(key, expose);
			InitProperty(property);
			return property;
		}

		protected ListQuantumProperty CreateList(string key, bool expose = false)
		{
			var property = new ListQuantumProperty(key, expose);
			InitProperty(property);
			return property;
		}

		protected ListEnumQuantumProperty<T> CreateEnumList<T>(string key, bool expose = false) where T : struct, Enum
		{
			var property = new ListEnumQuantumProperty<T>(key, expose);
			InitProperty(property);
			return property;
		}


		private void OnLocalPlayerSetPropertyCallback(IQuantumProperty property)
		{
			OnLocalPlayerSetProperty?.Invoke(property.Key, property.ToRaw());
		}

		public void OnReceivedPropertyChange(string key, object value)
		{
			var roomProperty = _allProperties.Find(e => e.Key == key);
			roomProperty?.FromRaw(value);
		}

		public string[] GetExposedPropertiesIds()
		{
			return _allProperties.Where(e => e.Expose).Select(e => e.Key).ToArray();
		}

		public Hashtable ToHashTable()
		{
			var table = new Hashtable();
			foreach (var roomProperty in _allProperties.Where(roomProperty => roomProperty.HasValue))
			{
				table.Add(roomProperty.Key, roomProperty.ToRaw());
			}

			return table;
		}
	}
}