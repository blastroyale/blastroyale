#if UNITY_EDITOR
using System;
using System.Collections.Generic;

namespace GSpawn
{
    public class CumulativeProbabilityTable<TEntity>
    {
        private class Entry
        {
            public TEntity  entity;
            public float    probability;
            public float    normProbability;
            public float    cumulProbability;
        }

        private List<Entry> _entries = new List<Entry>();

        public int numEntities { get { return _entries.Count; } }

        public TEntity pickEntity()
        {
            float randomVal = UnityEngine.Random.Range(0.0f, 1.0f);
            foreach (var entry in _entries)
            {
                if (entry.cumulProbability >= randomVal) return entry.entity;
            }

            return default(TEntity);
        }

        public void addEntity(TEntity entity, float probability) 
        {
            _entries.Add(new Entry { entity = entity, probability = probability });
        }

        public void addEntityAndRefresh(TEntity entity, float probability)
        {
            _entries.Add(new Entry { entity = entity, probability = probability });
            refresh();
        }

        public int removeAll(Predicate<TEntity> match)
        {
            int numRemoved = _entries.RemoveAll(item => match(item.entity));
            if (numRemoved != 0) refresh();

            return numRemoved;
        }

        public void removeEntityAndRefresh(TEntity entity)
        {
            if (_entries.RemoveAll(item => item.entity.Equals(entity)) != 0)
                refresh();
        }

        public void clear()
        {
            _entries.Clear();
        }

        public bool containsEntity(TEntity entity)
        {
            return _entries.Find(item => item.entity.Equals(entity)) != null;
        }

        public void refresh()
        {
            if (_entries.Count == 0) return;

            float invSum = 0.0f;
            foreach (var entry in _entries)
                invSum += entry.probability;

            invSum = 1.0f / invSum;
            foreach (var entry in _entries)
                entry.normProbability = entry.probability * invSum;

            _entries.Sort(delegate (Entry e0, Entry e1)
            { return e0.normProbability.CompareTo(e1.normProbability); });

            for (int index = 0; index < _entries.Count; ++index)
            {
                var entry = _entries[index];
                entry.cumulProbability = entry.normProbability;
                if (index > 0) entry.cumulProbability += _entries[index - 1].cumulProbability;
            }
        }
    }
}
#endif