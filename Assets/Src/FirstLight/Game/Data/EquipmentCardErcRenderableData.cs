using System;
using System.Collections.Generic;
using UnityEngine;

namespace FirstLight.Game.Data
{
	[System.Serializable]
	public class SpriteDimensional
	{
		public Sprite[] Array;
	}
		
	[CreateAssetMenu(fileName = "EquipmentCardErcRenderableData", menuName = "ScriptableObjects/EquipmentCardErcRenderableData", order = 1)]
	public class EquipmentCardErcRenderableData : ScriptableObject
	{
		public Sprite[] FactionSprites;
		public SpriteDimensional[] Back;
		public SpriteDimensional[] Frame;
		public Sprite[] FrameShapeMask;
		public Sprite[] NameTag;
		public Sprite[] AdjectivePattern;
		public Sprite[] PlusAmountGradePattern;
	}
}