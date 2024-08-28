using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Quantum;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FirstLight.Game.Utils
{
	[Serializable]
	public class SerializedDictionary<TKey,TValue> : UnitySerializedDictionary<TKey, TValue>
	{
	}
	
	public static class ConfigDictExtensions
	{
		public static TV Get<TK, TV>(this  List<Pair<TK, TV>> d, TK key) => d.First(kp => kp.Key.Equals(key)).Value;

		public static Dictionary<TK, TV> AsDictionary<TK, TV>(this  List<Pair<TK, TV>> d) => d.ToDictionary(kp => kp.Key, kp => kp.Value);
	}
}
