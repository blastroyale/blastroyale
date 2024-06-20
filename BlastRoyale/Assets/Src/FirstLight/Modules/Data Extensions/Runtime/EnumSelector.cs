using System;
using System.Collections.Generic;
using UnityEngine;

// ReSharper disable once CheckNamespace

namespace FirstLight
{
	/// <summary>
	/// Tags the current object as an <seealso cref="EnumSelector{T}"/>
	/// </summary>
	public interface IEnumSelector
	{
		/// <summary>
		/// Requests the enum index number of current selected enum
		/// </summary>
		int GetSelectedIndex();

		/// <summary>
		/// Requests if the selected enum is a valid enum.
		/// If the enum values was changed and the selected string was removed, then the selected enum will be invalid.
		/// </summary>
		bool HasValidSelection();

		/// <summary>
		/// Requests the enum selected value as string
		/// </summary>
		string GetSelectionString();
	}
	
	/// <summary>
	/// The EnumSelector <typeparamref name="T"/> serves as a dropdown selection field that offers all enum values of <typeparamref name="T"/>. 
	/// It stores the enum name instead of the enum value, to prevent pointing to the wrong enum when new values are added or removed
	/// </summary>
	[Serializable]
	public class EnumSelector<T> : IEnumSelector where T : Enum
	{
		[SerializeField, HideInInspector] private string _selection = "";
 
		public static readonly string[] EnumNames = Enum.GetNames(typeof(T));
		public static readonly T[] EnumValues = (T[])Enum.GetValues(typeof(T));
		public static readonly Dictionary<string, T> EnumDictionary = new Dictionary<string, T>();

		private EnumSelector()
		{
			SetSelection(EnumValues[0]);
		}

		protected EnumSelector(T data)
		{
			SetSelection(data);
		}
		
		/// <inheritdoc />
		public int GetSelectedIndex()
		{
			for (var i = 0; i < EnumNames.Length; i++)
			{
				if (EnumNames[i].ToLower().Equals(_selection.ToLower()))
				{
					return i;
				}
			}
			
			Debug.LogError($"Could not load enum for string: {_selection}");
			
			return -1;
		}
 
		/// <inheritdoc />
		public bool HasValidSelection()
		{
			return GetSelectedIndex() != -1;
		}
 
		/// <inheritdoc />
		public string GetSelectionString()
		{
			return _selection;
		}
 
		/// <summary>
		/// Requests the enum selected value
		/// </summary>
		public T GetSelection()
		{
			if (EnumDictionary.TryGetValue(_selection, out var enumConstant))
			{
				return enumConstant;
			}
 
			enumConstant = EnumValues[GetSelectedIndex()];
			EnumDictionary.Add(_selection, enumConstant);
 
			return enumConstant;
		}
 
		/// <summary>
		/// Sets the enum value to <paramref name="data"/>
		/// </summary>
		public void SetSelection(T data)
		{
			_selection = EnumNames[(int) (object) data];
		}
 
		public static implicit operator T(EnumSelector<T> d)
		{
			return d.GetSelection();
		}
	}
}