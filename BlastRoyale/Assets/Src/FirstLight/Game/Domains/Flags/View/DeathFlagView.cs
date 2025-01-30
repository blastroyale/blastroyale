using UnityEngine;

namespace FirstLight.Game.Domains.Flags.View
{
	public class DeathFlagView : MonoBehaviour
	{
		private static readonly int _show = Animator.StringToHash("Show");
		[SerializeField] private SkinnedMeshRenderer _renderer;
		[SerializeField] private MeshRenderer _shadow;
		[SerializeField] private Animator _animator;
		[field: SerializeField] public Transform RotatedChild { get; private set; }

		public void Initialise(Mesh mesh)
		{
			_renderer.sharedMesh = mesh;
			_renderer.enabled = true;
			_shadow.enabled = true;
			_animator.enabled = true;
		}

		public void Reset()
		{
			_animator.SetBool(_show, false);
			_animator.Rebind();
			_renderer.sharedMesh = null;
			_renderer.enabled = false;
			_shadow.enabled = false;
			_animator.enabled = false;
		}

		public void TriggerFlag()
		{
			_animator.SetBool(_show, true);
		}
	}
}