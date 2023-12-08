using System;
using System.Collections.Generic;

namespace Best.HTTP.Shared.Databases.Indexing
{
    public enum Side
    {
        Left,
        Right
    }

    /// <summary>
    /// Implements most common list functions. With best case (no or only one item) it doesn't do any allocation.
    /// </summary>
    public struct NoAllocList<T>
    {        
        private T _value;
        private bool _hasValue;

        private List<T> _values;

        public NoAllocList(T value)
        {
            this._value = value;
            this._hasValue = true;
            this._values = null;
        }

        public T this[int index] {
            get => this._hasValue ? this._value : this._values[index];
            set
            {
                if (index < 0 || (this._values == null && index > 0))
                    throw new IndexOutOfRangeException(index.ToString());

                if (this._values != null)
                    this._values[index] = value;
                else
                {
                    this._value = value;
                    this._hasValue = true;
                }
            }
        }

        public int Count { get => this._values != null ? this._values.Count : (this._hasValue ? 1 : 0); }

        public void Add(T item)
        {
            if (this._values != null)
                this._values.Add(item);
            else if (this._hasValue)
            {
                this._values = new List<T> { this._value, item };
                this._value = default(T);
                this._hasValue = false;
            }
            else
            {
                this._value = item;
                this._hasValue = true;
            }
        }

        public void Clear()
        {
            this._values?.Clear();
            this._values = null;
            this._value = default(T);
            this._hasValue = false;
        }

        public bool Contains(T item)
        {
            if (this._values != null)
                return this._values.Contains(item);

            // This can thrown a NullRefException if _value is null!
            return this._hasValue ? this._value.Equals(item) : false;
        }

        public bool Remove(T item)
        {
            if (this._values != null)
                return this._values.Remove(item);
            else if (this._hasValue && this._value.Equals(item))
            {
                this._value = default(T);
                this._hasValue = false;
                return true;
            }

            return false;
        }

        public void RemoveAt(int index)
        {
            if (index < 0 || (this._values == null && index > 0))
                throw new IndexOutOfRangeException(index.ToString());

            if (this._values != null)
                this._values.RemoveAt(index);
            else
            {
                this._value = default(T);
                this._hasValue = false;
            }
        }
    }

    public sealed class Node<KeyT, ValueT>
    {
        public Node<KeyT, ValueT> Parent, Left, Right;

        public KeyT Key { get; private set; }

        /// <summary>
        /// Depth of the node.
        /// </summary>
        public int Depth;

        /// <summary>
        /// Difference between LeftDepth and RightDepth.
        /// </summary>
        public int BalanceFactor { get { return this.LeftDepth - this.RightDepth; } }

        /// <summary>
        /// Left node's Depth, or -1 if it's null.
        /// </summary>
        public int LeftDepth { get { return this.Left == null ? -1 : this.Left.Depth; } }

        /// <summary>
        /// Right node's Depth, or -1 if it's null.
        /// </summary>
        public int RightDepth { get { return this.Right == null ? -1 : this.Right.Depth; } }

        public bool IsRoot { get { return this.Parent == null; } }

        public int ChildCount { get { return (this.Left == null ? 0 : 1) + (this.Right == null ? 0 : 1); } }

        // Stored values aren't public as modifing them requires special care.
        private NoAllocList<ValueT> _item;

        public Node(Node<KeyT, ValueT> parent, KeyT key, ValueT value)
        {
            this.Parent = parent;
            this.Key = key;
            this._item = new NoAllocList<ValueT>(value);

            // Depth is 0 by default, as it has no child
            this.Depth = 0;
        }

        public void BubbleUpDepthChange()
        {
            var current = this;

            while (current != null)
            {
                var oldDepth = current.Depth;
                current.Depth = Math.Max(current.LeftDepth, current.RightDepth) + 1;

                if (oldDepth != current.Depth)
                    current = current.Parent;
                else
                    break;
            }
        }

        public ValueT this[int index] { get => this._item[index]; }

        public int Count { get => this._item.Count; }

        public void Clear() => this._item = new NoAllocList<ValueT>();

        public void Add(ValueT value)
        {
            var tmp = this._item;
            tmp.Add(value);
            this._item = tmp;
        }

