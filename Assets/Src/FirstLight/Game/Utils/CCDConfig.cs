using UnityEngine;
using UnityEngine.Scripting;

namespace FirstLight.Game.Utils
{
	/// <summary>
	/// Stores CCD bucket id and badge name used by addressables (implicitly from the addressable profile).
	/// </summary>
	[Preserve]
	public class CCDConfig
	{
		/// <summary>
		/// The current bucket id for CCD.
		/// </summary>
		[Preserve] public static string CCDBucketID = FLEnvironment.Current.UCSBucketID;

		/// <summary>
		/// The current badge for CCD, defaults to version.
		/// </summary>
		[Preserve] public static string CCDBadgeName = Application.version.Replace(".", "_");

		/// <summary>
		/// The current environment for CCD.
		/// </summary>
		[Preserve] public static string CCDEnvironment = FLEnvironment.Current.UCSEnvironmentName;
	}
}