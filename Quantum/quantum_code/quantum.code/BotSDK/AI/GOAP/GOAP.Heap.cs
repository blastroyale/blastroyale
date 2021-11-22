using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Quantum {
  public struct GOAPCompoundKey {
    public Int32 Foo;
    public Int32 Bar;

    public class Comparer : IEqualityComparer<GOAPCompoundKey> {
      public static Comparer Instance = new Comparer();

      public bool Equals(GOAPCompoundKey x, GOAPCompoundKey y) {
        return x.Bar == y.Bar && x.Foo == y.Foo;
      }

      public int GetHashCode(GOAPCompoundKey obj) {
        return obj.Bar ^ obj.Foo;
      }
    }
  }

  public static class Example {
    static void Test() {
      var dictionary = new Dictionary<GOAPCompoundKey, object>(GOAPCompoundKey.Comparer.Instance);
    }
  }

  public struct GOAPNode {
    public GOAPState Goal;
    public Int64 State;
    public Int64 ParentKey;
    public GOAPTask Action;
    public Int32 F;
    public Int32 G;
    public Int32 H;
    public Int32 Slot;
  }

  public class GOAPHeap {
    Int32 _size;
    GOAPNode[] _heap;

    public GOAPHeap(Int32 capacity) {
      _heap = new GOAPNode[capacity];
      _size = 1;
    }

    public GOAPHeap()
        : this(1024) {
    }

    public void Clear() {
      _size = 1;

      // remove all stuff from heap
      Array.Clear(_heap, 0, _heap.Length);
    }

    public Int32 Size {
      get { return _size - 1; }
    }

    public void Update(ref GOAPNode node) {
      var bubbleIndex = node.Slot;

      while (bubbleIndex != 1) {
        var parentIndex = bubbleIndex / 2;
        if (_heap[parentIndex].F > node.F) {
          _heap[bubbleIndex] = _heap[parentIndex];
          _heap[parentIndex] = node;

          _heap[parentIndex].Slot = parentIndex;
          _heap[bubbleIndex].Slot = bubbleIndex;

          bubbleIndex = parentIndex;
        } else
          break;
      }
    }

    public void Push(ref GOAPNode node) {
      if (_size == _heap.Length) {
        ExpandHeap();
      }

      var bubbleIndex = _size;
      _heap[bubbleIndex] = node;
      node.Slot = bubbleIndex;

      while (bubbleIndex != 1) {
        var parentIndex = bubbleIndex / 2;
        if (_heap[parentIndex].F > node.F) {
          _heap[bubbleIndex] = _heap[parentIndex];
          _heap[parentIndex] = node;

          _heap[parentIndex].Slot = parentIndex;
          _heap[bubbleIndex].Slot = bubbleIndex;

          bubbleIndex = parentIndex;
        } else {
          break;
        }
      }

      ++_size;
    }

    public GOAPNode Pop() {
      _size--;

      var returnItem = _heap[1];
      _heap[1] = _heap[_size];

      var swapItem = 1;
      var parent = 1;

      do {
        parent = swapItem;

        if ((2 * parent + 1) <= _size) {
          // Both children exist
          if (_heap[parent].F >= _heap[2 * parent].F) {
            swapItem = 2 * parent;
          }

          if (_heap[swapItem].F >= _heap[2 * parent + 1].F) {
            swapItem = 2 * parent + 1;
          }
        } else if ((2 * parent) <= _size) {
          // Only one child exists
          if (_heap[parent].F >= _heap[2 * parent].F) {
            swapItem = 2 * parent;
          }
        }

        // One if the parent's children are smaller or equal, swap them
        if (parent != swapItem) {
          var tmpIndex = _heap[parent];

          _heap[parent] = _heap[swapItem];
          _heap[swapItem] = tmpIndex;

          _heap[parent].Slot = parent;
          _heap[swapItem].Slot = swapItem;
        }

      } while (parent != swapItem);

      return returnItem;
    }

    void ExpandHeap() {
      // create new double sized heap
      var newHeap = new GOAPNode[_heap.Length * 2];

      // copy old stuff
      Array.Copy(_heap, newHeap, _heap.Length);

      // replace old heap
      _heap = newHeap;
    }
  }
}
