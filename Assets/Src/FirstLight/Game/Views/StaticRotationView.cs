using UnityEngine;

namespace FirstLight.Game.Views
{
	/// <summary>
	/// This keeps the view always with the same rotation independently of external calls
	/// </summary>
	public class StaticRotationView : MonoBehaviour
	{
		[SerializeField] private Vector3 _staticRotation;

		private void LateUpdate()
		{
			transform.rotation = Quaternion.Euler(_staticRotation);
		}
	}
}