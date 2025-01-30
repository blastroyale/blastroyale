namespace FirstLightServerSDK.Modules
{
	/// <summary>
	/// Extensions for generating OS agnostic deterministic hashes
	/// </summary>
	public static class Hasher
	{
		/// <summary>
		/// Overrides default net 6 back to 4.7 generation of string hash codes
		/// so its deterministic across different OS & .net versions
		/// Using unsafe to save up unboxing validation CPU cycles
		/// This function uses base Marvin32 hashing
		/// <seealso cref="https://referencesource.microsoft.com/#mscorlib/system/string.cs,827"/>
		/// </summary>
		public static int GetDeterministicHashCode(this string str)
		{
			if (string.IsNullOrEmpty(str))
			{
				return 0;
			}
			int hash1 = 5381;
			int hash2 = hash1;
			unsafe
			{
				fixed (char* src = str)
				{
					int counter;
					char* charPointer = src;
					while ((counter = charPointer[0]) != 0)
					{
						hash1 = ((hash1 << 5) + hash1) ^ counter;
						counter = charPointer[1];
						if (counter == 0)
							break;
						hash2 = ((hash2 << 5) + hash2) ^ counter;
						charPointer += 2;
					}
				}
			}
			return hash1 + (hash2 * 1566083941);
		}
	}
}