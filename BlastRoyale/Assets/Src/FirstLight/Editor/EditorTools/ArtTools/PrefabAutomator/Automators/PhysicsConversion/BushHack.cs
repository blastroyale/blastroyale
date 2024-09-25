using System.Collections.Generic;
using System.Linq;
using FirstLight.Game.MonoComponent.EntityPrototypes;
using Quantum;
using UnityEditor;
using UnityEngine;

namespace FirstLight.Editor.EditorTools.ArtTools
{
    public class BushHack : AutoPrefabUpdater
    {
       [MenuItem("FLG/Art/Prefab Automation/Physics 3D to 2D/Remaining Fixes")]
       public static void OpenWindow()
       {
          var wnd = GetWindow<BushHack>();
          wnd.titleContent = new GUIContent("Physics Converter");
       }

       protected override void OnRenderUI()
       {
          GUILayout.Label("Update All Bushes n stuffs", EditorStyles.boldLabel);
          GUILayout.Label("Fixes visibility area compounds.");
       }
       
       protected override bool OnUpdateGameObject(GameObject o)
       {
          var vis = o.GetComponent<VisibilityAreaMonoComponent>();
          if (vis == null) return false;
          return TryMigrateBox(o);
          
       }

       private bool TryMigrateBox(GameObject o)
       {
          var e = o.GetComponentsInChildren<EntityPrototype>();

          foreach (var c in e)
          {
             if (c.PhysicsCollider.Shape3D.ShapeType == Shape3DType.None && c.PhysicsCollider.Shape3D.BoxExtents.X > 0)
             {
                c.TransformMode = EntityPrototypeTransformMode.Transform2D;
                c.PhysicsCollider.Shape2D.ShapeType = Shape2DType.Box;
                c.PhysicsCollider.Shape2D.BoxExtents = c.PhysicsCollider.Shape3D.BoxExtents.XZ;
                c.PhysicsCollider.Shape2D.PositionOffset = c.PhysicsCollider.Shape3D.PositionOffset.XZ;
                c.PhysicsCollider.Shape3D.ShapeType = Shape3DType.None;
                EditorUtility.SetDirty(c);
                return true;
             }
             
             if (c.PhysicsCollider.Shape3D.ShapeType == Shape3DType.Box)
             {
                c.TransformMode = EntityPrototypeTransformMode.Transform2D;
                c.PhysicsCollider.Shape2D.ShapeType = Shape2DType.Box;
                c.PhysicsCollider.Shape2D.BoxExtents = c.PhysicsCollider.Shape3D.BoxExtents.XZ;
                c.PhysicsCollider.Shape2D.PositionOffset = c.PhysicsCollider.Shape3D.PositionOffset.XZ;
                c.PhysicsCollider.Shape2D.RotationOffset = c.PhysicsCollider.Shape3D.RotationOffset.Y;
                c.PhysicsCollider.Shape3D.ShapeType = Shape3DType.None;
                EditorUtility.SetDirty(c);
                return true;
             }

             if (c.PhysicsCollider.Shape3D.ShapeType == Shape3DType.Compound)
             {
                c.TransformMode = EntityPrototypeTransformMode.Transform2D;
                var shapes = new List<Shape2DConfig.CompoundShapeData2D>();
                for (var i = 0; i < c.PhysicsCollider.Shape3D.CompoundShapes.Length; i++)
                {
                   shapes.Add(new Shape2DConfig.CompoundShapeData2D()
                   {
                      ShapeType = Shape2DType.Box,
                      BoxExtents = c.PhysicsCollider.Shape3D.CompoundShapes[i].BoxExtents.XZ,
                      PositionOffset = c.PhysicsCollider.Shape3D.CompoundShapes[i].PositionOffset.XZ,
                      RotationOffset = c.PhysicsCollider.Shape3D.CompoundShapes[i].RotationOffset.Y,
                   });
                }
                c.PhysicsCollider.Shape3D.ShapeType = Shape3DType.None;
                c.PhysicsCollider.Shape2D.ShapeType = Shape2DType.Compound;
                c.PhysicsCollider.Shape2D.CompoundShapes = shapes.ToArray();
             }
             EditorUtility.SetDirty(c);
             return true;
          }
          return false;
       }
    }
}






