using System.Collections.Generic;
using UnityEngine;

namespace FirstLight.Game.Utils
{
	/// <summary>
	/// Unity does not support Dictionary serialization out of the box, but by
	/// exploiting Unity's serialization protocol it can be done.
	/// By making a new class that inherits both Dictionary and Unity's ISerializationCallbackReceiver interface,
	/// we can convert the Dictionary data to a format that Unity can serialize.
	///
	/// IMPORTANT: Because Unity does not serialize generic types, it is necessary to make a concrete
	/// Dictionary type by inheriting from the UnitySerializedDictionary.
	/// </summary>
	public abstract class UnitySerializedDictionary<TKey, TValue> : Dictionary<TKey, TValue>,
	                                                                ISerializationCallbackReceiver
	{
		[SerializeField, Quantum.Inspector.HideInInspector]
		private List<TKey> _keyData = new();

		[SerializeField, Quantum.Inspector.HideInInspector]
		private List<TValue> _valueData = new();

		void ISerializationCallbackReceiver.OnAfterDeserialize()
		{
			Clear();
			for (var i = 0; i < _keyData.Count && i < _valueData.Count; i++)
			{
				this[_keyData[i]] = _valueData[i];
			}
		}

		void ISerializationCallbackReceiver.OnBeforeSerialize()
		{
			_keyData.Clear();
			_valueData.Clear();

			foreach (var item in this)
			{
				_keyData.Add(item.Key);
				_valueData.Add(item.Value);
			}
		}
	}
}