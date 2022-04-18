using System.Collections.Generic;
using System.Runtime.Serialization;

namespace FirstLight.Game.Data
{
	public struct TraitAttribute
	{
		public string trait_type;
		public int value;
	}
	
	public struct Erc721MetaData
	{
		public string name;
		public string description;
		public string image;
		public TraitAttribute[] attributes;
		public Dictionary<string, int> attibutesDictionary;

		[OnDeserialized]
		internal void OnDeserializedMethod(StreamingContext context)
		{
			attibutesDictionary = new Dictionary<string, int>(attributes.Length);

			for (var i = 0; i < attributes.Length; i++)
			{
				attibutesDictionary.Add(attributes[i].trait_type, attributes[i].value);
			}
		}
	}
}