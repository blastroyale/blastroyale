using UnityEngine;
using UnityEngine.Rendering;

namespace FirstLight.Game.Configs
{
	[CreateAssetMenu(fileName = "GraphicsConfig", menuName = "ScriptableObjects/Configs/GraphicsConfig")]
	public class GraphicsConfig : ScriptableObject
	{
		public RenderPipelineAsset LowQualityURPSettingsAsset;
		public RenderPipelineAsset MediumQualityURPSettingsAsset;
		public RenderPipelineAsset HighQualityURPSettingsAsset;
		
	}
}