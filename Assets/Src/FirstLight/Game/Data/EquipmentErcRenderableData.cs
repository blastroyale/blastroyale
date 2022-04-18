using UnityEngine;

namespace FirstLight.Game.Data
{
	[CreateAssetMenu(fileName = "EquipmentErcRenderableData", menuName = "ScriptableObjects/EquipmentErcRenderableData", order = 1)]
	public class EquipmentErcRenderableData : ScriptableObject
	{
		public Sprite[] SurfaceTexture;
		public Sprite[] AdjectiveTexture;
	}
}