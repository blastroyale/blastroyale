using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace FirstLight.Game.Data
{
	[Serializable]
	public class SerializedVisualElementSetup
	{
		public string ElementId;
		public float PositionX;
		public float PositionY;
		public float Opacity = 1;
		public float Size = 1;

		public SerializedVisualElementSetup FromElement(VisualElement e)
		{
			ElementId = e.name;
			PositionX = e.transform.position.x;
			PositionY = e.transform.position.y;
			Opacity = e.style.opacity.value == 0 ? 1 : e.style.opacity.value;
			Size = e.style.scale.value.value.x == 0 ? 1 : e.style.scale.value.value.x;
			return this;
		}

		public void ToElement(VisualElement e)
		{
			e.transform.position = new Vector3(PositionX, PositionY, 0);
			e.style.opacity = Opacity;
			e.style.scale = new Scale(new Vector2(Size, Size));
		}
	}
	
	[Serializable]
	public class UIPositionData
	{
		public List<SerializedVisualElementSetup> HudScreenSetup = new();
	}
}