        public bool Remove(ValueT value)
        {
            var tmp = this._item;
            var result = tmp.Remove(value);
            if (result)
                this._item = tmp;
            return result;
        }

        public List<ValueT> ToList()
        {
            var list = new List<ValueT>(this._item.Count);

            for (int i = 0; i < this._item.Count; ++i)
                list.Add(this._item[i]);

            return list;
        }

        public override string ToString()
        {
            return $"{this.Left?.Key.ToString()} <- {this.Key.ToString()} -> {this.Right?.Key.ToString()}";
        }
    }

    // https://www.codesdope.com/course/data-structures-avl-trees/
    public sealed class AVLTree<Key, Value>
    {
        public int ElemCount { get; private set; }
        public int NodeCount { get; private set; }
        public IComparer<Key> Comparer;

        public Node<Key, Value> RootNode { get; private set; } = null;

        public AVLTree(IComparer<Key> comparer)
        {
            this.Comparer = comparer;
        }

        public void Add(Key key, Value item, bool clearValues = false)
        {
            if (this.RootNode == null) {
                this.NodeCount++;
                this.ElemCount++;
                this.RootNode = new Node<Key, Value>(null, key, item);
                return;
            }

            var current = this.RootNode;
            do
            {
                // +--------------------+-----------------------+
                // |        Value       |     Meaning           |
                // +--------------------+-----------------------+
                // | Less than zero     |  x is less than y.    |
                // | Zero               |  x equals y.          |
                // | Greater than zero  |  x is greater than y. |
                // +--------------------------------------------+
                int comp = this.Comparer.Compare(/*x: */ current.Key, /*y: */ key);

                // equals
                if (comp == 0)
                {
                    if (clearValues)
                    {
                        this.ElemCount -= current.Count;
                        current.Clear();
                    }

                    current.Add(item);
                    break;
                }

                // current's key > key
                if (comp > 0)
                {
                    // insert new node
                    if (current.Left == null)
                    {
                        current.Left = new Node<Key, Value>(current, key, item);
                        current.BubbleUpDepthChange(/*Side.Left, 1*/);

                        current = current.Left;

                        this.NodeCount++;
                        break;
                    }
                    else
                    {
                        current = current.Left;
                        continue;
                    }
                }

                // current's key < key
                if (comp < 0)
                {
                    // insert new node
                    if (current.Right == null)
                    {
                        current.Right = new Node<Key, Value>(current, key, item);
                        current.BubbleUpDepthChange(/*Side.Right, 1*/);

                        current = current.Right;

                        this.NodeCount++;
                        break;
                    }
                    else
                    {
                        current = current.Right;
                        continue;
                    }
                }
            } while (true);

            this.ElemCount++;

            while (RebalanceFrom(current) != null)
                ;

            //TestBalance(this.root);
        }

        public bool TestBalance() => TestBalance(this.RootNode);

        private bool TestBalance(Node<Key, Value> node)
        {
            if (node == null)
                return true;

            if (Math.Abs(node.BalanceFactor) > 1)
            {
                //UnityEngine.Debug.Break();
                return false;
            }

            return TestBalance(node.Left) && TestBalance(node.Right);
        }

        List<Side> path = new List<Side>(2);

        private Node<Key, Value> RebalanceFrom(Node<Key, Value> newNode)
        {
            if (newNode.IsRoot || newNode.Parent.IsRoot)
                return null;

            path.Clear();

            // find first unbalanced node or exit when found the root node (root still can be unbalanced!)
            var current = newNode;
            var balanceFactor = current.BalanceFactor;
            while (!current.IsRoot && Math.Abs(balanceFactor) <= 1)
            {
                if (current.Parent.Left == current)
                    path.Add(Side.Left);
                else
                    path.Add(Side.Right);

                current = current.Parent;
                balanceFactor = current.BalanceFactor;
            }

            // it's a balanced tree
            if (Math.Abs(balanceFactor) <= 1)
                return null;

            Side last = path[path.Count - 1];// path[path.StartIdx];
            Side prev = path[path.Count - 2];// path[path.EndIdx];

            if (last == Side.Left && prev == Side.Left)
            {
                // insertion to a left child of a left child
                RotateRight(current)
                    .BubbleUpDepthChange();
                current.BubbleUpDepthChange();
            }
            else if (last == Side.Right && prev == Side.Right)
            {
                // insertion to a right child of a right child
                RotateLeft(current)
                    .BubbleUpDepthChange();
                current.BubbleUpDepthChange();
            }
            else if (last == Side.Right && prev == Side.Left)
            {
                // insertion to a left child of a right child
                var current_right = current.Right;
                RotateRight(current.Right);
                RotateLeft(current);

                current_right.BubbleUpDepthChange();
                current.BubbleUpDepthChange();
            }
            else if (last == Side.Left && prev == Side.Right)
            {
                // insertion to a right child of a left child

                var current_left = current.Left;
                RotateLeft(current.Left);
                RotateRight(current);

                current_left.BubbleUpDepthChange();
                current.BubbleUpDepthChange();
            }

            return current;
        }

