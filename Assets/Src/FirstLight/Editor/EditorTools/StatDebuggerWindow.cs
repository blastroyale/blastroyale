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
	public class StatDebuggerWindow : OdinEditorWindow
	{
		private PlayerRef _LocalPlayer;
		private bool _Initialized = false;

		[Header("Defence Stats")]
		[ShowInInspector, ReadOnly]
		public float Health = 0;
		[ShowInInspector, ReadOnly]
		public float ShieldCapacity = 0;
		[ShowInInspector, ReadOnly]
		public float MaxShieldCapacity = 0;
		[ShowInInspector, ReadOnly]
		public float Armor = 0;

		[Header("Offence Stats")]
		[ShowInInspector, ReadOnly]
		public float Power = 0;
		[ShowInInspector, ReadOnly]
		public float AttackRange = 0;

		[Header("Utility Stats")]
		[ShowInInspector, ReadOnly]
		public float PickupSpeed = 0;
		[ShowInInspector, ReadOnly]
		public float Speed = 0;
		[ShowInInspector, ReadOnly]
		public float AmmoCapacity = 0;

		[MenuItem("FLG/Stat Debugger")]
		private static void OpenWindow()
		{
			GetWindow<StatDebuggerWindow>("Stat Debugger").Show();
		}

		protected override void OnGUI()
		{
			base.OnGUI();

			if (Application.IsPlaying(this) && !_Initialized)
			{
				QuantumEvent.Subscribe<EventOnLocalPlayerSpawned>(this, GetLocalPlayer);
				QuantumEvent.Subscribe<EventOnPlayerEquipmentStatsChanged>(this, UpdateStats);
				_Initialized = true;
			}
			if (!Application.IsPlaying(this) && _Initialized)
			{
				QuantumEvent.UnsubscribeListener(this);
				_Initialized = false;
			}
		}

		protected override void OnDestroy()
		{
			QuantumEvent.UnsubscribeListener(this);
			base.OnDestroy();
		}

		public void GetLocalPlayer(EventOnLocalPlayerSpawned callback)
		{
			_LocalPlayer = callback.Player;
		}

		public void UpdateStats(EventOnPlayerEquipmentStatsChanged callback)
		{
			if (callback.Player != _LocalPlayer)
				return;

			Health = callback.CurrentStats.GetStatData(StatType.Health).StatValue.AsFloat;
			ShieldCapacity = callback.CurrentStats.GetStatData(StatType.Shield).StatValue.AsFloat;
			MaxShieldCapacity = callback.CurrentStats.GetStatData(StatType.Shield).BaseValue.AsFloat;
			Armor = callback.CurrentStats.GetStatData(StatType.Armour).StatValue.AsFloat;

			AttackRange = callback.CurrentStats.GetStatData(StatType.AttackRange).StatValue.AsFloat;
			Power = callback.CurrentStats.GetStatData(StatType.Power).StatValue.AsFloat;

			Speed = callback.CurrentStats.GetStatData(StatType.Speed).StatValue.AsFloat;
			PickupSpeed = callback.CurrentStats.GetStatData(StatType.PickupSpeed).StatValue.AsFloat;
			AmmoCapacity = callback.CurrentStats.GetStatData(StatType.AmmoCapacity).StatValue.AsFloat;

			Repaint();
		}

	}
}