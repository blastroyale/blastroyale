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
		
		public float Radius { get; set; } = 1f;
		
		public Vector3 CenterPosition { get; set; } = Vector3.zero;
		private readonly Vector3[] _circlePositions = new Vector3[_circleVertexResolution];

		private void Awake()
		{
			_lineRenderer.positionCount = _circleVertexResolution;
		}

		public void Draw()
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
				
				var position = rotationMatrix.MultiplyPoint(new Vector3(0, Radius, 0));
				_circlePositions[i] = CenterPosition + new Vector3(position.x, 0, position.y);
			}
			
			_lineRenderer.SetPositions(_circlePositions);
		}
	}
}