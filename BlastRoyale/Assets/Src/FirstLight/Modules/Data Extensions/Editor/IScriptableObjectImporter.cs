using System;

// ReSharper disable once CheckNamespace

namespace FirstLightEditor
{
	public interface IScriptableObjectImporter
	{
		Type ScriptableObjectType { get; }
	}
}