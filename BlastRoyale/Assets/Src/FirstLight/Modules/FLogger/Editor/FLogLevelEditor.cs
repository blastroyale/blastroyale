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
	internal class FLogLevelEditor
	{
		private const string SymbolError = "LOG_LEVEL_ERROR";
		private const string SymbolWarn = "LOG_LEVEL_WARN";
		private const string SymbolInfo = "LOG_LEVEL_INFO";
		private const string SymbolVerbose = "LOG_LEVEL_VERBOSE";

		private const string MenuNameNone = "FLG/Logging/None";
		private const string MenuNameError = "FLG/Logging/Error";
		private const string MenuNameWarn = "FLG/Logging/Warning";
		private const string MenuNameInfo = "FLG/Logging/Info";
		private const string MenuNameVerbose = "FLG/Logging/Verbose";

		private static readonly List<string> _currentDefineSymbols;

		static FLogLevelEditor()
		{
			var namedBuildTarget =
				NamedBuildTarget.FromBuildTargetGroup(EditorUserBuildSettings.selectedBuildTargetGroup);
			var symbols = PlayerSettings.GetScriptingDefineSymbols(namedBuildTarget);

			_currentDefineSymbols = symbols.Split(';').ToList();
		}

		[MenuItem(MenuNameNone)]
		private static void SetLevelNone()
		{
			SetSelectedLevel(null);
		}

		[MenuItem(MenuNameError)]
		private static void SetLevelError()
		{
			SetSelectedLevel(FLogLevel.Error);
		}

		[MenuItem(MenuNameWarn)]
		private static void SetLevelWarn()
		{
			SetSelectedLevel(FLogLevel.Warn);
		}

		[MenuItem(MenuNameInfo)]
		private static void SetLevelInfo()
		{
			SetSelectedLevel(FLogLevel.Info);
		}

		[MenuItem(MenuNameVerbose)]
		private static void SetLevelVerbose()
		{
			SetSelectedLevel(FLogLevel.Verbose);
		}

		[MenuItem(MenuNameNone, true)]
		private static bool SetLevelNoneValidate()
		{
			return ValidateSetLevel(MenuNameNone, null);
		}

		[MenuItem(MenuNameError, true)]
		private static bool SetLevelErrorValidate()
		{
			return ValidateSetLevel(MenuNameError, FLogLevel.Error);
		}

		[MenuItem(MenuNameWarn, true)]
		private static bool SetLevelWarnValidate()
		{
			return ValidateSetLevel(MenuNameWarn, FLogLevel.Warn);
		}

		[MenuItem(MenuNameInfo, true)]
		private static bool SetLevelInfoValidate()
		{
			return ValidateSetLevel(MenuNameInfo, FLogLevel.Info);
		}

		[MenuItem(MenuNameVerbose, true)]
		private static bool SetLevelVerboseValidate()
		{
			return ValidateSetLevel(MenuNameVerbose, FLogLevel.Verbose);
		}

		private static FLogLevel? GetSelectedLevel()
		{
			if (_currentDefineSymbols.Contains(SymbolError)) return FLogLevel.Error;
			if (_currentDefineSymbols.Contains(SymbolWarn)) return FLogLevel.Warn;
			if (_currentDefineSymbols.Contains(SymbolInfo)) return FLogLevel.Info;
			if (_currentDefineSymbols.Contains(SymbolVerbose)) return FLogLevel.Verbose;

			return null;
		}

		private static void SetSelectedLevel(FLogLevel? level)
		{
			_currentDefineSymbols.Remove(SymbolError);
			_currentDefineSymbols.Remove(SymbolWarn);
			_currentDefineSymbols.Remove(SymbolInfo);
			_currentDefineSymbols.Remove(SymbolVerbose);

			switch (level)
			{
				case FLogLevel.Error:
					_currentDefineSymbols.Add(SymbolError);
					break;
				case FLogLevel.Warn:
					_currentDefineSymbols.Add(SymbolWarn);
					break;
				case FLogLevel.Info:
					_currentDefineSymbols.Add(SymbolInfo);
					break;
				case FLogLevel.Verbose:
					_currentDefineSymbols.Add(SymbolVerbose);
					break;
				case null:
					// Do nothing - null means no logging, no symbols needed
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(level), level, null);
			}

			var namedBuildTarget =
				NamedBuildTarget.FromBuildTargetGroup(EditorUserBuildSettings.selectedBuildTargetGroup);
			PlayerSettings.SetScriptingDefineSymbols(namedBuildTarget, _currentDefineSymbols.ToArray());
		}

		private static bool ValidateSetLevel(string menuName, FLogLevel? level)
		{
			var selectedLevel = GetSelectedLevel();
			Menu.SetChecked(menuName, selectedLevel == level);
			return selectedLevel != level;
		}
	}
}