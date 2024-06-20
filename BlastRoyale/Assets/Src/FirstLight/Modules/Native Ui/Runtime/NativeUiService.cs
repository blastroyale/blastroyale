using System;
using UnityEngine;

// ReSharper disable once CheckNamespace

namespace FirstLight.NativeUi
{
	public enum AlertButtonStyle
	{
		Default,
		Positive,
		Negative
	}

	public struct AlertButton
	{
		public string Text;
		public AlertButtonStyle Style;
		public Action Callback;
	}
	
	/// <summary>
	/// This service provides the functionality to call native UI screens
	/// </summary>
	public static class NativeUiService
	{
		/// <summary>
		/// Shows an alert native OS message popup with the given <paramref name="title"/>, <paramref name="message"/>
		/// and the <paramref name="buttons"/> ordered from left to right.
		/// If on iOS device, it can be set the pop up to be visible as an alert sheet depending on the given <paramref name="isAlertSheet"/>
		/// </summary>
		/// <exception cref="SystemException">
		/// Thrown if the current platform is not iOS nor Android
		/// </exception>
		public static void ShowAlertPopUp(bool isAlertSheet, string title, string message, params AlertButton[] buttons)
		{
#if UNITY_EDITOR
			Debug.Log($"Show Alert Pop Up is not available in the editor and was triggered with: {title} - {message}");
#elif UNITY_IOS
			_currentButtons = buttons ?? throw new ArgumentException("The buttons count must be higher than zero");

			var buttonsText = new string[buttons.Length];
			var buttonsStyle = new int[buttons.Length];

			for (var i = 0; i < buttons.Length; i++)
			{
				buttonsText[i] = buttons[i].Text;
				buttonsStyle[i] = (int) buttons[i].Style;
			}

			AlertMessage(isAlertSheet, title, message, buttonsText, buttonsStyle, buttons.Length, AlertButtonCallback);
#elif UNITY_ANDROID
			using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
			using (var unityActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
			using (var alertDialogBuilder = new AndroidJavaObject("android.app.AlertDialog$Builder", unityActivity))
			using (var alertDialog = alertDialogBuilder.Call<AndroidJavaObject>("create"))
			{
				alertDialog.Call("setTitle", title);
				alertDialog.Call("setMessage", message);
				
				for (var i = 0; i < buttons.Length; i++) 
				{
					alertDialog.Call("setButton", ConvertToAndroidStyle(buttons[i].Style), 
						buttons[i].Text, new AndroidButtonCallback(buttons[i].Callback));
				}
				
				alertDialog.Call("show");
			}
#else
			throw new SystemException("Show an alert Pop Up is only available for iOS and Android platforms");
#endif
		}

		/// <summary>
		/// Shows a toast native OS message popup with the given <paramref name="message"/>.
		/// This toast will be available on the screen for 3.5sec or 2sec depending
		/// on the given <paramref name="isLongDuration"/> information
		/// </summary>
		/// <exception cref="SystemException">
		/// Thrown if the current platform is not iOS nor Android
		/// </exception>
		public static void ShowToastMessage(string message, bool isLongDuration)
		{
#if UNITY_EDITOR
			Debug.Log($"Show Toast message is not available in the editor and was triggered with: {message}");
#elif UNITY_IOS
			ToastMessage(message, isLongDuration);
#elif UNITY_ANDROID
			using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
			using (var unityActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
			using (var toastClass = new AndroidJavaClass("android.widget.Toast"))
			{
				var duration = isLongDuration ? toastClass.GetStatic<int>("LENGTH_LONG") : toastClass.GetStatic<int>("LENGTH_SHORT");
				var toast = toastClass.CallStatic<AndroidJavaObject>("makeText", unityActivity, message, duration);
				
				toast.Call("show");
				
				toast.Dispose();
			}
#else
			throw new SystemException("Show a Toast message is only available for iOS and Android platforms");
#endif
		}
		
#if UNITY_IOS
		internal delegate void AlertButtonDelegate(string buttonText);
		
		[System.Runtime.InteropServices.DllImport("__Internal")] 
		private static extern void AlertMessage(bool isSheet, string title, string message, string[] buttonsText, 
			int[] buttonsStyle, int buttonsLength, AlertButtonDelegate alertButtonCallback);
		
		[System.Runtime.InteropServices.DllImport("__Internal")] 
		private static extern void ToastMessage(string message, bool isLongDuration);

		[AOT.MonoPInvokeCallback(typeof(AlertButtonDelegate))]
		private static void AlertButtonCallback(string buttonText)
		{
			if (_currentButtons == null)
			{
				return;
			}

			foreach (var button in _currentButtons)
			{
				if (button.Text == buttonText)
				{
					button.Callback?.Invoke();
					break;
				}
			}
		}

		private static AlertButton[] _currentButtons;
#elif UNITY_ANDROID
		private class AndroidButtonCallback : AndroidJavaProxy
		{
			private readonly Action _callback;
			
			public AndroidButtonCallback(Action callback) : base("android.content.DialogInterface$OnClickListener")
			{
				_callback = callback;
			}

			// ReSharper disable once InconsistentNaming
			public void onClick(AndroidJavaObject dialog, int which)
			{
				dialog.Call("dismiss");

				_callback();
			}
		}

		private static int ConvertToAndroidStyle(AlertButtonStyle style)
		{
			switch (style)
			{
				case AlertButtonStyle.Default:
					return -3;
				case AlertButtonStyle.Positive:
					return -1;
				case AlertButtonStyle.Negative:
					return -2;
				default:
					throw new ArgumentOutOfRangeException(nameof(style), style, "Wrong given style");
			}
		}
#endif
	}
}