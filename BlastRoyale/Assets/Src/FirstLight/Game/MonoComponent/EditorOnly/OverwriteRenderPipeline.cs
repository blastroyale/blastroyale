using UnityEngine;
using UnityEngine.Rendering;

namespace FirstLight.Game.MonoComponent.EditorOnly
{
	public class ActiveRenderPipelineExample : MonoBehaviour
	{
		// In the Inspector, assign a Render Pipeline Asset to each of these fields
		public RenderPipelineAsset Overwrite;

		void Start()
		{
			GraphicsSettings.defaultRenderPipeline = Overwrite;
			QualitySettings.renderPipeline = Overwrite;

			DisplayCurrentRenderPipeline();
		}
	

		// Print the current render pipeline information to the console
		void DisplayCurrentRenderPipeline()
		{
			// GraphicsSettings.defaultRenderPipeline determines the default render pipeline
			// If it is null, the default is the Built-in Render Pipeline
			if (GraphicsSettings.defaultRenderPipeline != null)
			{
				Debug.Log("The default render pipeline is defined by " + GraphicsSettings.defaultRenderPipeline.name);
			}
			else
			{
				Debug.Log("The default render pipeline is the Built-in Render Pipeline");
			}

			// QualitySettings.renderPipeline determines the override render pipeline for the current quality level
			// If it is null, no override exists for the current quality level
			if (QualitySettings.renderPipeline != null)
			{
				Debug.Log("The override render pipeline for the current quality level is defined by " + QualitySettings.renderPipeline.name);
			}
			else
			{
				Debug.Log("No override render pipeline exists for the current quality level");
			}

			// If an override render pipeline is defined, Unity uses that
			// Otherwise, it falls back to the default value
			if (QualitySettings.renderPipeline != null)
			{
				Debug.Log("The active render pipeline is the override render pipeline");
			}
			else
			{
				Debug.Log("The active render pipeline is the default render pipeline");
			}

			// To get a reference to the Render Pipeline Asset that defines the active render pipeline,
			// without knowing if it is the default or an override, use GraphicsSettings.currentRenderPipeline
			if (GraphicsSettings.currentRenderPipeline != null)
			{
				Debug.Log("The active render pipeline is defined by " + GraphicsSettings.currentRenderPipeline.name);
			}
			else
			{
				Debug.Log("The active render pipeline is the Built-in Render Pipeline");
			}
		}
	}
}