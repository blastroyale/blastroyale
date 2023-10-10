using System;
using System.IO;
using FirstLight.Game.Utils;
using JetBrains.Annotations;
using Photon.Deterministic;
using Quantum;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using LayerMask = UnityEngine.LayerMask;
using Object = UnityEngine.Object;


namespace FirstLight.Editor.EditorTools.ArtTools
{
	public class MapLayerFixer 
	{
		[MenuItem("FLG/Art/Fix Map Layers & Colliders")]
		private static void OpenWindow()
		{
			foreach (var o in Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None))
			{
				if (o.GetComponentInChildren<QuantumStaticBoxCollider3D>() != null)
				{
					if (o.layer != LayerMask.NameToLayer(PhysicsLayers.OBSTACLES) && o.layer != LayerMask.NameToLayer(PhysicsLayers.PLAYER_TRIGGERS))
					{
						o.SetLayer(LayerMask.NameToLayer(PhysicsLayers.OBSTACLES));
						Debug.Log($"{o.name} layer set to {PhysicsLayers.OBSTACLES}");
					} else if (o.layer == LayerMask.NameToLayer(PhysicsLayers.PLAYER_TRIGGERS))
					{
						// not a bush but its in Player Triggers
						if (!o.GetComponentInChildren<EntityComponentVisibilityArea>())
						{
							o.SetLayer(LayerMask.NameToLayer(PhysicsLayers.PLAYER_TRIGGERS));
							Debug.Log($"{o.name} layer set to {PhysicsLayers.PLAYER_TRIGGERS}");
						}
					}
				}

				if (o.GetComponentInChildren<EntityComponentVisibilityArea>() != null)
				{
					o.SetLayer(LayerMask.NameToLayer(PhysicsLayers.PLAYER_TRIGGERS));
					Debug.Log($"{o.name} layer set to {PhysicsLayers.PLAYER_TRIGGERS}");

					var p = o.GetComponentInChildren<EntityPrototype>();
					if (p != null)
					{
						p.PhysicsCollider.Layer = LayerMask.NameToLayer(PhysicsLayers.PLAYER_TRIGGERS);
						for (var x = 0 ; x< p.PhysicsCollider.Shape3D.CompoundShapes.Length; x++)
						{
							var s = p.PhysicsCollider.Shape3D.CompoundShapes[x];
							s.BoxExtents = new FPVector3(s.BoxExtents.X, 3, s.BoxExtents.Z);
							p.PhysicsCollider.Shape3D.CompoundShapes[x] = s;
						}

						var shape = p.PhysicsCollider.Shape3D;
						shape.BoxExtents = new FPVector3(shape.BoxExtents.X, 3, shape.BoxExtents.Z);
						p.PhysicsCollider.Shape3D = shape;

					}
				}
			}
		}
	}
}