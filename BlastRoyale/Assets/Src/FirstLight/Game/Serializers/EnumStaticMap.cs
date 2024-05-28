using System;
using System.Collections.Generic;
using System.Linq;

namespace FirstLight.Game.Serializers
{
	public class EnumStaticMap<TEnum> where TEnum : struct, Enum
	{
		private Dictionary<TEnum, int> _enumToInt;
		private Dictionary<int, TEnum> _intToEnum;

		public EnumStaticMap(Dictionary<TEnum, int> mapping)
		{
			_enumToInt = mapping;
			_intToEnum = mapping.ToDictionary(kv => kv.Value, kv => kv.Key);
		}


		public int GetIntValue(TEnum value)
		{
			if (_enumToInt.TryGetValue(value, out int byteValue))
			{
				return byteValue;
			}

			throw new ArgumentException("Invalid enum value");
		}

		public TEnum GetValue(int? intValue)
		{
			if (intValue != null && _intToEnum.TryGetValue((int) intValue, out TEnum value))
			{
				return value;
			}

			throw new ArgumentException("Invalid int value");
		}
	}
}