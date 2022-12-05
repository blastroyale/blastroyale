using System.Collections.Generic;
using FirstLight.Game.Data;
using FirstLight.Game.Ids;
using FirstLight.Game.Logic.RPC;
using FirstLight.Services;
using Quantum;

namespace FirstLight.Game.Logic
{
	/// <summary>
	/// This logic provides the necessary behaviour to manage any entity <seealso cref="UniqueId"/> relationship with an <seealso cref="GameId"/>
	/// </summary>
	public interface IUniqueIdDataProvider
	{
		/// <summary>
		/// Requests the <see cref="GameId"/> representation in readonly form of the <see cref="IObservableDictionary"/> data
		/// </summary>
		IObservableDictionaryReader<UniqueId, GameId> Ids { get; }

		/// <summary>
		/// Requests New <see cref="UniqueId"/> Ids that the player has not seen yet.
		/// </summary>
		IObservableList<UniqueId> NewIds { get; }
	}

	/// <inheritdoc />
	public interface IUniqueIdLogic : IUniqueIdDataProvider
	{
		/// <summary>
		/// Generates a new <see cref="UniqueId"/> to be associated with the given <paramref name="gameId"/>
		/// </summary>
		UniqueId GenerateNewUniqueId(GameId gameId);

		/// <summary>
		/// Removes the given <paramref name="id"/> from the game registry
		/// </summary>
		void RemoveId(UniqueId id);

		/// <summary>
		/// Marks an ID as seen, removing it from NewIDs list.
		/// </summary>
		/// <param name="id"></param>
		void MarkIdSeen(UniqueId id);
	}

	/// <inheritdoc cref="IUniqueIdLogic" />
	public class UniqueIdLogic : AbstractBaseLogic<IdData>, IUniqueIdLogic, IGameLogicInitializer
	{
		private IObservableDictionary<UniqueId, GameId> _ids;
		private IObservableList<UniqueId> _newIds;


		/// <inheritdoc />
		public IObservableDictionaryReader<UniqueId, GameId> Ids => _ids;

		/// <inheritdoc />
		public IObservableList<UniqueId> NewIds => _newIds;

		public UniqueIdLogic(IGameLogic gameLogic, IDataProvider dataProvider) : base(gameLogic, dataProvider)
		{
		}

		/// <inheritdoc />
		public void Init()
		{
			_ids = new ObservableDictionary<UniqueId, GameId>(Data.GameIds);
			_newIds = new ObservableList<UniqueId>(Data.NewIds);
		}

		/// <inheritdoc />
		public UniqueId GenerateNewUniqueId(GameId gameId)
		{
			Data.UniqueIdCounter = Data.UniqueIdCounter.Id + 1;

			_ids.Add(Data.UniqueIdCounter, gameId);
			NewIds.Add(Data.UniqueIdCounter);

			return Data.UniqueIdCounter;
		}

		/// <inheritdoc />
		public void RemoveId(UniqueId id)
		{
			if (!_ids.Remove(id))
			{
				throw new LogicException($"The given {id} is not registered in the game logic to be removed");
			}
		}

		public void MarkIdSeen(UniqueId id)
		{
			_newIds.Remove(id);
		}
	}
}
