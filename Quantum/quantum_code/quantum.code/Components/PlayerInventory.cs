namespace Quantum
{
	public unsafe partial struct PlayerInventory
	{
		public bool TryAddSpecial(Frame f, PlayerRef player, Special special)
		{
			for (var i = 0; i < Specials.Length; i++)
			{
				if (Specials[i].IsValid)
				{
					continue;
				}

				Specials[i] = special;
				f.Events.OnLocalPlayerSpecialUpdated(player, (uint) i, special);
				return true;
			}

			return false;
		}
	}
}