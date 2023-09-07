using System.Linq;
using FirstLight.Game.Utils;
using Sirenix.OdinInspector.Editor.Validation;


[assembly: RegisterValidator(typeof(QuantumStatic3dColliderValidator))]

/// <summary>
/// Validate QuantumStaticBoxCollider3D gameObject owner scale is not negative
/// </summary>
public class TechPassValidator : SceneValidator
{
	protected override void Validate(ValidationResult result)
	{
		var l = this.GetAllGameObjectsInScene();
		
		for (int i = 0; i < l.Count(); i++)
		{
			var go = l.ElementAt(i);

			if (go.GetComponent<QuantumStaticBoxCollider3D>() != null)
			{
				var s = go.transform.localScale;
				if (s.x < 0 || s.y < 0 || s.z < 0)
				{
					result.AddError($"QCV | {go.FullGameObjectPath()} has negative scale");
				}
			}
		}
	}
}
