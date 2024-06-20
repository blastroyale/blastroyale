using System.IO;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;

namespace FirstLight.Editor.Artifacts
{
	public interface IArtifact
	{
		void CopyTo(string directory);
	}
}