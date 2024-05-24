namespace Quantum
{
	public partial struct PlayerInventory
	{
		public bool TryAddSpecial(Frame f, EntityRef playerEntity, PlayerRef player, in Special special)
		{
			for (var i = 0; i < Specials.Length; i++)
			{
				if (Specials[i].IsValid)
				{
					continue;
				}

				Specials[i] = special;
				f.Events.OnPlayerSpecialUpdated(player, playerEntity, (uint)i, special);
				return true;
			}

			return false;
		}

		public bool HasAnySpecial()
		{
			for (var i = 0; i < Specials.Length; i++)
			{
				if (Specials[i].IsValid)
				{
					return true;
				}
			}

			return false;
		}

		public bool HasSpaceForSpecial()
		{
			for (var i = 0; i < Specials.Length; i++)
			{
				if (!Specials[i].IsValid)
				{
					return true;
				}
			}

			return false;
		}
	}
}