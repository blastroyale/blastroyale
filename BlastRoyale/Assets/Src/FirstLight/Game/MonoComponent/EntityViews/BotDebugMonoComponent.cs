using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Cysharp.Threading.Tasks;
using FirstLight.Game.Presenters;
using FirstLight.Game.UIElements;
using FirstLight.Game.Utils;
using Quantum;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace FirstLight.Game.MonoComponent.EntityViews
{
	public class BotDebugMonoComponent : MonoBehaviour
	{
		private EntityView _view;
		private EntityRef _entityRef => _view.EntityRef;
		private VisualElement _element;
		private Label _decisionDelay;
		private Label _waypointStatus;

		[Serializable]
		public class BotDebugInfo
		{
			public float Time;
			public string Action;
		}

		public List<BotDebugInfo> Infos = new ();

		public LabelOutlined CreateText(Color color, int size)
		{
			var temp = new LabelOutlined("0.5s");
			temp.style.color = color;
			temp.style.fontSize = size;
			temp.style.unityTextOutlineColor = Color.black;
			temp.style.unityTextOutlineWidth = 1;
			return temp;
		}

		private void Awake()
		{
			_element = new VisualElement();
			_element.style.position = Position.Absolute;
			_element.style.unityTextAlign = TextAnchor.MiddleLeft;

			_decisionDelay = CreateText(Color.magenta, 22);
			_decisionDelay.style.left = -50;

			_waypointStatus = CreateText(Color.cyan, 22);
			_waypointStatus.style.top = -100;

			WorldDebugPresenter.Instance.DebugElement(this.transform, _element);
			WorldDebugPresenter.Instance.DebugElement(this.transform, _decisionDelay);
			WorldDebugPresenter.Instance.DebugElement(this.transform, _waypointStatus);

			_view = GetComponent<EntityView>();
			QuantumEvent.Subscribe<EventBotDebugInfo>(this, OnBotDebugInfoReceived);
			QuantumCallback.Subscribe<CallbackUpdateView>(this, HandleUpdateView);
		}

		private void DrawPath(Frame f, ref NavMeshPathfinder meshPathfinder)
		{
			for (int i = 0; i < meshPathfinder.WaypointCount; i++)
			{
				var way = meshPathfinder.GetWaypoint(f, i);
			}
		}

		private CancellationTokenSource _cancellationTokenSource;

		/*private void SetAiming()
		{
			if (_cancellationTokenSource != null)
			{
				_cancellationTokenSource.Cancel();
			}

			_cancellationTokenSource = new CancellationTokenSource();
			_movementType.text = "AIMING";
			UniTask.Void(async (cc) =>
			{
				await UniTask.WaitForSeconds(0.2f, cancellationToken: cc);
				_movementType.text = "";
			}, _cancellationTokenSource.Token);
		}*/

		private unsafe void HandleUpdateView(CallbackUpdateView callback)
		{
			var f = callback.Game.Frames.Verified;
			if (f.Exists(_entityRef))
			{
				if (f.TryGet<BotCharacter>(_entityRef, out var botCharacter))
				{
					var a = botCharacter.NextDecisionTime - f.Time;
					_decisionDelay.text = a.AsFloat.ToString("F") + "s";
					var pathFinder = f.Get<NavMeshPathfinder>(_entityRef);
					var gameId = "";
					if (f.Unsafe.TryGetPointer<Collectable>(botCharacter.MoveTarget, out var collectable) && botCharacter.MovementType == BotMovementType.GoToCollectable)
					{
						gameId = " for " + collectable->GameId.ToString();
					}

					_waypointStatus.text = $"<color=green>Loves {botCharacter.FavoriteWeapon.ToString()}</color>\n<color=orange>{botCharacter.MovementType.ToString()}{gameId}</color>\nInternalTarget {pathFinder.InternalTarget}\n Count:{pathFinder.WaypointCount} Current:{pathFinder.WaypointIndex}";
				}
			}
		}

		private void OnBotDebugInfoReceived(EventBotDebugInfo callback)
		{
			if (callback.Bot != _entityRef)
			{
				return;
			}

			if (callback.Action == "aiming")
			{
				return;
			}

			var text = new LabelOutlined(callback.Action);
			text.style.color = Color.white;
			text.style.fontSize = 22;
			text.style.unityTextOutlineColor = Color.black;
			text.style.unityTextOutlineWidth = 4;
			text.style.transitionDuration = new List<TimeValue>()
			{
				new TimeValue(0.2f, TimeUnit.Second)
			};
			text.style.transitionProperty = new List<StylePropertyName>()
			{
				new ("translate")
			};
			if (callback.TraceLevel == (byte) TraceLevel.Error)
			{
				text.style.color = Color.red;
			}

			if (callback.TraceLevel == (byte) TraceLevel.Warning)
			{
				text.style.color = Color.yellow;
			}

			_element.Add(text);
			text.AnimatePing(1.05f);
			UniTask.Void(async () =>
			{
				await UniTask.WaitForSeconds(1f);
				text.AnimatePingOpacity(1, 0, 500);
				await UniTask.WaitForSeconds(0.5f);
				_element.Remove(text);
			});
			Infos.Add(new BotDebugInfo()
			{
				Time = callback.Game.Frames.Verified.Time.AsFloat,
				Action = callback.Action,
			});
		}
	}
}