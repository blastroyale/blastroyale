using System;

namespace Quantum
{
  public unsafe partial struct BlackboardValue
  {
    public String ValueToString()
    {
      switch (Field)
      {
        case BlackboardValue.BOOLEANVALUE: return string.Format("{0}", _BooleanValue);
        case BlackboardValue.BYTEVALUE: return string.Format("{0}",_ByteValue);
        case BlackboardValue.INTEGERVALUE: return string.Format("{0}", _IntegerValue);
        case BlackboardValue.FPVALUE: return string.Format("{0}", _FPValue);
        case BlackboardValue.FPVECTOR2VALUE: return string.Format("{0}", _FPVector2Value);
        case BlackboardValue.FPVECTOR3VALUE: return string.Format("{0}", _FPVector3Value);
        case BlackboardValue.ENTITYREFVALUE: return string.Format("{0}", _EntityRefValue);
      }

      return base.ToString();
    }
  }

  public unsafe partial struct BlackboardValue
  {
    public String TypeToString()
    {
      switch (Field)
      {
        case BlackboardValue.BOOLEANVALUE: return "Boolean";
        case BlackboardValue.BYTEVALUE: return "Byte";
        case BlackboardValue.INTEGERVALUE: return "Integer";
        case BlackboardValue.FPVALUE: return "FP";
        case BlackboardValue.FPVECTOR2VALUE: return "Vector2";
        case BlackboardValue.FPVECTOR3VALUE: return "Vector3";
        case BlackboardValue.ENTITYREFVALUE: return "EntityRef";
      }

      return base.ToString();
    }
  }
}
