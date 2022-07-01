// ReSharper disable CheckNamespace

namespace FirstLight.Statechart.Internal
{
	internal struct InnerStateData
	{
		public IStateInternal InitialState;
		public IStateInternal CurrenState;
		public IStateFactoryInternal NestedFactory;
		public bool ExecuteExit;
		public bool ExecuteFinal;
	}
}