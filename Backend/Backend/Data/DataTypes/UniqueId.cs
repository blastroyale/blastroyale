using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Backend.Data.DataTypes
{/// <summary>
	/// Used to reference any entity by an unique Id value
	/// </summary>
	[Serializable]
	public struct UniqueId : IEquatable<UniqueId>, IComparable<UniqueId>, IComparable<uint>
	{
		public static readonly UniqueId Invalid = new UniqueId(0);

		public readonly uint Id;
		
		[JsonIgnore]
		public bool IsValid => this != Invalid;

		public UniqueId(uint id)
		{
			Id = id;
		}
 
		/// <inheritdoc />
		public override int GetHashCode()
		{
			return (int)Id;
		}

		/// <inheritdoc />
		public int CompareTo(UniqueId value)
		{
			if (Id < value.Id)
			{
				return -1;
			}
			
			return Id > value.Id ? 1 : 0;
		}

		/// <inheritdoc />
		public int CompareTo(uint value)
		{
			if (Id < value)
			{
				return -1;
			}
			
			return Id > value ? 1 : 0;
		}

		/// <inheritdoc />
		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj))
			{
				return false;
			}
 
			return obj is UniqueId && Equals((UniqueId)obj);
		}
 
		/// <inheritdoc />
		public override string ToString()
		{
			return Id.ToString();
		}

		public bool Equals(UniqueId other)
		{
			return Id == other.Id;
		}
 
		public static bool operator ==(UniqueId p1, UniqueId p2)
		{
			return p1.Id == p2.Id;
		}
 
		public static bool operator !=(UniqueId p1, UniqueId p2)
		{
			return p1.Id != p2.Id;
		}
		
		public static implicit operator uint(UniqueId id)
		{
			return id.Id;
		}
		
		public static implicit operator long(UniqueId id)
		{
			return id.Id;
		}
		
		public static implicit operator UniqueId(uint id)
		{
			return new UniqueId(id);
		}
		
		public static implicit operator UniqueId(long id)
		{
			if (id < 0)
			{
				throw new InvalidCastException($"UniqueId cannot haven negative id values and is being created with {id}");
			}
			return new UniqueId((uint) id);
		}
		
		public static implicit operator UniqueId(string id)
		{
			return new UniqueId(uint.Parse(id));
		}
	}

	/// <summary>
	/// Avoids boxing for Dictionary
	/// </summary>
	public class UniqueIdKeyComparer : IEqualityComparer<UniqueId>
	{
		/// <inheritdoc />
		public bool Equals(UniqueId x, UniqueId y)
		{
			return x.Id == y.Id;
		}

		/// <inheritdoc />
		public int GetHashCode(UniqueId obj)
		{
			return (int) obj.Id;
		}
	}
}