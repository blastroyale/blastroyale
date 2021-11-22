using FirstLight.Services;

namespace FirstLight.Game.Logic
{
	/// <summary>
	/// Abstract basic signature for any Logic that is part of the <see cref="IGameLogic"/>
	/// </summary>
	public abstract class AbstractBaseLogic<TData> where TData : class
	{
		protected readonly IGameLogic GameLogic;
		protected readonly IDataProvider DataProvider;

		protected TData Data => DataProvider.GetData<TData>();
		
		private AbstractBaseLogic() {}

		public AbstractBaseLogic(IGameLogic gameLogic, IDataProvider dataProvider)
		{
			GameLogic = gameLogic;
			DataProvider = dataProvider;
		}
	}
}