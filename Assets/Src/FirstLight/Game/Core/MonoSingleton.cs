using UnityEngine;

namespace FirstLight.Game.Core
{
	public class MonoSingleton<T> : MonoBehaviour where T : MonoSingleton<T>
	{
		protected static T s_Instance;

		//Alternate Awake and Enable methods provided.
		//The normal methods cannot be used, because this superclass is using
		//them to listen to Unity event, and does not want to rely on subclasses calling base.Awake
		
		protected virtual void _Awake()
		{
		}
		
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
				//Debug.Assert(s_Instance, string.Format("MonoSingleton<{0}> is null", typeof(T).Name));
				return s_Instance;
			}
		}

		public static bool IsInstanceNull()
		{
			return s_Instance == null;
		}

		private bool SetupInstance()
		{
			if (s_Instance == this)
				return true;
			if (s_Instance == null)
			{
				s_Instance = this as T;
				return true;
			}
			else
			{
				Debug.LogError(string.Format("DOUBLE SINGLETON. KILLING SECOND INSTANCE ({0})", gameObject.name));
				Destroy(this);
				return false;
			}
		}

		private void Awake()
		{
			SetupInstance();
			
			_Awake();
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
			if (s_Instance == this)
			{
				_OnDestroy();
				s_Instance = null;
			}
		}
	}
}