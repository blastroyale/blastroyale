using Quantum;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace FirstLight.Editor.EditorTools
{
	/// <summary>
	/// A debug window that displays the current stat total for each stat during play mode
	///
	/// NOTE: Only works in Play mode.
	/// </summary>
	public class StatDebuggerWindow : OdinMenuEditorWindow
	{
		private LocalPlayerStats _LocalPlayerStats;
		private PlayerRef _LocalPlayer;

		[MenuItem("FLG/Stat Debugger")]
		private static void OpenWindow()
		{
			GetWindow<StatDebuggerWindow>("Stat Debugger").Show();
		}
		protected override OdinMenuTree BuildMenuTree()
		{
			var tree = new OdinMenuTree();

			if (!Application.isPlaying)
			{
				tree.Add("Play Mode Only", "Only available in Play mode.");
				return tree;
			}

			QuantumEvent.Subscribe<EventOnLocalPlayerSpawned>(this, GetLocalPlayer);
			QuantumEvent.Subscribe<EventOnPlayerEquipmentStatsChanged>(this, UpdateStats);
			_LocalPlayerStats = new LocalPlayerStats();

			tree.Add("Stat Debugger", _LocalPlayerStats);

			return tree;
		}

		public void GetLocalPlayer(EventOnLocalPlayerSpawned callback)
		{
			_LocalPlayer = callback.Player;
		}

		public void UpdateStats(EventOnPlayerEquipmentStatsChanged callback)
		{
			if (callback.Player != _LocalPlayer)
				return;

			_LocalPlayerStats.health = callback.CurrentStats.GetStatData(StatType.Health).StatValue.AsFloat;
			_LocalPlayerStats.shieldCapacity = callback.CurrentStats.GetStatData(StatType.Shield).StatValue.AsFloat;
			_LocalPlayerStats.armor = callback.CurrentStats.GetStatData(StatType.Armour).StatValue.AsFloat;

			_LocalPlayerStats.attackRange = callback.CurrentStats.GetStatData(StatType.AttackRange).StatValue.AsFloat;
			_LocalPlayerStats.attack = callback.CurrentStats.GetStatData(StatType.Power).StatValue.AsFloat;

			_LocalPlayerStats.speed = callback.CurrentStats.GetStatData(StatType.Speed).StatValue.AsFloat;
			_LocalPlayerStats.pickupSpeed = callback.CurrentStats.GetStatData(StatType.PickupSpeed).StatValue.AsFloat;
			_LocalPlayerStats.ammoCapacity = callback.CurrentStats.GetStatData(StatType.AmmoCapacity).StatValue.AsFloat;

			Repaint();
		}

		public class LocalPlayerStats
		{
			[ShowInInspector, Sirenix.OdinInspector.ReadOnly]
			public float health = 0;
			[ShowInInspector, Sirenix.OdinInspector.ReadOnly]
			public float armor = 0;
			[ShowInInspector, Sirenix.OdinInspector.ReadOnly]
			public float shieldCapacity = 0;

			[ShowInInspector, Sirenix.OdinInspector.ReadOnly]
			public float attack = 0;
			[ShowInInspector, Sirenix.OdinInspector.ReadOnly]
			public float attackRange = 0;

			[ShowInInspector, Sirenix.OdinInspector.ReadOnly]
			public float pickupSpeed = 0;
			[ShowInInspector, Sirenix.OdinInspector.ReadOnly]
			public float speed = 0;
			[ShowInInspector, Sirenix.OdinInspector.ReadOnly]
			public float ammoCapacity = 0;
		}
	}
}