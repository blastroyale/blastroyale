using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using FirstLight.Game.Configs;
using FirstLight.Game.MonoComponent;
using FirstLight.Game.Utils;
using I2.Loc;
using Newtonsoft.Json;
using Quantum;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.AddressableAssets;
using Equipment = Quantum.Equipment;


namespace FirstLight.Editor.EditorTools.MapGenerator
{
	/// <summary>
	/// This editor window provides functionality for generating a map 
	/// </summary>
	public class MapGeneratorEditorWindow : OdinEditorWindow
	{
		[BoxGroup("Folder Paths")] [FolderPath(AbsolutePath = true, RequireExistingPath = true)]
		public string _exportFolderPath;
		
		[MenuItem("FLG/Generators/Map Generator EditorWindow")]
		private static void OpenWindow()
		{
			GetWindow<MapGeneratorEditorWindow>("Map Generator EditorWindow").Show();
		}
	}
}



