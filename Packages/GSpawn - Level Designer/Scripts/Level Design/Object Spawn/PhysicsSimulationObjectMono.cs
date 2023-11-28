#if UNITY_EDITOR
using UnityEngine;

namespace GSpawn
{
    [ExecuteInEditMode]
    public class PhysicsSimulationObjectMono : MonoBehaviour
    {
        public bool         is2DObject;
        public bool         hasRigidBody;
        public bool         isKinematic;

        public Rigidbody2D  rigidBody2D;
        public Rigidbody    rigidBody3D;

        public Collider2D   addedCollider2D;
        public Collider     addedCollider3D;

        public Vector3      velocity { get { return is2DObject ? rigidBody2D.velocity : rigidBody3D.velocity; } }

        public void onExitSimulation()
        {
            if (is2DObject)
            {
                if (addedCollider2D != null) Collider2D.DestroyImmediate(addedCollider2D);

                if (!hasRigidBody) Rigidbody2D.DestroyImmediate(rigidBody2D);
                else rigidBody2D.isKinematic = isKinematic;
            }
            else
            {
                if (addedCollider3D != null) Collider.DestroyImmediate(addedCollider3D);

                if (!hasRigidBody) Rigidbody.DestroyImmediate(rigidBody3D);
                else rigidBody3D.isKinematic = isKinematic;
            }
        }

        public void onEnterSimulation()
        {
            is2DObject          = gameObject.hierarchyHasSprite(false, false);

            if (is2DObject)
            {
                rigidBody2D     = gameObject.getRigidBody2D();
                hasRigidBody    = rigidBody2D != null;
                isKinematic     = rigidBody2D != null ? rigidBody2D.isKinematic : false;

                if (rigidBody2D == null)
                {
                    rigidBody2D = UndoEx.addComponent<Rigidbody2D>(gameObject);
                    rigidBody2D.isKinematic = false;
                }

                if (gameObject.getCollider2D() == null) addedCollider2D = UndoEx.addComponent<BoxCollider2D>(gameObject);
            }
            else
            {
                rigidBody3D     = gameObject.getRigidBody();
                hasRigidBody    = rigidBody3D != null;
                isKinematic     = rigidBody3D != null ? rigidBody3D.isKinematic : false;
             
                if (rigidBody3D == null)
                {
                    rigidBody3D = UndoEx.addComponent<Rigidbody>(gameObject);
                    rigidBody3D.isKinematic = false;
                }

                if (gameObject.getCollider() == null) addedCollider3D = UndoEx.addComponent<BoxCollider>(gameObject);
            }
        }
    }
}
#endif