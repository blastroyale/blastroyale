using System.Threading.Tasks;
using FirstLight.Game.Utils;
using Quantum;
using UnityEngine;

namespace FirstLight.Game.MonoComponent.EntityViews
{
	/// <summary>
	/// Shows any visual feedback for the hazards created in the scene via quantum.
	/// This object is only responsible for the view.
	/// </summary>
	public class HazardViewMonoComponent : EntityMainViewBase
	{
		[SerializeField] private float _gracefulFinishDuration = 4f;
		[SerializeField] private bool _doNotScaleView = false;
		
		protected override void OnInit()
		{
			EntityView.OnEntityDestroyed.AddListener(HandleOnEntityDestroyed);
		}

		/// <summary>
		/// Sets the view <paramref name="radius"/> scale
		/// </summary>
		public void SetRadius(float radius)
		{
			if (_doNotScaleView)
			{
				return;
			}

			transform.localScale = Vector3.one * radius;
		}
		
		private void HandleOnEntityDestroyed(QuantumGame game)
		{
			transform.parent = null;

			this.LateCall(_gracefulFinishDuration, () => Destroy(gameObject));
		}
	}
}