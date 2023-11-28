using System;
using Newtonsoft.Json;

[Serializable]
public class ShortLinkParams {
    private string data;
    private string error;

    public ShortLinkParams() {
    }

    [JsonProperty(PropertyName = "data")]
    public string Data {
        get {
            return data;
        }
        set {
            data = value;
        }
    }


    [JsonProperty(PropertyName = "error")]
    public string Error {
        get {
            return error;
        }
        set {
            error = value;
        }
    }


}
