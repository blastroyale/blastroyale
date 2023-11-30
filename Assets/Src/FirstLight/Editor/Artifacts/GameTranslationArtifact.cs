using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using FirstLight.Game.Configs;
using FirstLight.Game.Services;
using FirstLight.Server.SDK.Modules;
using FirstLight.Server.SDK.Modules.GameConfiguration;
using I2.Loc;
using UnityEngine;

namespace FirstLight.Editor.Artifacts
{
	public class GameTranslationArtifact : IArtifact
	{
		public string GenerateTranslationJson()
		{
			var language = "English";
			var terms = new Dictionary<string, string>();
			foreach (var s in LocalizationManager.GetTermsList())
			{
				terms[s] = LocalizationManager.GetTranslation(s, default, default, default, default, default, language);
			}

			return ModelSerializer.Serialize(terms).Value;
		}

		public UniTask CopyTo(string directory)
		{
			var path = $"{directory}/gameTranslations.json";
			File.WriteAllText(path, GenerateTranslationJson());
			Debug.Log($"Parsed and saved translations at {path}");
			return UniTask.CompletedTask;
		}
	}
}