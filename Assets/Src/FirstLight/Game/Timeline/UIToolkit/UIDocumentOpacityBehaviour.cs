using System;
using UnityEngine;

namespace FirstLight.Game.Timeline.UIToolkit
{
	/// <summary>
	/// The behaviour / data of the opacity operation.
	/// 
	/// <see cref="UIDocumentOpacityClip"/>
	/// </summary>
	[Serializable]
	public class UIDocumentOpacityBehaviour : UIDocumentBehaviour
	{
		[Range(-1, 0)] public float Opacity;
	}
}