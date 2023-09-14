using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;

namespace FirstLight.Editor.Build
{
	/// <summary>
	/// Post build step to modify permissions to add tracking data permission on iOS app
	/// </summary>
	public class IosPermissions {
	
		// Set the IDFA request description:
		private const string _description = "Your data will be used to provide you a better and personalized ad experience.";
 
		[PostProcessBuild(0)]
		public static void OnPostProcessBuild(BuildTarget buildTarget, string pathToXcode) {
			if (buildTarget == BuildTarget.iOS) {
				AddPListValues(pathToXcode);
			}
		}
 
		// Implement a function to read and write values to the plist file:
		static void AddPListValues(string pathToXcode) {

		// Retrieve the plist file from the Xcode project directory:
		string plistPath = pathToXcode + "/Info.plist";
		var plistObj = new UnityEditor.iOS.Xcode.PlistDocument();
		
		// Read the values from the plist file:
		plistObj.ReadFromString(File.ReadAllText(plistPath));
 
		// Set values from the root object:
		var plistRoot = plistObj.root;
 
		// Set the description key-value in the plist:
		plistRoot.SetString("NSUserTrackingUsageDescription", _description);
 
		// Save changes to the plist:
		File.WriteAllText(plistPath, plistObj.WriteToString());

		}
	}
}