        public void Clear()
        {
            this.RootNode = null;
            this.ElemCount = 0;
            this.NodeCount = 0;
        }

        public List<Value> Remove(Key key)
        {
            if (this.RootNode == null)
                return null;

            var current = this.RootNode;
            do
            {
                int comp = this.Comparer.Compare(current.Key, key);

                // equals
                if (comp == 0)
                {
                    this.NodeCount--;
                    this.ElemCount -= current.Count;

                    // remove current node from the tree
                    RemoveNode(current);

                    return current.ToList();
                }

                // current's key > key
                if (comp > 0)
                {
                    if (current.Left == null)
                        return null;
                    else
                    {
                        current = current.Left;
                        continue;
                    }
                }

                // current's key < key
                if (comp < 0)
                {
                    if (current.Right == null)
                        return null;
                    else
                    {
                        current = current.Right;
                        continue;
                    }
                }
            } while (true);
        }

        public void Remove(Key key, Value value)
        {
            if (this.RootNode == null)
                return;

            var current = this.RootNode;
            do
            {
                int comp = this.Comparer.Compare(current.Key, key);

                // equals
                if (comp == 0)
                {
                    if (current.Remove(value))
                        this.ElemCount--;

                    if (current.Count == 0)
                    {
                        // remove current node from the tree
                        RemoveNode(current);

                        this.NodeCount--;
                    }

                    return;
                }

                // current's key > key
                if (comp > 0)
                {
                    if (current.Left == null)
                        return ;
                    else
                    {
                        current = current.Left;
                        continue;
                    }
                }

                // current's key < key
                if (comp < 0)
                {
                    if (current.Right == null)
                        return ;
                    else
                    {
                        current = current.Right;
                        continue;
                    }
                }
            } while (true);
        }

