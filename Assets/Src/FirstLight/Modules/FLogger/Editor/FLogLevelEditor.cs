using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;

namespace FirstLight.FLogger.Editor
{
	/// <summary>
	/// Adds menu items to switch between different logging levels.
	/// </summary>
	[InitializeOnLoad]
	public class FLogLevelEditor
	{
		private const string SymbolError = "LOG_LEVEL_ERROR";
		private const string SymbolWarn = "LOG_LEVEL_WARN";
		private const string SymbolInfo = "LOG_LEVEL_INFO";
		private const string SymbolVerbose = "LOG_LEVEL_VERBOSE";

		private const string MenuNameNone = "First Light Games/Logging/None";
		private const string MenuNameError = "First Light Games/Logging/Error";
		private const string MenuNameWarn = "First Light Games/Logging/Warning";
		private const string MenuNameInfo = "First Light Games/Logging/Info";
		private const string MenuNameVerbose = "First Light Games/Logging/Verbose";

		private static List<string> CurrentDefineSymbols;

		static FLogLevelEditor()
		{
			var namedBuildTarget =
				NamedBuildTarget.FromBuildTargetGroup(EditorUserBuildSettings.selectedBuildTargetGroup);
			var symbols = PlayerSettings.GetScriptingDefineSymbols(namedBuildTarget);

			CurrentDefineSymbols = symbols.Split(';').ToList();
		}

		private static FLogLevel? SelectedLevel
		{
			get
			{
				if (CurrentDefineSymbols.Contains(SymbolError)) return FLogLevel.Error;
				if (CurrentDefineSymbols.Contains(SymbolWarn)) return FLogLevel.Warn;
				if (CurrentDefineSymbols.Contains(SymbolInfo)) return FLogLevel.Info;
				if (CurrentDefineSymbols.Contains(SymbolVerbose)) return FLogLevel.Verbose;

				return null;
			}

			set
			{
				CurrentDefineSymbols.Remove(SymbolError);
				CurrentDefineSymbols.Remove(SymbolWarn);
				CurrentDefineSymbols.Remove(SymbolInfo);
				CurrentDefineSymbols.Remove(SymbolVerbose);

				switch (value)
				{
					case FLogLevel.Error:
						CurrentDefineSymbols.Add(SymbolError);
						break;
					case FLogLevel.Warn:
						CurrentDefineSymbols.Add(SymbolWarn);
						break;
					case FLogLevel.Info:
						CurrentDefineSymbols.Add(SymbolInfo);
						break;
					case FLogLevel.Verbose:
						CurrentDefineSymbols.Add(SymbolVerbose);
						break;
					case null:
						// Do nothing - null means no logging, no symbols needed
						break;
					default:
						throw new ArgumentOutOfRangeException(nameof(value), value, null);
				}

				var namedBuildTarget =
					NamedBuildTarget.FromBuildTargetGroup(EditorUserBuildSettings.selectedBuildTargetGroup);
				PlayerSettings.SetScriptingDefineSymbols(namedBuildTarget, CurrentDefineSymbols.ToArray());
			}
		}

		[MenuItem(MenuNameNone)]
		private static void SetLevelNone() => SelectedLevel = null;

		[MenuItem(MenuNameError)]
		private static void SetLevelError() => SelectedLevel = FLogLevel.Error;

		[MenuItem(MenuNameWarn)]
		private static void SetLevelWarn() => SelectedLevel = FLogLevel.Warn;

		[MenuItem(MenuNameInfo)]
		private static void SetLevelInfo() => SelectedLevel = FLogLevel.Info;

		[MenuItem(MenuNameVerbose)]
		private static void SetLevelVerbose() => SelectedLevel = FLogLevel.Verbose;

		[MenuItem(MenuNameNone, true)]
		private static bool SetLevelNoneValidate()
		{
			Menu.SetChecked(MenuNameNone, SelectedLevel == null);
			return SelectedLevel != null;
		}

		[MenuItem(MenuNameError, true)]
		private static bool SetLevelErrorValidate()
		{
			Menu.SetChecked(MenuNameError, SelectedLevel == FLogLevel.Error);
			return SelectedLevel != FLogLevel.Error;
		}

		[MenuItem(MenuNameWarn, true)]
		private static bool SetLevelWarnValidate()
		{
			Menu.SetChecked(MenuNameWarn, SelectedLevel == FLogLevel.Warn);
			return SelectedLevel != FLogLevel.Warn;
		}

		[MenuItem(MenuNameInfo, true)]
		private static bool SetLevelInfoValidate()
		{
			Menu.SetChecked(MenuNameInfo, SelectedLevel == FLogLevel.Info);
			return SelectedLevel != FLogLevel.Info;
		}

		[MenuItem(MenuNameVerbose, true)]
		private static bool SetLevelVerboseValidate()
		{
			Menu.SetChecked(MenuNameVerbose, SelectedLevel == FLogLevel.Verbose);
			return SelectedLevel != FLogLevel.Verbose;
		}
	}
}