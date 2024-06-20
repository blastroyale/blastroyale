using System.Collections.Generic;
using FirstLight.Server.SDK.Models;
using Newtonsoft.Json;

namespace BlastRoyaleNFTPlugin.NftSyncs;

/// <summary>
/// Default FLG attribute structure
/// </summary>
public class FlgAttributeStructure
{
	public string trait_type;
	public string value;
}

/// <summary>
/// Parser to parse the FLG metadata format of
///  {
/// "trait_type": "generation",
/// "value": 3
/// },
/// </summary>
public class FlgTraitParser
{
	public readonly Dictionary<string, string> Traits = new();
	public readonly RemoteCollectionItem Nft;
	
	public FlgTraitParser(RemoteCollectionItem indexedNft)
	{
		Nft = indexedNft;
		foreach (var attr in indexedNft.Attributes)
		{
			var structure = JsonConvert.DeserializeObject<FlgAttributeStructure>(attr);
			Traits[structure.trait_type] = structure.value;
		}
	}
	
	public void AddAttribute(string key, string value)
	{
		Nft.Attributes.Add(JsonConvert.SerializeObject(new FlgAttributeStructure()
		{
			trait_type = key, value = value
		}));
		Traits[key] = value;
	}
}