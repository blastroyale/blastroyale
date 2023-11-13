namespace FirstLight.Game.Services.Tutorial
{
	/// <summary>
	/// All steps that are only handled client side to display tutorial steps
	/// </summary>
	public enum TutorialClientStep
	{
		// FIRST GAME
		CreateTutorialRoom,
		WaitSimulationStart,
		Spawn,
		MoveJoystick,
		FirstMove,
		DestroyBarrier,
		PickUpWeapon,
		MoveToDummyArea,
		Kill2Bots,
		PickupSpecial,
		Kill1BotSpecial,
		MoveToGateArea,
		MoveToChestArea,
		OpenBox,
		KillFinalBot,
		MatchEnded,
		TutorialFinish,

		// INGAME UI META TUTORIAL
		EnterName,
		BattlePassClick,
		ClickReward,
		ClaimReward,
		GoToEquipment,
		ClickWeaponCategory,
		SelectWeapon,
		EquipWeapon,
		PlayGameClick,
		CreateTutorialMatchRoom,
		SelectMapPoint,
		WaitTutorialMatchStart,
		TutorialMatchFinish
	}
}