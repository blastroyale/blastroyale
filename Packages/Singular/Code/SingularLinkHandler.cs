using System;
using Newtonsoft.Json;

public interface SingularLinkHandler {
    void OnSingularLinkResolved(SingularLinkParams linkParams);
}

[Serializable]
public class SingularLinkParams {
    private string _deeplink;
    private string _passthrough;
    private bool _isDeferred;

    public SingularLinkParams() {
    }

    [JsonProperty(PropertyName = "deeplink")]
    public string Deeplink {
        get {
            return _deeplink;
        }
        set {
            _deeplink = value;
        }
    }


    [JsonProperty(PropertyName = "passthrough")]
    public string Passthrough {
        get {
            return _passthrough;
        }
        set {
            _passthrough = value;
        }
    }

    [JsonProperty(PropertyName = "is_deferred")]
    public bool IsDeferred {
        get {
            return _isDeferred;
        }
        set {
            _isDeferred = value;
        }
    }
}

