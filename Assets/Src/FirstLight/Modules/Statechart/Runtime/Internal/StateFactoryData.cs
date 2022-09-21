using System;

// ReSharper disable CheckNamespace

namespace FirstLight.Statechart.Internal
{
	internal struct StateFactoryData
	{
		public Action<IStatechartEvent> StateChartMoveNextCall;
		public IStatechart Statechart;
	}
}