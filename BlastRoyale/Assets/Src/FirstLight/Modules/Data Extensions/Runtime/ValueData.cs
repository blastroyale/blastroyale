using System;
using UnityEngine;

// ReSharper disable once CheckNamespace

namespace FirstLight
{
	[Serializable]
	public struct Pair<TKey, TValue>
	{
		public TKey Key;
		public TValue Value;

		public Pair(TKey key, TValue value)
		{
			Key = key;
			Value = value;
		}
		
		public override string ToString()
		{
			return $"[{Key.ToString()},{Value.ToString()}]";
		}
	}
	
	[Serializable]
	public struct Vector4Int
	{
		public int x;
		public int y;
		public int z;
		public int w;

		public Vector4Int(int x, int y, int z, int w)
		{
			this.x = x;
			this.y = y;
			this.z = z;
			this.w = w;
		}
	}
	
	[Serializable]
	public struct Vector4Serializable
	{
		public float x;
		public float y;
		public float z;
		public float w;

		public Vector4Serializable(float x, float y, float z, float w)
		{
			this.x = x;
			this.y = y;
			this.z = z;
			this.w = w;
		}

		public static implicit operator Vector4Serializable(Vector4 v)
		{
			return new Vector4Serializable(v.x, v.y, v.z, v.w);
		}

		public static implicit operator Vector4Serializable(Quaternion v)
		{
			return new Vector4Serializable(v.x, v.y, v.z, v.w);
		}

		public static implicit operator Vector4(Vector4Serializable v)
		{
			return new Vector4(v.x, v.y, v.z, v.w);
		}

		public static implicit operator Quaternion(Vector4Serializable v)
		{
			return new Quaternion(v.x, v.y, v.z, v.w);
		}
	}
	
	[Serializable]
	public struct Vector3Serializable
	{
		public float x;
		public float y;
		public float z;

		public Vector3Serializable(float x, float y, float z)
		{
			this.x = x;
			this.y = y;
			this.z = z;
		}

		public static implicit operator Vector3Serializable(Vector3 v)
		{
			return new Vector3Serializable(v.x, v.y, v.z);
		}

		public static implicit operator Vector3(Vector3Serializable v)
		{
			return new Vector3(v.x, v.y, v.z);
		}
	}
	
	[Serializable]
	public struct Vector2Serializable
	{
		public float x;
		public float y;

		public Vector2Serializable(float x, float y)
		{
			this.x = x;
			this.y = y;
		}

		public static implicit operator Vector2Serializable(Vector2 v)
		{
			return new Vector2Serializable(v.x, v.y);
		}

		public static implicit operator Vector2(Vector2Serializable v)
		{
			return new Vector3(v.x, v.y);
		}
	}
	
	[Serializable]
	public struct Vector3IntSerializable
	{
		public int x;
		public int y;
		public int z;

		public Vector3IntSerializable(int x, int y, int z)
		{
			this.x = x;
			this.y = y;
			this.z = z;
		}

		public static implicit operator Vector3IntSerializable(Vector3Int v)
		{
			return new Vector3IntSerializable(v.x, v.y, v.z);
		}

		public static implicit operator Vector3Int(Vector3IntSerializable v)
		{
			return new Vector3Int(v.x, v.y, v.z);
		}
	}
	
	[Serializable]
	public struct Vector2IntSerializable
	{
		public int x;
		public int y;

		public Vector2IntSerializable(int x, int y)
		{
			this.x = x;
			this.y = y;
		}

		public static implicit operator Vector2IntSerializable(Vector2Int v)
		{
			return new Vector2IntSerializable(v.x, v.y);
		}

		public static implicit operator Vector2Int(Vector2IntSerializable v)
		{
			return new Vector2Int(v.x, v.y);
		}
	}
}