using UnityEngine;
using UnityEngine.UIElements;

namespace FirstLight.Game.UIElements
{
	/// <summary>
	/// Draws a vertical or horizontal gradient between two colors.
	/// </summary>
	public class GradientElement : VisualElement
	{
		private static readonly ushort[] Indices = {0, 1, 2, 2, 3, 0};
		private static readonly Vertex[] Vertices = new Vertex[4];

		private Color startColor { get; set; }
		private Color endColor { get; set; }
		private bool vertical { get; set; }

		public GradientElement()
		{
			generateVisualContent += GenerateVisualContent;
		}

		private void GenerateVisualContent(MeshGenerationContext mgc)
		{
			var rect = contentRect;

			Vertices[0].tint = startColor;
			Vertices[1].tint = vertical ? endColor : startColor;
			Vertices[2].tint = endColor;
			Vertices[3].tint = vertical ? startColor : endColor;

			var left = 0f;
			var right = rect.width;
			var top = 0f;
			var bottom = rect.height;

			Vertices[0].position = new Vector3(left, bottom, Vertex.nearZ);
			Vertices[1].position = new Vector3(left, top, Vertex.nearZ);
			Vertices[2].position = new Vector3(right, top, Vertex.nearZ);
			Vertices[3].position = new Vector3(right, bottom, Vertex.nearZ);

			var mwd = mgc.Allocate(Vertices.Length, Indices.Length);
			mwd.SetAllVertices(Vertices);
			mwd.SetAllIndices(Indices);
		}

		public new class UxmlFactory : UxmlFactory<GradientElement, GradientUxmlTraits>
		{
		}

		public class GradientUxmlTraits : UxmlTraits
		{
			readonly UxmlColorAttributeDescription _startColorAttribute = new()
			{
				name = "start-color",
				use = UxmlAttributeDescription.Use.Required,
				defaultValue = Color.red
			};

			readonly UxmlColorAttributeDescription _endColorAttribute = new()
			{
				name = "end-color",
				use = UxmlAttributeDescription.Use.Required,
				defaultValue = Color.blue
			};

			private readonly UxmlBoolAttributeDescription _verticalAttribute = new()
			{
				name = "vertical",
				defaultValue = false
			};

			public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
			{
				base.Init(ve, bag, cc);

				var ge = (GradientElement) ve;
				ge.startColor = _startColorAttribute.GetValueFromBag(bag, cc);
				ge.endColor = _endColorAttribute.GetValueFromBag(bag, cc);
				ge.vertical = _verticalAttribute.GetValueFromBag(bag, cc);
			}
		}
	}
}