        /// <summary>
        /// Removes node and reparent any child it has.
        /// </summary>
        private void RemoveNode(Node<Key, Value> node)
        {
            var parent = node.Parent;
            Side side = parent?.Left == node ? Side.Left : Side.Right;
            int childCount = node.ChildCount;

            var testForRebalanceNode = parent;

            switch(childCount)
            {
                case 0:
                    // node has no child

                    if (parent == null)
                    {
                        this.RootNode = null;
                    }
                    else
                    {
                        if (parent.Left == node)
                            parent.Left = null;
                        else
                            parent.Right = null;
                        parent.BubbleUpDepthChange();
                    }

                    node.Parent = null;
                    break;

                case 1:
                    // re-parent the only child
                    // Example: Removing node 25 will replace it with 30
                    //
                    //      20
                    //  15      25
                    //              30
                    //  
                    var child = node.Left ?? node.Right;

                    if (parent == null)
                    {
                        this.RootNode = child;
                        this.RootNode.Parent = null;
                    }
                    else
                    {
                        if (parent.Left == node)
                            parent.Left = child;
                        else
                            parent.Right = child;
                        child.Parent = parent;

                        parent.BubbleUpDepthChange();
                    }
                    break;

                default:
                    // two child

                    // 1: Replace 20 with 25
                    //
                    //      20                     20
                    //  15      25             15      25
                    //                                     30
                    //  

                    // 2: Replace 20 with 22
                    // 
                    //      20
                    //  15      25
                    //       22

                    // 3: Re-parent 23 for 25, replace 20 with 22
                    //
                    //      20
                    //  15      25
                    //       22
                    //          23

                    // Cases 1 and 3 are the same, both 25 and 22 has a right child. But in case 3, 22 isn't first child of 20!

                    // Find node with the least Key, that's a node without a left node so we have to deal only with its right node.
                    var nodeToReplaceWith = FindMin(node.Right);

                    testForRebalanceNode = nodeToReplaceWith;
                    side = Side.Right;

                    // re-parent 23 in case 3:
                    if (nodeToReplaceWith.Parent != node)
                    {
                        testForRebalanceNode = nodeToReplaceWith.Parent;
                        if (nodeToReplaceWith.Parent.Left == nodeToReplaceWith)
                        {
                            nodeToReplaceWith.Parent.Left = nodeToReplaceWith.Right;
                            side = Side.Left;
                        }
                        else
                        {
                            nodeToReplaceWith.Parent.Right = nodeToReplaceWith.Right;
                            side = Side.Right;
                        }

                        if (nodeToReplaceWith.Right != null)
                            nodeToReplaceWith.Right.Parent = nodeToReplaceWith.Parent;
                    }

                    if (parent == null)
                        this.RootNode = nodeToReplaceWith;
                    else
                    {
                        if (parent.Left == node)
                            parent.Left = nodeToReplaceWith;
                        else
                            parent.Right = nodeToReplaceWith;
                    }
                    nodeToReplaceWith.Parent = parent;

                    // Reparent node's left
                    nodeToReplaceWith.Left = node.Left;
                    node.Left.Parent = nodeToReplaceWith;

                    // Reparent node's right node, if it's not the one we replaceing it with
                    if (node.Right != nodeToReplaceWith)
                    {
                        nodeToReplaceWith.Right = node.Right;
                        node.Right.Parent = nodeToReplaceWith;
                    }
                    //else
                    //    nodeToReplaceWith.Right = null;

                    if (testForRebalanceNode != nodeToReplaceWith)
                        testForRebalanceNode.BubbleUpDepthChange();
                    nodeToReplaceWith.BubbleUpDepthChange();

                    break;
            }

            while (RebalanceForRemoval(testForRebalanceNode, side) != null)
                ;

            //TestBalance(this.root);
        }

        private Node<Key, Value> RebalanceForRemoval(Node<Key, Value> removedParentNode, Side side)
        {
            if (removedParentNode == null)
                return null;

            path.Clear();
            path.Add(side);

            // find first unbalanced node or exit when found the root node (root still can be unbalanced!)
            var current = removedParentNode;
            while (!current.IsRoot && Math.Abs(current.BalanceFactor) <= 1)
            {
                if (current.Parent.Left == current)
                    path.Add(Side.Left);
                else
                    path.Add(Side.Right);

                current = current.Parent;
            }

            // it's a balanced tree
            if (Math.Abs(current.BalanceFactor) <= 1)
                return null;

            // from what direction we came from
            Side fromDirection = path[path.Count - 1];

            // check weather it's an inside or outside case
            switch (fromDirection)
            {
                case Side.Right:
                    {
                        bool isOutside = current.Left.LeftDepth >= current.Left.RightDepth;
                        if (isOutside)
                        {
                            RotateRight(current)
                                .BubbleUpDepthChange();
                            current.BubbleUpDepthChange();
                        }
                        else
                        {
                            var current_left = current.Left;
                            RotateLeft(current.Left);
                            RotateRight(current);

                            current_left.BubbleUpDepthChange();
                            current.BubbleUpDepthChange();
                        }
                    }
                    break;

                case Side.Left:
                    {
                        bool isOutside = current.Right.RightDepth >= current.Right.LeftDepth;
                        if (isOutside)
                        {
                            // Example: Removing node 14 result in a disbalance in node 20
                            //
                            //         20
                            //     15       25
                            // (14)      22    26
                            //                    27

                            var current_right = current.Right; // node 25
                            RotateLeft(current);
                            // After RotateLeft(current: node 20):
                            //        25
                            //     20    26
                            //   15  22     27

                            current.BubbleUpDepthChange(); // node 20
                            current_right.BubbleUpDepthChange(); // node 25
                        }
                        else
                        {
                            // Example: Removing node 14 results in a disbalance in node 20.
                            //
                            //         20
                            //     15       25
                            // (14)      22    26
                            //             23

                            var current_right = current.Right;

                            RotateRight(current.Right);
                            // After RotateRight(current.Right: node 22):
                            //         20
                            //      15     22
                            //                  25
                            //               23    26

                            RotateLeft(current);
                            // After RotateLeft(current: node 20):
                            //          22
                            //      20      25
                            //   15      23    26

                            current.BubbleUpDepthChange();
                            current_right.BubbleUpDepthChange();
                        }
                    }
                    break;
            }

            return current;
        }

