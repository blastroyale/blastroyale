using UnityEngine.UI;

// ReSharper disable CheckNamespace

namespace FirstLight.UiService
{
	/// <summary>
	/// A concrete subclass of the Unity UI `Graphic` class that just skips drawing.
	/// Useful for providing a raycast target without actually drawing anything.
	/// </summary>
	public class NonDrawingView : Graphic
	{
		public override void SetMaterialDirty() { }
		public override void SetVerticesDirty() { }
		
		/// <summary>
		/// Probably not necessary since the chain of calls
		/// `Rebuild()`->`UpdateGeometry()`->`DoMeshGeneration()`->`OnPopulateMesh()` won't happen.
		/// So here really just as a fail-safe.
		/// </summary>
		protected override void OnPopulateMesh(VertexHelper vh) 
		{
			vh.Clear();
		}
	}
}