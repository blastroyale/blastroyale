using FirstLight.Game.Utils;
using Quantum;
using Sirenix.OdinInspector;
using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class BulletMonoComponent : MonoBehaviour
{
	[SerializeField] public Gradient GoldenGunGradient;
	[SerializeField] public Gradient NormalGunGradient;


	public static Vector3 CameraCorrectionOffset = new Vector3(0, -0.6f, 0);
	
	private EntityView View => GetComponent<EntityView>() ?? GetComponentInParent<EntityView>();
	
	void Awake()
	{
		View.OnEntityInstantiated.AddListener(SetupBulletColor);

		if (FeatureFlags.BULLET_CAMERA_ADJUSTMENT)
		{
			foreach (var render in GetComponentsInChildren<Renderer>())
			{
				render.transform.Translate(CameraCorrectionOffset);
			}
		}
	}

	
	public void SetupBulletColor(QuantumGame game)
	{
		var lineRenderer = GetComponent<LineRenderer>();
		if (lineRenderer == null) return;
		var f = game.Frames.Predicted;
		var view = GetComponent<EntityView>() ?? GetComponentInParent<EntityView>();
		if (!f.TryGet<Projectile>(view.EntityRef, out var projectile))
		{
			return;
		}
		var shooter = projectile.Attacker;
		if (f.TryGet<PlayerCharacter>(shooter, out var playerShooter))
		{
				if (playerShooter.CurrentWeapon.Material == EquipmentMaterial.Golden)
				{
					lineRenderer.colorGradient = GoldenGunGradient;
					return;
				}
		}

		lineRenderer.colorGradient = NormalGunGradient;
	}
	
}