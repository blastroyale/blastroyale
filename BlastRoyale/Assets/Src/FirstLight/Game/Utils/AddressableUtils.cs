using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace FirstLight.Game.Utils
{
	public static class AddressableUtils
	{
		public static T LoadAddressableEditorTime<T>(AssetReferenceT<T> reference) where T : Object
		{
#if UNITY_EDITOR

			var path = UnityEditor.AssetDatabase.GUIDToAssetPath(reference.AssetGUID);
			return UnityEditor.AssetDatabase.LoadAssetAtPath<T>(path);
#else
			return null;
#endif
		}

		public static AssetReferenceT<T> Clone<T>(this AssetReferenceT<T> assetRef) where T : Object
		{
			return new AssetReferenceT<T>(assetRef.AssetGUID);
		}

		public static AssetReferenceSprite Clone(this AssetReferenceSprite assetRef)
		{
			return new AssetReferenceSprite(assetRef.AssetGUID);
		}

		public static AssetReferenceTexture2D Clone(this AssetReferenceTexture2D assetRef)
		{
			return new AssetReferenceTexture2D(assetRef.AssetGUID);
		}
	}
}