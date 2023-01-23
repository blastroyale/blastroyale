using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FirstLight.Game.Services
{
	/// <summary>
	/// This service provides calls to tutorial related methods, UI, and requesting tutorial status
	/// </summary>
	public interface ITutorialService
	{
		/// <summary>
		/// Requests check if a tutorial is currently in progress
		/// </summary>
		IObservableFieldReader<bool> IsTutorialRunning { get; }
	}

	/// <inheritdoc cref="ITutorialService"/>
	public interface IInternalTutorialService : ITutorialService
	{
		/// <inheritdoc cref="ITutorialService.IsTutorialRunning" />
		new IObservableField<bool> IsTutorialRunning { get; }
	}

	/// <inheritdoc cref="ITutorialService"/>
	public class TutorialService : IInternalTutorialService
	{
		private readonly IGameServices _services;
		
		public IObservableField<bool> IsTutorialRunning { get; }
		
		IObservableFieldReader<bool> ITutorialService.IsTutorialRunning => IsTutorialRunning;
		
		public TutorialService()
		{
			IsTutorialRunning = new ObservableField<bool>(false);
		}
	}
}