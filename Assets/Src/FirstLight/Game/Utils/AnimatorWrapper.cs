using UnityEngine;

namespace FirstLight.Game.Utils
{
	/// <summary>
	/// Wrapper class for an animator component. Allows us to more easily cache parameter key hashes
	/// and to validate that our hash matches the correct parameter type at compile time.
	/// </summary>
	public class AnimatorWrapper
	{
		private readonly Animator _animator;

		/// <summary>
		/// Constructor.
		/// </summary>
		public AnimatorWrapper(Animator animator)
		{
			_animator = animator;
		}

		public bool Enabled
		{
			set => _animator.enabled = value;
		}

		/// <summary>
		/// Set a bool animation parameter on the wrapped animator component.
		/// </summary>
		public void SetBool(Bool key, bool val)
		{
			_animator.SetBool(key.Hash, val);
		}

		/// <summary>
		/// Get a bool animation parameter on the wrapped animator component.
		/// </summary>
		public bool GetBool(Bool key)
		{
			return _animator.GetBool(key.Hash);
		}

		/// <summary>
		/// Set a trigger animation parameter on the wrapped animator component.
		/// </summary>
		public void SetTrigger(Trigger key)
		{
			_animator.SetTrigger(key.Hash);
		}
		
		/// <summary>
		/// Reset a trigger animation parameter on the wrapped animator component.
		/// </summary>
		public void ResetTrigger(Trigger key)
		{
			_animator.ResetTrigger(key.Hash);
		}

		/// <summary>
		/// Set a float animation parameter on the wrapped animator component.
		/// </summary>
		public void SetFloat(Float key, float val)
		{
			_animator.SetFloat(key.Hash, val);
		}
		
		/// <summary>
		/// Set an integer animation parameter on the wrapped animator component.
		/// </summary>
		public void SetInt(Int key, int val)
		{
			_animator.SetInteger(key.Hash, val);
		}
		
		/// <summary>
		/// Wrapper for a bool animation parameter key hash.
		/// </summary>
		public struct Bool
		{
			public Bool(string key)
			{
				Key = key;
				Hash = Animator.StringToHash(key);
			}
			
			public string Key { get; }
			public int Hash { get; }
		}
		
		/// <summary>
		/// Wrapper for a trigger animation parameter key hash.
		/// </summary>
		public struct Trigger
		{
			public Trigger(string key)
			{
				Key = key;
				Hash = Animator.StringToHash(key);
			}
			
			public string Key { get; }
			public int Hash { get; }
		}
		
		/// <summary>
		/// Wrapper for a float animation parameter key hash.
		/// </summary>
		public struct Float
		{
			public Float(string key)
			{
				Key = key;
				Hash = Animator.StringToHash(key);
			}
			
			public string Key { get; }
			public int Hash { get; }
		}
		
		/// <summary>
		/// Wrapper for a integer animation parameter key hash.
		/// </summary>
		public struct Int
		{
			public Int(string key)
			{
				Key = key;
				Hash = Animator.StringToHash(key);
			}
			
			public string Key { get; }
			public int Hash { get; }
		}
		
		/// <summary>
		/// Wrapper for a state animation parameter key hash.
		/// </summary>
		public struct State
		{
			public State(string key)
			{
				Key = key;
				Hash = Animator.StringToHash(key);
			}
			
			public string Key { get; }
			public int Hash { get; }
		}
	}
}