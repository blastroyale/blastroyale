using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace FirstLight.Game.MonoComponent.Vfx
{
	/// <summary>
	/// Base spawner for spawning VFX on the defined position
	/// </summary>
	public abstract class VfxSpawnerBase : MonoBehaviour
	{
		[SerializeField, Required] protected Transform _target;
		[SerializeField] protected Vector3 _offset = Vector3.zero;
		[SerializeField] private Vector3 _additionalRotation = Vector3.zero;

		/// <summary>
		/// Spawn a new Spawner Vfx Instance
		/// </summary>
		public abstract void Spawn();

		protected void OnValidate()
		{
			_target = _target ? _target : transform;
		}

		protected Vector3 GetSpawnPosition()
		{
			return _target.position +  _target.TransformDirection(_offset);
		}

		protected Quaternion GetSpawnRotation()
		{
			return _target.rotation * Quaternion.Euler(_additionalRotation);
		}
		
#if UNITY_EDITOR
		// For use by the custom editor.
		public Vector3 SpawnPosition => GetSpawnPosition();
		public Quaternion SpawnRotation => GetSpawnRotation();
		public Vector3 SpawnTargetPosition => _target.position; 
		public Vector3 Offset { set => _offset = _target.InverseTransformDirection(value); }
		public Vector3 AdditionalRotation { set => _additionalRotation = value; }
#endif
	}
	
	/// <inheritdoc />
	public abstract class VfxSpawnerBase<T> : VfxSpawnerBase where T : struct, Enum
	{
		[SerializeField] protected T _vfx;
	}
}