using System;
using System.Collections.Generic;
using System.Linq;
using NSubstitute.Core;

namespace FirstLight.Game.Services.RoomService
{
	public interface IRoomProperty
	{
		public string Key { get; }
		public bool Expose { get; }
		public bool HasValue { get; }

		void FromRaw(object value);
		object ToRaw();
	}

	public class RoomProperty<T> : IRoomProperty
	{
		public string Key { get; }

		/// <summary>
		/// Callback for when the local player changes a property
		/// </summary>
		public event Action<RoomProperty<T>> OnLocalPlayerSet;
		/// <summary>
		/// Generic callback for when the value changes, it might be received an update from server, or local player changed it
		/// </summary>
		public event Action<RoomProperty<T>> OnValueChanged;

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

		public RoomProperty(string key,bool expose = false)
		{
			Key = key;
			Expose = expose;
			HasValue = false;
		}


		public virtual void FromRaw(object value)
		{
			SetInternal((T)value);
		}

		public virtual object ToRaw()
		{
			return _currentValue;
		}

		protected void SetInternal(T value)
		{
			HasValue = true;
			var changed = !value.Equals(_currentValue);
			_currentValue = value;
			if (changed)
			{
				OnValueChanged?.Invoke(this);
			}
		}
	}

	public class EnumProperty<T> : RoomProperty<T> where T : struct, Enum
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

	public class ListRoomProperty : RoomProperty<List<string>>
	{
		public ListRoomProperty(string key, bool expose) : base(key)
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
			return _currentValue.Join(",");
		}
	}
}