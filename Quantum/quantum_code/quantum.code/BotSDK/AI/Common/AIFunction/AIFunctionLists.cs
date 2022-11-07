using Photon.Deterministic;
using Quantum.Collections;

namespace Quantum
{
	public unsafe abstract class AIFunctionAssetRefList : AIFunction<QList<AssetRef>> { }
	public unsafe abstract class AIFunctionBoolList : AIFunction<QList<bool>> { }
	public unsafe abstract class AIFunctionByteList : AIFunction<QList<byte>> { }
	public unsafe abstract class AIFunctionEntityRefList : AIFunction<QList<EntityRef>> { }
	public unsafe abstract class AIFunctionFPList : AIFunction<QList<FP>> { }
	public unsafe abstract class AIFunctionFPVector2List : AIFunction<QList<FPVector2>> { }
	public unsafe abstract class AIFunctionFPVector3List : AIFunction<QList<FPVector3>> { }
	public unsafe abstract class AIFunctionIntegerList : AIFunction<QList<int>> { }
}
