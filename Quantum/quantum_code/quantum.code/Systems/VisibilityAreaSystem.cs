namespace Quantum.Systems
{
	public struct VisibilityCheckResult
	{
		public InsideVisibilityArea ViewerArea;
		public InsideVisibilityArea TargetArea;
		public bool CanSee;

		public override string ToString()
		{
			return $"<VisCheck CanSee={CanSee} ViewArea={ViewerArea.Area} TargetArea={TargetArea.Area}>";
		}
	}
	
	/// <summary>
	/// Handles visibility areas (e.g bushes). Bushes are parts of the map that players can enter and leave so the client can detect
	/// when to display one player for another player.
	/// </summary>
	public class VisibilityAreaSystem : SystemSignalsOnly, 
										ISignalOnTriggerEnter3D, ISignalOnTriggerExit3D,
										ISignalOnComponentAdded<VisibilityArea>, ISignalOnComponentRemoved<VisibilityArea>
	{
		public void OnTriggerEnter3D(Frame f, TriggerInfo3D info)
		{
			OnEntityEnterVisibilityArea(ref f, info.Entity, info.Other);
		}

		public void OnTriggerExit3D(Frame f, ExitInfo3D info)
		{
			OnExitVisibilityArea(ref f, info.Entity, info.Other);
		}

		private void OnEntityEnterVisibilityArea(ref Frame f, in EntityRef areaEntity, in EntityRef entering)
		{
			
			if (f.TryGet<VisibilityArea>(areaEntity, out var area))
			{
				if (f.TryGet<InsideVisibilityArea>(entering, out var existingArea))
				{
					OnExitVisibilityArea(ref f, existingArea.Area, entering);
				}
				f.Add(entering, new InsideVisibilityArea() { Area = areaEntity });
				f.ResolveList(area.EntitiesIn).Add(entering);
				f.Events.OnEnterVisibilityArea(entering, areaEntity);
			}
		}

		private void OnExitVisibilityArea(ref Frame f, in EntityRef areaEntity, in EntityRef leaving)
		{
			if (f.TryGet<InsideVisibilityArea>(leaving, out var existingArea) && existingArea.Area != areaEntity)
			{
				return;
			}
			if (f.TryGet<VisibilityArea>(areaEntity, out var area))
			{
				f.Remove<InsideVisibilityArea>(leaving);
				f.ResolveList(area.EntitiesIn).Remove(leaving);
				f.Events.OnLeaveVisibilityArea(leaving, areaEntity);
			}
		}

		public unsafe void OnAdded(Frame f, EntityRef entity, VisibilityArea* component)
		{
			component->EntitiesIn = f.AllocateList(component->EntitiesIn);
		}

		public unsafe void OnRemoved(Frame f, EntityRef entity, VisibilityArea* component)
		{
			f.FreeList(component->EntitiesIn);
		}

		public static bool CanEntityViewEntityRaw(in Frame f, in EntityRef viewer, in EntityRef target)
		{
			if (!f.TryGet<InsideVisibilityArea>(target, out var targetArea)) return true;
			if(TeamSystem.HasSameTeam(f, viewer, target)) return true;
			if (!f.TryGet<InsideVisibilityArea>(viewer, out var viewerArea)) return false;
			return targetArea.Area == viewerArea.Area;
		}
		
		public static VisibilityCheckResult CanEntityViewEntity(in Frame f, in EntityRef viewer, in EntityRef target)
		{
			var result = new VisibilityCheckResult();
			result.CanSee = true;
			if (!f.TryGet(target, out result.TargetArea)) return result;
			if(TeamSystem.HasSameTeam(f, viewer, target)) return result;
			if (!f.TryGet(viewer, out result.ViewerArea))
			{
				result.CanSee = false;
				return result;
			}
			result.CanSee = result.ViewerArea.Area == result.TargetArea.Area;
			return result;
		}
	}
}