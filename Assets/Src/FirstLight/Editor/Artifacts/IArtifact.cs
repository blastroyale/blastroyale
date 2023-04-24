using System.IO;
using System.Threading.Tasks;

namespace FirstLight.Editor.Artifacts
{
	public interface IArtifact
	{
		Task CopyTo(string directory);
	}

	public static class IArtifactExtensions
	{
		public static void AssureDirectoryExistence(this IArtifact artifact, string directory)
		{
			if (!Directory.Exists(directory))
			{
				Directory.CreateDirectory(directory);
			}
		}
	}
}