using System;
using UnityEngine;

namespace FirstLight.Game.Views
{
	/// <summary>
	/// This Mono Behaviour is responsible for circle line renderer data manipulation
	/// </summary>
	public class CircleLineRenderer : MonoBehaviour
	{
		[SerializeField] private LineRenderer _lineRenderer;
	
		private static int _circleVertexResolution = 100;
		
		private readonly Vector3[] _circlePositions = new Vector3[_circleVertexResolution];

		private void Awake()
		{
			_lineRenderer.positionCount = _circleVertexResolution;
		}

		/// <summary>
		/// Draw circle at a given world space position with radius
		/// </summary>
		public void Draw(Vector3 position, float radius)
		{
			var angle = 2 * Mathf.PI / _circleVertexResolution;

			for (var i = 0; i < _circleVertexResolution; i++)
			{
				var cos = Mathf.Cos(angle * i);
				var sin = Mathf.Sin(angle * i);
				var rotationMatrix = new Matrix4x4(new Vector4(cos, sin, 0, 0),
				                                   new Vector4(-sin, cos, 0, 0),
				                                   new Vector4(0, 0, 1, 0),
				                                   new Vector4(0, 0, 0, 1));
				
				_circlePositions[i] = rotationMatrix.MultiplyPoint(new Vector3(0, radius, 0)) + new Vector3(position.x, 0, position.y);
			}
			
			_lineRenderer.SetPositions(_circlePositions);
		}
	}
}