using Quantum;
using Quantum.Commands;
using UnityEngine;

namespace FirstLight.Game.MonoComponent.Ftue
{
	/// <summary>
	/// This FTUE mono component spawns a <see cref="DummyCharacter"/> on the it's current position
	/// </summary>
	public class FtueDummySpawnerMonoComponent : MonoBehaviour
	{
		[SerializeField] private int _health = 600;
		
		private void Start()
		{
			QuantumRunner.Default.Game.SendCommand(new DummySpawnCommand
			{
				Position = transform.position.ToFPVector3(),
				Rotation = transform.rotation.ToFPQuaternion(),
				Health = _health
			});
		}
	}
}
