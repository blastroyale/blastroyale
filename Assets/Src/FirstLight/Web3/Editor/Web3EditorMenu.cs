using System;
using Cysharp.Threading.Tasks;
using FirstLight.Game.Utils;
using UnityEditor;
using Debug = UnityEngine.Debug;

namespace FirstLight.Editor.EditorTools
{
	public class Web3EditorShortcuts
	{
		private static FlgImxWeb3Service Web3 => (FlgImxWeb3Service) MainInstaller.ResolveWeb3();

		[MenuItem("FLG/Web3/Login")]
		private static void Login()
		{
			Web3.OnLoginRequested().Forget();
		}

		[MenuItem("FLG/Web3/Logout")]
		private static void Logout()
		{
			Web3.OnLogoutRequested().Forget();
		}

		[MenuItem("FLG/Web3/PrintState")]
		private static void PrintState()
		{
			DebugState().Forget();
		}

		private static async UniTaskVoid DebugState()
		{
			var Web3 = Web3EditorShortcuts.Web3;
			try
			{
				Debug.Log($"[Imx] Token: {await Web3.Passport.GetAccessToken()}");
			}
			catch (Exception e)
			{
				Debug.LogError("Error obtaining Token");
				Debug.LogError(e);
			}
			try
			{
				Debug.Log($"[Imx] Address: {await Web3.Passport.GetAddress()}");
			}
			catch (Exception e)
			{
				Debug.LogError("Error obtaining Address");
				Debug.LogError(e);
			}
			try
			{
				Debug.Log($"[Imx] Email: {await Web3.Passport.GetEmail()}");
			}
			catch (Exception e)
			{
				Debug.LogError("Error obtaining Email");
				Debug.LogError(e);
			}
			try
			{
				Debug.Log($"[Imx] IdToken: {await Web3.Passport.GetIdToken()}");
			}
			catch (Exception e)
			{
				Debug.LogError("Error obtaining IdToken");
				Debug.LogError(e);
			}
		}
	}
}