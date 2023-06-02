using FirstLight.Game.Utils;
using Quantum;
using Sirenix.OdinInspector;
using UnityEngine;

[RequireComponent(typeof(MeshRenderer))]
public class BulletMonoComponent : MonoBehaviour
{
	[SerializeField, Required] public Material MyBullet;
	[SerializeField, Required] public Material EnemyBullet;
	[SerializeField, Required] public GameObject Trail;
	
	private EntityView View => GetComponent<EntityView>() ?? GetComponentInParent<EntityView>();
	
	void Awake()
	{
		View.OnEntityInstantiated.AddListener(SetupBulletColor);
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
