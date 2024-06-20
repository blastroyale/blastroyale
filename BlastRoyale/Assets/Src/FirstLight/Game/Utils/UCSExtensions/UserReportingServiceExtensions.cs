using System;
using Cysharp.Threading.Tasks;
using Unity.Services.UserReporting;

namespace FirstLight.Game.Utils.UCSExtensions
{
	/// <summary>
	/// Async extensions for the UserReportingService.
	/// </summary>
	public static class UserReportingServiceExtensions
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
			}, result =>
			{
				if (!result)
				{
					completionSource.TrySetException(new Exception("Error sending user report."));
				}
				else
				{
					completionSource.TrySetResult();
				}
			});

			return completionSource.Task;
		}
	}
}