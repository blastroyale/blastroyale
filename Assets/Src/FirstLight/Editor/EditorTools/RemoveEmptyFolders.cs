using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace FirstLight.Editor.EditorTools
{
	/// <summary>
	/// Remove empty folders automatically.
	/// </summary>
	public class RemoveEmptyFolders : UnityEditor.AssetModificationProcessor
	{
		private const string _kMenuText = "Tools/Remove Empty Folders";
		
		private static readonly StringBuilder _log = new StringBuilder();
		private static readonly List<DirectoryInfo> _results = new List<DirectoryInfo>();
		private static readonly List<string> _blacklistFileOrDirectory = new List<string> { "Vendor"};

		/// <summary>
		/// Raises the initialize on load method event.
		/// </summary>
		[InitializeOnLoadMethod]
		private static void OnInitializeOnLoadMethod()
		{
			EditorApplication.delayCall += () => Valid();
		}

		/// <summary>
		/// Toggles the menu.
		/// </summary>
		[MenuItem(_kMenuText)]
		private static void OnClickMenu()
		{
			// Check/Uncheck menu.
			bool isChecked = !Menu.GetChecked(_kMenuText);
			Menu.SetChecked(_kMenuText, isChecked);

			// Save to EditorPrefs.
			EditorPrefs.SetBool(_kMenuText, isChecked);

			OnWillSaveAssets(null);
		}

		[MenuItem(_kMenuText, true)]
		private static bool Valid()
		{
			// Check/Uncheck menu from EditorPrefs.
			Menu.SetChecked(_kMenuText, EditorPrefs.GetBool(_kMenuText, false));
			return true;
		}

		/// <summary>
		/// Raises the will save assets event.
		/// </summary>
		private static string[] OnWillSaveAssets(string[] paths)
		{
			// If menu is unchecked, do nothing.
			if (!EditorPrefs.GetBool(_kMenuText, false))
				return paths;
	
			// Get empty directories in Assets directory
			_results.Clear();
			var assetsDir = Application.dataPath + Path.DirectorySeparatorChar;
			GetEmptyDirectories(new DirectoryInfo(assetsDir), _results);

			// When empty directories has detected, remove the directory.
			if (0 < _results.Count)
			{
				_log.Length = 0;
				_log.AppendFormat("Remove {0} empty directories as following:\n", _results.Count);
				foreach (var d in _results)
				{
					_log.AppendFormat("- {0}\n", d.FullName.Replace(assetsDir, ""));
					FileUtil.DeleteFileOrDirectory(d.FullName);
				}

				// UNITY BUG: Debug.Log can not set about more than 15000 characters.
				_log.Length = Mathf.Min(_log.Length, 15000);
				Debug.Log(_log.ToString());
				_log.Length = 0;

				AssetDatabase.Refresh();
			}
			return paths;
		}

		/// <summary>
		/// Get empty directories.
		/// </summary>
		private static bool GetEmptyDirectories(DirectoryInfo dir, List<DirectoryInfo> results)
		{
			bool isEmpty = true;
			try
			{
				isEmpty = dir.GetDirectories().Count(x => !GetEmptyDirectories(x, results)) == 0 && // Are sub directories empty?
					dir.GetFiles("*.*").All(x => x.Extension == ".meta") && // No file exist?
					!IsBlackListed(dir);
			}
			catch
			{
				// ignored
			}

			// Store empty directory to results.
			if (isEmpty)
				results.Add(dir);
			return isEmpty;
		}

		private static bool IsBlackListed(DirectoryInfo dir)
		{
			foreach (var fileOrDirectory in _blacklistFileOrDirectory)
			{
				if (dir.FullName.Contains(fileOrDirectory))
				{
					return true;
				}
			}

			return false;
		}
	}
}