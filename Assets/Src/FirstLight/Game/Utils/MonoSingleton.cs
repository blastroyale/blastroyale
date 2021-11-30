using UnityEngine;

namespace FirstLight.Game.Utils
{
	/// <summary>
	/// Singleton MonoBehaviour class pattern.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public class MonoSingleton<T> : MonoBehaviour where T : MonoSingleton<T>
	{
		private static T _sInstance;
		
		protected virtual void _Start()
		{
		}

		protected virtual void _OnEnable()
		{
		}

		protected virtual void _OnDisable()
		{
		}

		protected virtual void _OnDestroy()
		{
		}

		public static T Instance
		{
			get
			{
				Debug.Assert(_sInstance, $"MonoSingleton<{typeof(T).Name}> is null");
				return _sInstance;
			}
		}

		public static bool IsInstanceNull()
		{
			return _sInstance == null;
		}

		private bool SetupInstance()
		{
			if (_sInstance == this)
			{
				return true;
			}
			
			if (_sInstance == null)
			{
				_sInstance = this as T;
				
				return true;
			}
		
			Debug.LogError(string.Format("Double singleton. Killing second instance ({0})", gameObject.name));
			Destroy(this);
			
			return false;
		}

		private void Awake()
		{
			SetupInstance();
		}

		//Do not hide/override. Use _Start instead
		protected virtual void Start()
		{
			if (SetupInstance())
			{
				_Start();
			}
		}

		//Do not hide/override. Use _OnEnable instead
		protected void OnEnable()
		{
			if (SetupInstance())
			{
				_OnEnable();
			}
		}

		//Do not hide/override. Use _OnEnable instead
		protected void OnDisable()
		{
			if (SetupInstance())
			{
				_OnDisable();
			}
		}

		//Do not hide/override. Use _OnDestroy instead
		protected void OnDestroy()
		{
			if (_sInstance == this)
			{
				_OnDestroy();
				_sInstance = null;
			}
		}
	}
}