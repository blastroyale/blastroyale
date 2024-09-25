using System.Linq;
using Quantum;
using UnityEditor;
using UnityEngine;

namespace FirstLight.Editor.EditorTools.ArtTools
{
    public class EntityProto3Dto2D : AutoPrefabUpdater
    {
       [MenuItem("FLG/Art/Prefab Automation/Physics 3D to 2D/Entity Prototypes")]
       public static void OpenWindow()
       {
          var wnd = GetWindow<EntityProto3Dto2D>();
          wnd.titleContent = new GUIContent("Physics Converter");
       }

       protected override void OnRenderUI()
       {
          GUILayout.Label("Update Entity Prototypes", EditorStyles.boldLabel);
          GUILayout.Label("Ports all prototypes in folder from 3d to 2d.");
       }
       
       protected override bool OnUpdateGameObject(GameObject o)
       {
          return TryMigrateBox(o) || TryMigrateSphere(o);
       }

       private bool TryMigrateBox(GameObject o)
       {
          var e = o.GetComponentsInChildren<EntityPrototype>();

          foreach (var c in e)
          {
             if (c.TransformMode == EntityPrototypeTransformMode.Transform3D)
             {
                c.TransformMode = EntityPrototypeTransformMode.Transform2D;
                if (c.PhysicsCollider.IsEnabled)
                {
                   if (c.PhysicsCollider.Shape3D.ShapeType == Shape3DType.Box)
                   {
                      c.PhysicsCollider.Shape2D.ShapeType = Shape2DType.Box;
                      c.PhysicsCollider.Shape2D.BoxExtents = c.PhysicsCollider.Shape3D.BoxExtents.XZ;
                      c.PhysicsCollider.Shape2D.PositionOffset = c.PhysicsCollider.Shape3D.PositionOffset.XZ;
                   } else if (c.PhysicsCollider.Shape3D.ShapeType == Shape3DType.Sphere)
                   {
                      c.PhysicsCollider.Shape2D.ShapeType = Shape2DType.Circle;
                      c.PhysicsCollider.Shape2D.CircleRadius = c.PhysicsCollider.Shape3D.SphereRadius;
                   }else if (c.PhysicsCollider.Shape3D.ShapeType == Shape3DType.Compound)
                   {
                      c.PhysicsCollider.Shape2D.ShapeType = Shape2DType.Compound;
                      
                      c.PhysicsCollider.Shape2D.CompoundShapes = c.PhysicsCollider.Shape3D.CompoundShapes.Select((s, i) => new Shape2DConfig.CompoundShapeData2D()
                      {
                         ShapeType = s.ShapeType == Shape3DType.Box ? Shape2DType.Box : Shape2DType.Circle,
                         BoxExtents = c.PhysicsCollider.Shape3D.CompoundShapes[i].BoxExtents.XZ,
                         PositionOffset = c.PhysicsCollider.Shape3D.CompoundShapes[i].PositionOffset.XZ,
                      }).ToArray();
                   }
                }
             }
             
          }

          return true;
       }
       
       private bool TryMigrateSphere(GameObject o)
       {
          var colliders = o.GetComponentsInChildren<QuantumStaticSphereCollider3D>();
          if (colliders == null || colliders.Length == 0) return false;

          foreach (var c in colliders)
          {
             var c2d = c.gameObject.AddComponent<QuantumStaticCircleCollider2D>();
             c2d.SourceCollider = c.SourceCollider;
             c2d.Radius = c.Radius;
             DestroyImmediate(c);
          }

          return true;
       }
    }
}






