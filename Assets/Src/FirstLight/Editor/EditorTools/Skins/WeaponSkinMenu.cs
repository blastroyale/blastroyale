using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FirstLight.Game.Configs.Collection;
using FirstLight.Game.Ids;
using FirstLight.Game.MonoComponent.Collections;
using Quantum;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;

namespace FirstLight.Editor.EditorTools.Skins
{
	public class WeaponSkinMenu
	{
        

		
		public static string hammerSkinFolder = "Assets/AddressableResources/Collections/WeaponSkins/Hammer";
		
		[MenuItem("Assets/Skins/Import FBX As Melee Skin")]
		public static void ConvertFBXs()
		{
			foreach (var fbx in Selection.objects.Cast<GameObject>())
			{
				string myPath = AssetDatabase.GetAssetPath( fbx );

				var fileName = myPath.Split("/").Last();
				var folderName = fileName.Replace(".fbx","");

				var bestId = GameIdGroup.MeleeSkin.GetIds()
					.Select(id => (CalculateDistance(id.ToString().ToLower(), folderName.Replace("MW_", "").ToLower()), id))
					.OrderBy(a => a.Item1).First().id;
				
				
				var directoryGuid = AssetDatabase.CreateFolder(hammerSkinFolder, folderName);
				var directory = AssetDatabase.GUIDToAssetPath(directoryGuid);
				var prefab = new GameObject();
				prefab.AddComponent<WeaponSkinMonoComponent>();
				fbx.transform.localScale = new Vector3(1, 1, 1);
				var obj = PrefabUtility.InstantiatePrefab(fbx, prefab.transform);
				((GameObject)obj).transform.rotation = Quaternion.Euler(new Vector3(0,-90,0));
				
				var prefabLocation = directory + "/Prefab.prefab";
				PrefabUtility.SaveAsPrefabAsset(prefab,prefabLocation);
				AssetDatabase.MoveAsset(myPath,directory+"/"+fileName);
				var newPrefabGuid = AssetDatabase.AssetPathToGUID(prefabLocation);
				var id = AddressableConfigLookup.GetConfig(AddressableId.Collections_WeaponSkins_Config);
				var op = Addressables.LoadAssetAsync<WeaponSkinsConfigContainer>(id.Address);
				op.Completed += handle =>
				{
					var config = handle.Result;
					var hammerGroup = config.Config.Groups.Find(a => a.Key == GameIdGroup.MeleeSkin);
                    
					hammerGroup.Value.Configs.Add(new WeaponSkinConfigEntry()
					{
						SkinId = bestId,
						Prefab =  new AssetReferenceGameObject(newPrefabGuid)
						
					});
					EditorUtility.SetDirty(config);
					AssetDatabase.SaveAssets();
					AssetDatabase.Refresh();
				};
			}
		}
		public static int CalculateDistance(string s, string t)
		{
			if (string.IsNullOrEmpty(s))
			{
				if (string.IsNullOrEmpty(t))
					return 0;
				return t.Length;
			}

			if (string.IsNullOrEmpty(t))
			{
				return s.Length;
			}

			int n = s.Length;
			int m = t.Length;
			int[,] d = new int[n + 1, m + 1];

			// initialize the top and right of the table to 0, 1, 2, ...
			for (int i = 0; i <= n; d[i, 0] = i++);
			for (int j = 1; j <= m; d[0, j] = j++);

			for (int i = 1; i <= n; i++)
			{
				for (int j = 1; j <= m; j++)
				{
					int cost = (t[j - 1] == s[i - 1]) ? 0 : 1;
					int min1 = d[i - 1, j] + 1;
					int min2 = d[i, j - 1] + 1;
					int min3 = d[i - 1, j - 1] + cost;
					d[i, j] = Math.Min(Math.Min(min1, min2), min3);
				}
			}
			return d[n, m];
		}

        
        
	}
	}
