using System;
using Newtonsoft.Json;

public interface SingularConversionValueUpdatedHandler {
    void OnConversionValueUpdated(int value);
}
