using Quantum;
using UnityEngine;

namespace FirstLight.Game.MonoComponent.Match
{
	/// <summary>
	/// This interface defines the contract for a player's 3D world attack indicator
	/// </summary>
	public interface IIndicator
	{
		/// <summary>
		/// Requests the visual state of the indicator.
		/// Call <seealso cref="SetVisualState"/> to change it
		/// </summary>
		bool VisualState { get; }
		
		/// <summary>
		/// Requests the <see cref="IndicatorVfxId"/> representing this indicator type
		/// </summary>
		IndicatorVfxId IndicatorVfxId { get; }
		
		/// <summary>
		/// Set's the visual state of the indicator based on the given parameter values.
		/// </summary>
		void SetVisualState(bool isVisible, bool isEmphasized = false);

		/// <summary>
		/// Set's the indicator visual properties to allow to update them on a loop if necessary
		/// </summary>
		void SetVisualProperties(float size, float minRange, float maxRange);
		
		/// <summary>
		/// Repositions the indicator to the given HUD stick <paramref name="position"/>
		/// </summary>
		void SetTransformState(Vector2 position);
		
		/// <summary>
		/// Initializes this indicator view with the given data
		/// </summary>
		void Init(EntityView playerEntityView);
	}
}