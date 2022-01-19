using System;
using System.Collections;
using System.Collections.Generic;

namespace Quantum
{
	public class GOAPHeap : IEnumerable, IEnumerable<GOAPNode>
	{
		private GOAPNode[] _heap;
		private int _size;

		public GOAPHeap(int capacity)
		{
			_heap = new GOAPNode[capacity];
		}

		public GOAPHeap() : this(1024)
		{
		}

		public int Size { get { return _size; } }

		public void Clear()
		{
			_size = 0;

			// remove all stuff from heap
			Array.Clear(_heap, 0, _heap.Length);
		}

		public void Update(GOAPNode updateNode)
		{
			int bubbleIndex = -1;
			for (int i = 0; i < _size; i++)
			{
				var node = _heap[i];
				if (node.Hash == updateNode.Hash)
				{
					bubbleIndex = i;
					break;
				}
			}

			if (bubbleIndex < 0)
			{
				Log.Error($"Cannot update node: Node with hash {updateNode.Hash} is not present in the heap");
				return;
			}

			_heap[bubbleIndex] = updateNode;

			while (bubbleIndex != 0)
			{
				int parentIndex = (bubbleIndex - 1) / 2;

				if (_heap[parentIndex].F <= updateNode.F)
					break;

				_heap[bubbleIndex] = _heap[parentIndex];
				_heap[parentIndex] = updateNode;

				bubbleIndex = parentIndex;
			}
		}

		public void Push(GOAPNode node)
		{
			if (_size == _heap.Length)
			{
				ExpandHeap();
			}

			int bubbleIndex = _size;
			_heap[bubbleIndex] = node;

			_size++;

			while (bubbleIndex != 0)
			{
				int parentIndex = (bubbleIndex - 1) / 2;
				if (_heap[parentIndex].F <= node.F)
					break;

				_heap[bubbleIndex] = _heap[parentIndex];
				_heap[parentIndex] = node;

				bubbleIndex = parentIndex;
			}
		}

		public GOAPNode Pop()
		{
			GOAPNode returnItem = _heap[0];
			_heap[0] = _heap[_size - 1];

			_size--;

			int swapItem = 0;
			int parent = 0;

			do
			{
				parent = swapItem;

				int leftChild = 2 * parent + 1;
				int rightChild = 2 * parent + 2;

				if (rightChild <= _size)
				{
					int smallerChild = _heap[leftChild].F < _heap[rightChild].F ? leftChild : rightChild;

					if (_heap[parent].F >= _heap[smallerChild].F)
					{
						swapItem = smallerChild;
					}
				}
				else if (leftChild <= _size)
				{
					// Only one child exists
					if (_heap[parent].F >= _heap[leftChild].F)
					{
						swapItem = leftChild;
					}
				}

				// One if the parent's children are smaller or equal, swap them
				if (parent != swapItem)
				{
					GOAPNode tmpIndex = _heap[parent];

					_heap[parent] = _heap[swapItem];
					_heap[swapItem] = tmpIndex;
				}
			}
			while (parent != swapItem);

			return returnItem;
		}

		private void ExpandHeap()
		{
			// Double the size
			GOAPNode[] newHeap = new GOAPNode[_heap.Length * 2];

			Array.Copy(_heap, newHeap, _heap.Length);
			_heap = newHeap;
		}

		IEnumerator<GOAPNode> IEnumerable<GOAPNode>.GetEnumerator()
		{
			for (int i = 0; i < _size; i++)
			{
				yield return _heap[i];
			}
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return (this as IEnumerable<GOAPNode>).GetEnumerator();
		}
	}
}