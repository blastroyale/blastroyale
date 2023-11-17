namespace Quantum
{
	public partial struct PlayerInventory
	{
		public bool TryAddSpecial(Frame f, EntityRef playerEntity, PlayerRef player, Special special)
		{
			for (var i = 0; i < Specials.Length; i++)
			{
				if (Specials[i].IsValid)
				{
					continue;
				}

				Specials[i] = special;
				f.Events.OnLocalPlayerSpecialUpdated(player, playerEntity, (uint) i, special);
				return true;
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