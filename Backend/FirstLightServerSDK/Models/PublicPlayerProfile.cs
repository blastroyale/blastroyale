using System;
using System.Collections.Generic;

namespace FirstLight.Server.SDK.Models
{

	/// <summary>
	/// Serializable model for playfab statistics
	/// </summary>
	[Serializable]
	public struct Statistic
	{
		public string Name;
		public int Value;
		public int Version;
	}
	
	/// <summary>
	/// Serializable version of a player public profile
	/// </summary>
	[Serializable]
	public class PublicPlayerProfile 
	{
		public string Name;
		public string AvatarUrl;
		public List<Statistic> Statistics;
	}
}