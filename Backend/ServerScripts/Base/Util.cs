using System;
using System.Threading.Tasks;
using PlayFab;
using PlayFab.Internal;

namespace Scripts.Base;

public static class Util
{
	public static async Task<T> HandleError<T>(this Task<PlayFabResult<T>> task) where T : PlayFabResultCommon
	{
		var result = await task;
		if (result.Error == null) return result.Result;
		throw new Exception($"Playfab Error {result.Error.ErrorMessage}:{result.Error.GenerateErrorReport()}");
	}
}