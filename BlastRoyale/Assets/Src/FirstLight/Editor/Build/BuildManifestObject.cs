#if !UNITY_CLOUD_BUILD

using System;
using System.Collections.Generic;

namespace UnityEngine.CloudBuild
{
	/// <summary>
	/// The Unity cloud build manifest. This class is only available when building with Unity Cloud Build,
	/// so we need to add it here to avoid compilation errors.
	/// </summary>
	public class BuildManifestObject : ScriptableObject
	{
		// Try to get a manifest value - returns true if key was found and could be cast to type T, otherwise returns false.
		public bool TryGetValue<T>(string key, out T result) => throw new NotImplementedException();

		// Retrieve a manifest value or throw an exception if the given key isn't found.
		public T GetValue<T>(string key) => throw new NotImplementedException();

		// Set the value for a given key.
		public void SetValue(string key, object value) => throw new NotImplementedException();

		// Copy values from a dictionary. ToString() will be called on dictionary values before being stored.
		public void SetValues(Dictionary<string, object> sourceDict) => throw new NotImplementedException();

		// Remove all key/value pairs.
		public void ClearValues() => throw new NotImplementedException();

		// Return a dictionary that represents the current BuildManifestObject.
		public Dictionary<string, object> ToDictionary() => throw new NotImplementedException();

		// Return a JSON formatted string that represents the current BuildManifestObject
		public string ToJson() => throw new NotImplementedException();

		// Return an INI formatted string that represents the current BuildManifestObject
		public override string ToString() => throw new NotImplementedException();
	}
}
#endif