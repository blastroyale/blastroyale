using System;

namespace Quantum
{
	/// <summary>
	/// Marks a system as optional, meaning it must be turned on manually (by a game mode config).
	/// </summary>
	[AttributeUsage(AttributeTargets.Class)]
	public class OptionalSystemAttribute : Attribute
	{
	}
}