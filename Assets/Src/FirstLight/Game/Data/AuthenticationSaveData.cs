using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;

/// <summary>
/// Data object used for storing specific authentication data locally
/// </summary>
[Serializable,JsonObject(MemberSerialization.OptIn)]
public class AuthenticationSaveData
{
	[JsonProperty]
	public string LastLoginEmail { get; set; }
	
	[JsonProperty]
	public bool LinkedDevice { get; set; }
}

