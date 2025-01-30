using System.Collections.Generic;
using FirstLight.Server.SDK.Models;
using Newtonsoft.Json;

namespace BlastRoyaleNFTPlugin.Parsers;

/// <summary>
/// Default FLG TraitType attribute structure
/// </summary>
public class FLGTraitTypeAttributeStructure
{
	public string trait_type;
	public string value;
}

/// <summary>
/// Parses the FLG metadata format, specifically structures like:
/// {
///   "trait_type": "generation",
///   "value": 3
/// }
/// This parser is designed to extract and interpret the "trait_type" and "value" fields from the FLG metadata.
/// </summary>
public class FlgTraitTypeAttributeParser
{
	public readonly Dictionary<string, string> Traits = new();
	public readonly RemoteCollectionItem Nft;
	
	public FlgTraitTypeAttributeParser(RemoteCollectionItem indexedNft)
	{
		Nft = indexedNft;
		foreach (var attr in indexedNft.Attributes)
		{
			var structure = JsonConvert.DeserializeObject<FLGTraitTypeAttributeStructure>(attr);
			Traits[structure.trait_type] = structure.value;
		}
	}
	
	public void AddAttribute(string key, string value)
	{
		Nft.Attributes.Add(JsonConvert.SerializeObject(new FLGTraitTypeAttributeStructure()
		{
			trait_type = key, value = value
		}));
		
		Traits[key] = value;
	}
}