using System;
using Cysharp.Threading.Tasks;

namespace FirstLight.UIService
{
	public static class UIServiceExtensions
	{
		/// <summary>
		/// Open a screen UIPresenterResult and wait for the result
		/// </summary>
		/// <param name="uiService"></param>
		/// <param name="close"></param>
		/// <typeparam name="T"></typeparam>
		/// <typeparam name="S"></typeparam>
		/// <returns></returns>
		public static async UniTask<S> OpenScreenAndGetResult<T, S>(this FirstLight.UIService.UIService uiService, bool close = true)
			where T : UIPresenterResult<S>
		{
			var screen = await uiService.OpenScreen<T>();
			var result = await screen.WaitForResult();
			if (close)
			{
				await screen.Close();
			}
			else
			{
				screen.ResetResult();
			}

			return result;
		}
	}
}