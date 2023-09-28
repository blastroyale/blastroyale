using FirstLight.Game.Utils;
using Quantum;
using Sirenix.OdinInspector;
using UnityEngine;

[RequireComponent(typeof(MeshRenderer))]
public class BulletMonoComponent : MonoBehaviour
{
	[SerializeField, Required] public Material MyBullet;
	[SerializeField, Required] public Material EnemyBullet;
	[SerializeField] public GameObject Trail;

	public static Vector3 CameraCorrectionOffset = new Vector3(0, -0.6f, 0);
	
	private EntityView View => GetComponent<EntityView>() ?? GetComponentInParent<EntityView>();
	
	void Awake()
	{
		if (FeatureFlags.BULLET_COLORS)
		{
			View.OnEntityInstantiated.AddListener(SetupBulletColor);
		}

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
		var localPlayerData = game.GetLocalPlayerData(false, out var f);
		var view = GetComponent<EntityView>() ?? GetComponentInParent<EntityView>();
	
		if (!f.TryGet<Projectile>(view.EntityRef, out var projectile))
		{
			return;
		}
		var shooter = projectile.Attacker;
		var rend = GetComponent<MeshRenderer>();
		
		if (!f.TryGet<PlayerCharacter>(shooter, out var playerShooter))
		{
			rend.material = EnemyBullet;
		} else if (!f.TryGet<PlayerCharacter>(localPlayerData.Entity, out var localPlayer))
		{
			rend.material = EnemyBullet;
		} else if (playerShooter.Player == localPlayer.Player)
		{
			rend.material = MyBullet;
		}
		else if (playerShooter.TeamId == localPlayer.TeamId)
		{
			rend.material = MyBullet;
		}
		else
		{
			rend.material = EnemyBullet;
		}
	}
	
}