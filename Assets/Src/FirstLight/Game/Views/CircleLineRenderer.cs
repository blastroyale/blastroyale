using System;
using UnityEngine;

namespace FirstLight.Game.Views
{
	/// <summary>
	/// This Mono Behaviour is responsible for circle line renderer data manipulation.
	/// </summary>
	public class CircleLineRenderer : MonoBehaviour
	{
		[SerializeField] private float _lineWidth = 1;
		[SerializeField] private LineRenderer _lineRenderer;
	
		/// <summary>
		/// Set an overall multiplier that is applied to the LineRenderer.widthCurve to get the final width of the line.
		/// </summary>
		public float WidthMultiplier
		{
			set => _lineRenderer.widthMultiplier = value * _lineWidth;
		}
		
		private void Awake()
		{
			var circleVertexResolution = 100;
				
			var circlePositions = new Vector3[circleVertexResolution];
			
			_lineRenderer.positionCount = circleVertexResolution;
			
			var angle = 2 * Mathf.PI / circleVertexResolution;

			for (var i = 0; i < circleVertexResolution; i++)
			{
				var cos = Mathf.Cos(angle * i);
				var sin = Mathf.Sin(angle * i);
				
				var rotationMatrix = new Matrix4x4(new Vector4(cos, sin, 0, 0),
				                                   new Vector4(-sin, cos, 0, 0),
				                                   new Vector4(0, 0, 1, 0),
				                                   new Vector4(0, 0, 0, 1));
				
				circlePositions[i] = rotationMatrix.MultiplyPoint(new Vector3(0, 1, 0));
			}
			
			_lineRenderer.SetPositions(circlePositions);
		}
	}
}