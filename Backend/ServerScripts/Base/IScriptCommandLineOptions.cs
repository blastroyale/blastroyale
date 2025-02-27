using System;
using System.Threading.Tasks;

namespace Scripts.Base;

/// <summary>
/// 
/// </summary>
/// <typeparam name="T">Command line options</typeparam>
public interface IScriptCommandLineOptions<in T>
{
	public Task RunWithOptions(T options);
}

public static class Extensions
{
	public static Type GetOptionsType<T>(this IScriptCommandLineOptions<T> iIScript)
	{
		return typeof(T);
	}
}