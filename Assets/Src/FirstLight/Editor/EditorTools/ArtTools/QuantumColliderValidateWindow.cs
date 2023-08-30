using FirstLight.Editor.EditorTools.NFTGenerator;
using FirstLight.Game.Utils;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using Assert = UnityEngine.Assertions.Assert;
using Debug = UnityEngine.Debug;

namespace FirstLight.Editor.EditorTools.ArtTools
{
	/// <summary>
	/// 
	/// </summary>
	public class QuantumColliderValidateWindow : OdinEditorWindow
	{
		
		[MenuItem("FLG/Validators/QuantumColliderValidateWindow")]
		private static void OpenWindow()
		{
			GetWindow<QuantumColliderValidateWindow>("QuantumColliderValidateWindow").Show();
		}

		protected override void Initialize()
		{
			base.Initialize();
		}

		[Button, HideInPlayMode]
		private void ValidateNegativeScale()
		{
			var l = FindObjectsOfType<QuantumStaticBoxCollider3D>();

			foreach (var c in l)
			{
				var s = c.transform.localScale;
				if (s.x < 0 || s.y < 0 || s.z < 0)
				{
					Debug.LogError($"QCV | {c.gameObject.FullGameObjectPath()} has negative scale");
				}
			}
		}
		
		[UsedImplicitly]
		private bool IsEnvironmentReady()
		{
			return Application.isPlaying && SceneManager.GetActiveScene().name == "EquipmentCardRender";
		}
	}
}