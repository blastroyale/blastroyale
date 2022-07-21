using FirstLight.Game.Ids;
using FirstLight.Game.Logic;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using Quantum;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace FirstLight.Editor.EditorTools
{
	/// <summary>
	/// A debug window that displays the current equipment in your Inventory, and
	/// lets you add new items / remove existing ones.
	///
	/// NOTE: Only works in Play mode.
	/// </summary>
	public class EquipmentDebuggerWindow : OdinMenuEditorWindow
	{
		[MenuItem("FLG/Equipment Debugger")]
		private static void OpenWindow()
		{
			GetWindow<EquipmentDebuggerWindow>("Equipment Debugger").Show();
		}

		protected override OdinMenuTree BuildMenuTree()
		{
			var tree = new OdinMenuTree();

			if (!Application.isPlaying)
			{
				tree.Add("Play Mode Only", "Only available in Play mode.");
				return tree;
			}

			tree.Add("Create", new EquipmentMaker());

			var gameLogic = MainInstaller.Resolve<IGameDataProvider>() as IGameLogic;
			foreach (var (id, e) in gameLogic.EquipmentLogic.Inventory)
			{
				tree.Add($"{e.GetEquipmentGroup().ToString()}/{e.GameId.GetTranslation()} [{id}]",
				         new EquipmentWrapper(id, e));
			}

			return tree;
		}

		public class EquipmentWrapper
		{
			[ShowInInspector, Sirenix.OdinInspector.ReadOnly]
			private UniqueId _id;

			[ShowInInspector, Sirenix.OdinInspector.ReadOnly]
			private Equipment _data;

			public EquipmentWrapper(UniqueId id, Equipment data)
			{
				_id = id;
				_data = data;
			}

			[Button]
			public void Delete()
			{
				var services = MainInstaller.Resolve<IGameServices>();
				var gameLogic = MainInstaller.Resolve<IGameDataProvider>() as IGameLogic;

				gameLogic.EquipmentLogic.RemoveFromInventory(_id);
				((GameCommandService) services.CommandService).ForceServerDataUpdate();
			}
		}

		public class EquipmentMaker
		{
			[ShowInInspector] private Equipment _equipment;

			[Button, DisableInEditorMode]
			private void CreateAndAdd()
			{
				var services = MainInstaller.Resolve<IGameServices>();
				var gameLogic = MainInstaller.Resolve<IGameDataProvider>() as IGameLogic;

				gameLogic.EquipmentLogic.AddToInventory(_equipment);
				((GameCommandService) services.CommandService).ForceServerDataUpdate();
			}
		}
	}
}