        private Node<Key, Value> FindMin(Node<Key, Value> node)
        {
            var current = node;

            while (current.Left != null)
                current = current.Left;

            return current;
        }

        private Node<Key, Value> FindMax(Node<Key, Value> node)
        {
            var current = node;
            while (current.Right != null)
                current = current.Right;

            return current;
        }

        public List<Value> Find(Key key) {
            if (this.RootNode == null)
                return null;

            var current = this.RootNode;
            do
            {
                int comp = this.Comparer.Compare(current.Key, key);

                // equals
                if (comp == 0)
                    return current.ToList();

                // current's key > key
                if (comp > 0)
                {
                    if (current.Left == null)
                        return null;
                    else
                    {
                        current = current.Left;
                        continue;
                    }
                }

                // current's key < key
                if (comp < 0)
                {
                    if (current.Right == null)
                        return null;
                    else
                    {
                        current = current.Right;
                        continue;
                    }
                }
            } while (true);
        }

        public IEnumerable<Value> WalkHorizontal()
        {
            if (this.RootNode == null)
                yield break;

            Queue<Node<Key, Value>> toWalk = new Queue<Node<Key, Value>>();

            toWalk.Enqueue(this.RootNode);

            while (toWalk.Count > 0)
            {
                var current = toWalk.Dequeue();

                if (current.Left != null)
                    toWalk.Enqueue(current.Left);
                if (current.Right != null)
                    toWalk.Enqueue(current.Right);

                for (int i = 0; i < current.Count; i++)
                    yield return current[i];
            }
        }

        public bool ContainsKey(Key key)
        {
            if (this.RootNode == null)
                return false;

            var current = this.RootNode;
            do
            {
                int comp = this.Comparer.Compare(current.Key, key);

                // equals
                if (comp == 0)
                    return true;

                // current's key > key
                if (comp > 0)
                {
                    if (current.Left == null)
                        return false;
                    else
                    {
                        current = current.Left;
                        continue;
                    }
                }

                // current's key < key
                if (comp < 0)
                {
                    if (current.Right == null)
                        return false;
                    else
                    {
                        current = current.Right;
                        continue;
                    }
                }
            } while (true);
        }

        private Node<Key, Value> RotateRight(Node<Key, Value> current)
        {
            // Current\        
            //          20              15
            //      15              10      20
            //  10      ?                 ?
            var parent = current.Parent;
            var leftChild = current.Left;

            // re-parent left child
            if (parent != null)
            {
                if (parent.Left == current)
                    parent.Left = leftChild;
                else
                    parent.Right = leftChild;
            }
            else
                this.RootNode = leftChild;
            leftChild.Parent = parent;

            // re-parent left child's right child
            if (leftChild.Right != null)
                leftChild.Right.Parent = current;
            current.Left = leftChild.Right;

            // re-parent current
            current.Parent = leftChild;
            leftChild.Right = current;

            // return with the node that took the place of current
            return leftChild;
        }

        private Node<Key, Value> RotateLeft(Node<Key, Value> current)
        {
            //    /Current        
            //  20              15        
            //     15       20      10
            //   ?    10       ?
            var parent = current.Parent;
            var rightChild = current.Right;

            // re-parent right child
            if (parent != null)
            {
                if (parent.Left == current)
                    parent.Left = rightChild;
                else
                    parent.Right = rightChild;
            }
            else
                this.RootNode = rightChild;
            rightChild.Parent = parent;

            // re-parent right child's left child
            if (rightChild.Left != null)
                rightChild.Left.Parent = current;
            current.Right = rightChild.Left;

            // re-parent current
            current.Parent = rightChild;
            rightChild.Left = current;

            return rightChild;
        }
    }
}
