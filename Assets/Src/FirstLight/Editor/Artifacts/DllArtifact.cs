using System.IO;
using System.Threading.Tasks;

namespace FirstLight.Editor.Artifacts
{
	public class DllArtifacts : IArtifact
	{
		public string SourceDir { get; set; }
		public string[] Dlls { get; set; }


		public Task CopyTo(string target)
		{
			this.AssureDirectoryExistence(target);
			foreach (var dll in Dlls)
			{
				var gameDllPath = $"{SourceDir}{dll}";
				var destDll = $"{target}/{dll}";
				

				File.Copy(gameDllPath, destDll, true);
			}

			return Task.CompletedTask;
		}
	}
}