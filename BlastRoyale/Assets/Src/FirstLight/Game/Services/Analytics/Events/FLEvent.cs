using System.Collections.Generic;
using System.Reflection;
using Firebase.Analytics;
using Unity.Services.Analytics;

namespace FirstLight.Game.Services.Analytics.Events
{
	public abstract class FLEvent : Event
	{
		protected FLEvent(string name) : base(name)
		{
		}

		public Parameter[] ToFirebaseParameters()
		{
			// Reflection to support legacy firebase analytics
			var eventType = typeof(Event);
			var stringsFieldInfo = eventType.GetField("m_Strings", BindingFlags.NonPublic | BindingFlags.Instance);
			var integersFieldInfo = eventType.GetField("m_Integers", BindingFlags.NonPublic | BindingFlags.Instance);
			var booleansFieldInfo = eventType.GetField("m_Booleans", BindingFlags.NonPublic | BindingFlags.Instance);
			var floatsFieldInfo = eventType.GetField("m_Floats", BindingFlags.NonPublic | BindingFlags.Instance);

			var stringsDict = (Dictionary<string, string>) stringsFieldInfo!.GetValue(this);
			var integersDict = (Dictionary<string, long>) integersFieldInfo!.GetValue(this);
			var booleansDict = (Dictionary<string, bool>) booleansFieldInfo!.GetValue(this);
			var floatsDict = (Dictionary<string, double>) floatsFieldInfo!.GetValue(this);

			var parameters = new Parameter[stringsDict.Count + integersDict.Count + booleansDict.Count + floatsDict.Count];

			var index = 0;

			foreach (var (key, value) in stringsDict)
			{
				parameters[index++] = new Parameter(key, value);
			}

			foreach (var (key, value) in integersDict)
			{
				parameters[index++] = new Parameter(key, value);
			}

			foreach (var (key, value) in booleansDict)
			{
				parameters[index++] = new Parameter(key, value.ToString());
			}

			foreach (var (key, value) in floatsDict)
			{
				parameters[index++] = new Parameter(key, value);
			}

			return parameters;
		}

		public string ToFirebaseEventName()
		{
			var eventType = typeof(Event);
			var nameFieldInfo = eventType.GetField("Name", BindingFlags.NonPublic | BindingFlags.Instance);

			return (string) nameFieldInfo!.GetValue(this);
		}
	}
}