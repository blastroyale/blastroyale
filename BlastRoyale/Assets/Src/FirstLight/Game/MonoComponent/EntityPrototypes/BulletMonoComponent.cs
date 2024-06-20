using FirstLight.Game.Utils;
using Quantum;
using Sirenix.OdinInspector;
using UnityEngine;

public class BulletMonoComponent : MonoBehaviour
{
	[SerializeField] public Gradient GoldenGunGradient;
	[SerializeField] public Gradient NormalGunGradient;
	[SerializeField] private LineRenderer _renderer;


	public static Vector3 CameraCorrectionOffset = new Vector3(0, -0.6f, 0);


#if UNITY_EDITOR
	private void OnValidate()
	{
		if (_renderer == null)
			_renderer = transform.GetComponentInChildren<LineRenderer>();
	}
#endif

	private EntityView View => GetComponent<EntityView>() ?? GetComponentInParent<EntityView>();

	void Awake()
	{
		View.OnEntityInstantiated.AddListener(SetupBulletColor);
	}


	public void SetupBulletColor(QuantumGame game)
	{
		if (_renderer == null) return;
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
				_renderer.colorGradient = GoldenGunGradient;
				return;
			}
		}

		_renderer.colorGradient = NormalGunGradient;
	}
}