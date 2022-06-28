namespace Quantum
{
	public unsafe partial struct AirDrop
	{
		/// <summary>
		/// Initializes this <see cref="AirDrop"/> with values from <see cref="QuantumShrinkingCircleConfig"/>
		/// </summary>
		public static EntityRef Create(Frame f, QuantumShrinkingCircleConfig config)
		{
			var entity = f.Create(f.FindAsset<EntityPrototype>(f.AssetConfigs.AirDropPrototype.Id));

			f.Add<Chest>(entity);

			f.Add(entity, new AirDrop
			{
				Delay = f.RNG->NextInclusive(config.AirdropStartTimeRange.Value1, config.AirdropStartTimeRange.Value2),
				Duration = config.AirdropDropDuration,
				Stage = AirDropStage.Waiting,
				Chest = config.AirdropChest
			});

			return entity;
		}
	}
}