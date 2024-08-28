using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Photon.Deterministic;

namespace Quantum.Systems
{
	/// <summary>
	/// This system handles all the <see cref="Collectable"/> component collection interactions using triggers 
	/// </summary>
	public unsafe class CollectableChunkSystem : SystemSignalsOnly, ISignalOnComponentAdded<Collectable>, ISignalOnComponentRemoved<Collectable>
	{
		public static FP ChunkSize = FP._10 * FP._3;

		public override void OnEnabled(Frame f)
		{
			var singleton = f.Unsafe.GetOrAddSingletonPointer<CollectableChunks>();
			var chunks = f.ResolveDictionary(singleton->Collectables);
			var chunksLength = f.Map.WorldSize / ChunkSize.AsInt + 1;

			for (int cx = 0; cx < chunksLength; cx++)
			{
				for (int cy = 0; cy < chunksLength; cy++)
				{
					var index = (short)(cx + cy * chunksLength);
					var newChunk = new CollectableChunk()
					{
						Entities = f.AllocateHashSet<EntityRef>()
					};
					chunks[index] = newChunk;
				}
			}
		}

		public void OnAdded(Frame f, EntityRef entity, Collectable* component)
		{
			var pos = f.Unsafe.GetPointer<Transform2D>(entity)->Position;
			var chunkIndex = GetChunk(f, pos);

			var singleton = f.Unsafe.GetOrAddSingletonPointer<CollectableChunks>();
			var chunks = f.ResolveDictionary(singleton->Collectables);
			if (chunks.TryGetValue(chunkIndex, out var chunk))
			{
				f.Add(entity, new ChunkDebug()
				{
					Chunk = chunkIndex
				});
				f.ResolveHashSet(chunk.Entities).Add(entity);
			}
		}

		public void OnRemoved(Frame f, EntityRef entity, Collectable* component)
		{
			var pos = f.Unsafe.GetPointer<Transform2D>(entity)->Position;
			var chunkIndex = GetChunk(f, pos);

			var singleton = f.Unsafe.GetOrAddSingletonPointer<CollectableChunks>();
			var chunks = f.ResolveDictionary(singleton->Collectables);
			if (chunks.TryGetValue(chunkIndex, out var chunk))
			{
				f.ResolveHashSet(chunk.Entities).Remove(entity);
			}
		}

		public static List<EntityComponentPointerPair<Collectable>> GetCollectables(Frame f, short chunk)
		{
			var entities = new List<EntityComponentPointerPair<Collectable>>();

			var singleton = f.Unsafe.GetOrAddSingletonPointer<CollectableChunks>();
			var chunksDictionary = f.ResolveDictionary(singleton->Collectables);
			if (chunksDictionary.TryGetValue(chunk, out var chunkEntities))
			{
				var entityList = f.ResolveHashSet(chunkEntities.Entities);
				foreach (var entityRef in entityList)
				{
					entities.Add(new EntityComponentPointerPair<Collectable>()
					{
						Entity = entityRef,
						Component = f.Unsafe.GetPointer<Collectable>(entityRef)
					});
				}
			}

			return entities;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static short AddChunks(Frame f, short chunk, short x, short y)
		{
			var chunksLength = f.Map.WorldSize / ChunkSize.AsInt + 1;
			return (short)(chunk + x + y * chunksLength);
		}

		public static (short, short) GetChunkPosition(Frame f, short chunk)
		{
			var chunksLength = f.Map.WorldSize / ChunkSize.AsInt + 1;
			return ((short, short))(chunk % chunksLength, chunk / chunksLength);
		}

		public static short GetChunk(Frame f, FPVector2 pos)
		{
			FP halfWorldSize = f.Map.WorldSize / 2;

			var cx = FPMath.FloorToInt((pos.X + halfWorldSize) / ChunkSize);
			int cy = FPMath.FloorToInt((pos.Y + halfWorldSize) / ChunkSize);
			var chunksLength = f.Map.WorldSize / ChunkSize + 1;
#if DEBUG
			if (cx < FP._0 || cy < FP._0 || cx + cy * chunksLength > chunksLength * chunksLength)
			{
				throw new Exception("Invalid object position!");
			}
#endif
			return (short)(cx + cy * chunksLength);
		}
	}
}