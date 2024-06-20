using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Quantum;

namespace FirstLight.Editor.Ids
{
	internal class GameIdEntry
	{
		public string Name;
		public int Id;
		public Ids.GroupSource[] Groups;
	}

	internal class GameIdHolder : IEnumerable<GameIdEntry>
	{
		private List<GameIdEntry> _internalList = new();

		public List<GameIdEntry> InternalList
		{
			get => _internalList;
		}

		public void Add(string key, int id, params Ids.GroupSource[] values)
		{
			InternalList.Add(new GameIdEntry() {Name = key, Id = id, Groups = values});
		}

		public IEnumerator<GameIdEntry> GetEnumerator()
		{
			return InternalList.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		public void CheckDuplicates()
		{
			// Id
			var groupedById = InternalList.GroupBy(e => e.Id, e => e).ToList();
			foreach (var grouped in groupedById)
			{
				if (grouped.Count() != 1)
				{
					throw new Exception("Duplicated id " + grouped.Key + " with names " +
						string.Join(",", grouped.ToList().Select(id => id.Name)));
				}
			}
			
			// Name
			var groupedByName = InternalList.GroupBy(e => e.Name, e => e).ToList();
			foreach (var grouped in groupedByName)
			{
				if (grouped.Count() != 1)
				{
					throw new Exception("Duplicated name " + grouped.Key + " with ids " +
						string.Join(",", grouped.ToList().Select(id => id.Id)));
				}
			}
			
		}
	}
}