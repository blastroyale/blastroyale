namespace FirstLight.Game.Services.Tutorial
{
	/// <summary>
	/// All steps that are only handled client side to display tutorial steps
	/// </summary>
	public enum TutorialClientStep
	{
		// FIRST GAME
		CreateTutorialRoom = 0,
		WaitSimulationStart = 1,
		Spawn = 2,
		MoveJoystick = 3,
		FirstMove = 4,
		DestroyBarrier = 5,
		PickUpWeapon = 6,
		MoveToDummyArea = 7,
		Kill2Bots = 8,
		Kill1BotSpecial = 9,
		MoveToGateArea = 10,
		MoveToChestArea = 11,
		OpenBox = 12,
		KillFinalBot = 13,
		MatchEnded = 14,
		TutorialFinish = 15, 
		
		// INGAME UI META TUTORIAL
		EnterName = 16,
		BattlePassClick = 17,
		ClickReward = 18,
		ClaimReward = 19,
		GoToEquipment = 20,
		ClickWeaponCategory = 21,
		SelectWeapon = 22,
		EquipWeapon = 23,
		PlayGameClick = 24,
		CreateTutorialMatchRoom = 25,
		SelectMapPoint = 26,
		WaitTutorialMatchStart = 27,
		TutorialMatchFinish = 28
	}
}