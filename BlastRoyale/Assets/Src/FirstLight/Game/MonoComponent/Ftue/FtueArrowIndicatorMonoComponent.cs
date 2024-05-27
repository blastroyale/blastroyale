using FirstLight.Game.Ids;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using Sirenix.OdinInspector;
using UnityEngine;

namespace FirstLight.Game.MonoComponent.Ftue
{
	/// <summary>
	/// This mono component controls the arrow to be shown during the FTUE
	/// </summary>
	public class FtueArrowIndicatorMonoComponent : MonoBehaviour
	{
		[SerializeField, Required] private LineRenderer _lineRenderer;
		
		private Transform _playerTransform;
		
		private void Start()
		{
			_playerTransform = MainInstaller.Resolve<IGameServices>().GuidService.GetElement(GuidId.PlayerCharacter).Elements[0].transform;
		}

		private void Update()
		{
			var diff = _lineRenderer.transform.position - _playerTransform.position;

			diff.y = 0;
			
			_lineRenderer.SetPosition(1, diff);
		}
	}
}