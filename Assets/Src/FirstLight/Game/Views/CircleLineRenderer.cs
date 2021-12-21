using System;
using UnityEngine;

namespace FirstLight.Game.Views
{
	/// <summary>
	/// This Mono Behaviour is responsible for circle line renderer data manipulation.
	/// Width Multiplier property allows us to modify the line width as we scale the transform of the line
	/// renderer game object as we need to compensate for line width to maintain constant size.
	/// </summary>
	public class CircleLineRenderer : MonoBehaviour
	{
		[SerializeField] private LineRenderer _lineRenderer;
	
		private static readonly int _circleVertexResolution = 100;
		private readonly Vector3[] _circlePositions = new Vector3[_circleVertexResolution];

		public float WidthMultiplier
		{
			set => _lineRenderer.widthMultiplier = value;
		}

		private void Awake()
		{
			_lineRenderer.positionCount = _circleVertexResolution;
			
			var angle = 2 * Mathf.PI / _circleVertexResolution;

			for (var i = 0; i < _circleVertexResolution; i++)
			{
				var cos = Mathf.Cos(angle * i);
				var sin = Mathf.Sin(angle * i);
				
				var rotationMatrix = new Matrix4x4(new Vector4(cos, sin, 0, 0),
				                                   new Vector4(-sin, cos, 0, 0),
				                                   new Vector4(0, 0, 1, 0),
				                                   new Vector4(0, 0, 0, 1));
				
				_circlePositions[i] = rotationMatrix.MultiplyPoint(new Vector3(0, 1, 0));
			}
			
			_lineRenderer.SetPositions(_circlePositions);
		}
	}
}