using System;
using System.Collections.Generic;

namespace Best.HTTP.Shared.Databases
{
    public abstract class IndexingService<ContentType, MetadataType> where MetadataType : Metadata
    {
        /// <summary>
        /// Index newly added metadata
        /// </summary>
        public virtual void Index(MetadataType metadata) { }

        /// <summary>
        /// Remove metadata from all indexes.
        /// </summary>
        public virtual void Remove(MetadataType metadata) { }

        /// <summary>
        /// Clear all indexes
        /// </summary>
        public virtual void Clear() { }

        /// <summary>
        /// Get indexes in an optimized order. This is usually one of the indexes' WalkHorizontal() call.
        /// </summary>
        public virtual IEnumerable<int> GetOptimizedIndexes() => null;
    }
}
