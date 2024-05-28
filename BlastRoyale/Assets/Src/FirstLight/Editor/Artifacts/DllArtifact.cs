using System.IO;

namespace FirstLight.Editor.Artifacts
{
	public class DllArtifacts : IArtifact
	{
		public string SourceDir { get; set; }
		public string[] Dlls { get; set; }


		public void CopyTo(string target)
		{
			foreach (var dll in Dlls)
			{
				var gameDllPath = $"{SourceDir}{dll}";
				var destDll = $"{target}/{dll}";


				File.Copy(gameDllPath, destDll, true);
			}
		}
	}
}