using System;
using System.Collections.Generic;
using PlayFab.Internal;


namespace FirstLight.Game.Logic;

/// <summary>
/// This object defines the result of an function app logic execution.
// This is replicated from client due to client object inherits from PlayFab.SharedModels and server requires 
// PlayFab.Internal which client does not have.
/// </summary>
[Serializable]
public class BackendLogicResult : PlayFabResultCommon
{
    /// <summary>
    /// Player Id
    /// </summary>
    public string PlayFabId;
    /// <summary>
    /// The command executed
    /// </summary>
    public string Command;
    /// <summary>
    /// Extra Data to return back to the client executed from the logic request
    /// </summary>
    public Dictionary<string, string> Data;
}
