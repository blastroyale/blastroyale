

using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using Immutable.Passport;
using PlayFab.Public;
using System;
using System.ComponentModel;
using UnityEngine;

public partial class SROptions {

	[Category("Web3")]
	public async void DebugPassportState()
	{
		if(!MainInstaller.TryResolve<IWeb3Service>(out var web3))
		{
			return;
		}
		var imx = (FlgImxWeb3Service)web3;
		try
		{
			Debug.Log($"[Imx] Token: {await imx.Passport.GetAccessToken()}");
		}
		catch (Exception e) 
		{
			Debug.LogError("Error obtaining Token");
			Debug.LogError(e); 
		}
		try
		{
			Debug.Log($"[Imx] Address: {await imx.Passport.GetAddress()}");
		}
		catch (Exception e) 
		{
			Debug.LogError("Error obtaining Address");
			Debug.LogError(e);
		}
		try
		{
			Debug.Log($"[Imx] Email: {await imx.Passport.GetEmail()}");
		}
		catch (Exception e) 
		{
			Debug.LogError("Error obtaining Email");
			Debug.LogError(e); 
		}
		try
		{
			Debug.Log($"[Imx] IdToken: {await imx.Passport.GetIdToken()}");
		}
		catch (Exception e) 
		{
			Debug.LogError("Error obtaining IdToken");
			Debug.LogError(e); 
		}
	}
}