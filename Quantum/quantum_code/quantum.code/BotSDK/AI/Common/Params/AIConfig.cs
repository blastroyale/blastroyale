using Photon.Deterministic;
using System;
using System.Collections.Generic;

namespace Quantum
{
  public partial class AIConfig : AssetObject
  {
    public enum EValueType
    {
      None,
      Int,
      Bool,
      Byte,
      FP,
      FPVector2,
      FPVector3,
      String,
      EntityRef,
    }

    [Serializable]
    public class KeyValuePair
    {
      public string Key;
      public EValueType Type;
      public Value Value;
    }

    [Serializable]
    public struct Value
    {
      public Int32 Integer;
      public Boolean Boolean;
      public Byte Byte;
      public FP FP;
      public FPVector2 FPVector2;
      public FPVector3 FPVector3;
      public string String;
      public EntityRef EntityRef;
    }

    public int Count { get { return KeyValuePairs.Count; } }

    public AssetRefAIConfig DefaultConfig;
    public List<KeyValuePair> KeyValuePairs = new List<KeyValuePair>(32);

    public KeyValuePair Get(string key)
    {
      for (int i = 0; i < KeyValuePairs.Count; i++)
      {
        if (KeyValuePairs[i].Key == key)
          return KeyValuePairs[i];
      }

      return null;
    }

    public void Set<T>(string key, T value)
    {
      if (string.IsNullOrEmpty(key) == true)
        return;

      KeyValuePair pair = Get(key);

      if (pair == null)
      {
        pair = new KeyValuePair();
        pair.Key = key;
        KeyValuePairs.Add(pair);
      }

      Set(pair, value);
    }

    private void Set<T>(KeyValuePair pair, T value)
    {
      if (value is int intValue)
      {
        pair.Type = EValueType.Int;
        pair.Value.Integer = intValue;
      }
      else if (value is bool boolValue)
      {
        pair.Type = EValueType.Bool;
        pair.Value.Boolean = boolValue;
      }
      else if (value is Byte byteValue)
      {
        pair.Type = EValueType.Byte;
        pair.Value.Byte = byteValue;
      }
      else if (value is FP fpValue)
      {
        pair.Type = EValueType.FP;
        pair.Value.FP = fpValue;
      }
      else if (value is FPVector2 fpVector2Value)
      {
        pair.Type = EValueType.FPVector2;
        pair.Value.FPVector2 = fpVector2Value;
      }
      else if (value is FPVector3 fpVector3Value)
      {
        pair.Type = EValueType.FPVector3;
        pair.Value.FPVector3 = fpVector3Value;
      }
      else if (value is string stringValue)
      {
        pair.Type = EValueType.String;
        pair.Value.String = stringValue;
      }
      else if (value is EntityRef entityRefValue)
      {
        pair.Type = EValueType.EntityRef;
        pair.Value.EntityRef = entityRefValue;
      } 
      else
      {
        throw new NotSupportedException(string.Format("AIConfig - Type not supported. Type: {0} Key: {1}", typeof(T), pair.Key));
      }
    }
  }
}
