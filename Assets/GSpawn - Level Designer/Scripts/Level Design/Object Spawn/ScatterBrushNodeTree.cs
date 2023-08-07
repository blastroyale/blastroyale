#if UNITY_EDITOR
using System.Collections.Generic;

namespace GSpawn
{
    public class ScatterBrushNodeTree
    {
        private List<BinarySphereTreeNode<ScatterBrushNode>>    _sphereNodeBuffer   = new List<BinarySphereTreeNode<ScatterBrushNode>>();
        private BinarySphereTree<ScatterBrushNode>              _tree               = new BinarySphereTree<ScatterBrushNode>();

        public bool initialize()
        {
            return _tree.initialize(0.0f);
        }

        public void clear()
        {
            _tree.clear();
        }

        public void addNode(ScatterBrushNode node)
        {
            Sphere sphere = new Sphere(node.obb);
            _tree.createLeafNode(sphere.center, sphere.radius, node);
        }

        public bool checkBoxOverlap(OBB obb)
        {           
            if (_tree.overlapBox(obb, _sphereNodeBuffer))
            {
                foreach (var node in _sphereNodeBuffer) 
                {
                    if (node.data.obb.intersectsOBB(obb)) return true;
                }
            }

            return false;
        }
    }
}
#endif