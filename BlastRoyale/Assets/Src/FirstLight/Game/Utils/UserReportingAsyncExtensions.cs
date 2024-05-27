using System;
using Cysharp.Threading.Tasks;
using FirstLight.FLogger;
using Unity.Services.UserReporting;

namespace FirstLight.Game.Utils
{
	/// <summary>
	/// Async extensions for the UserReportingService.
	/// </summary>
	public static class UserReportingAsyncExtensions
	{
		/// <inheritdoc cref="IUserReporting.CreateNewUserReport"/>
		public static UniTask CreateNewUserReportAsync(this IUserReporting userReportingService)
		{
			var completionSource = new UniTaskCompletionSource();

			userReportingService.CreateNewUserReport(() =>
			{
				completionSource.TrySetResult();
			});

			return completionSource.Task;
		}

		/// <inheritdoc cref="IUserReporting.SendUserReport"/>
		public static UniTask SendUserReportAsync(this IUserReporting userReportingService, Action<float> progress = null)
		{
			var completionSource = new UniTaskCompletionSource();

			userReportingService.SendUserReport(p =>
			{
				progress?.Invoke(p);
			}, (result) =>
			{
				if (!result)
				{
					FLog.Error("Error sending user report.");
					return;
				}

				completionSource.TrySetResult();
			});

			return completionSource.Task;
		}
	}
}