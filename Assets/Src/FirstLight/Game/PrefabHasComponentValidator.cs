#if UNITY_EDITOR
using System;
using FirstLight.Game;
using Sirenix.OdinInspector.Editor.Validation;
using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets;

[assembly: RegisterValidator(typeof(PrefabHasComponentValidator))]

namespace FirstLight.Game
{
    public class PrefabHasComponentValidator : AttributeValidator<RequirePrefabComponentAttribute, AssetReferenceGameObject>
    {
        protected override void Validate(ValidationResult result)
        {

            var asset = this.ValueEntry.SmartValue;
            var components = this.Attribute.Components;


            if (asset == null || asset.editorAsset == null || !asset.RuntimeKeyIsValid())
            {
                return;
            }

            var path = AssetDatabase.GUIDToAssetPath(asset.RuntimeKey.ToString());
            var obj = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (obj == null)
            {
                result.AddError("Unable to instantiate game object in editor!");
                return;
            }

            foreach (var component in components)
            {
                if (obj.GetComponent(component) == null)
                {
                    result.AddError("Component " + component.Name + " required in prefab!");
                }
            }

        }
    }

}
#endif
    
namespace FirstLight.Game
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]

    public sealed class RequirePrefabComponentAttribute : Attribute
    {
        public Type[] Components;

        public RequirePrefabComponentAttribute(params Type[] components) => Components = components;
    }
}