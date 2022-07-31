namespace Scripts;

/// <summary>
/// Script arguments utility
/// </summary>
public class ScriptParameters : Dictionary<string, object> {

	public T? Get<T>(string key)
	{
		if (TryGetValue(key, out var o))
		{
			return (T)o;
		}
		return default(T);
	}
}

/// <summary>
/// Executable server script
/// </summary>
public interface IScript
{
	void Execute(ScriptParameters args);
}