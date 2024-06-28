using System;
using System.Globalization;
using Sirenix.OdinInspector;
using UnityEngine;

namespace FirstLight.Game.Configs.Utils
{
	[Serializable]
	public struct TransformParams
	{
		public Vector3 Position;
		public Vector3 Rotation;
		public Vector3 Scale;
	}

	[Serializable]
	public struct AnimatorParams
	{
		[Required] public bool ApplyRootMotion;
		[Required] public AnimatorUpdateMode UpdateMode;
		[Required] public AnimatorCullingMode CullingMode;
		[Required] public RuntimeAnimatorController Controller;
	}

	[Serializable]
	public class DurationConfig
	{
		public const string DATE_FORMAT = "dd/MM/yyyy HH:mm";

		public static DateTime Parse(string datetime)
		{
			return DateTime.ParseExact(datetime, DATE_FORMAT, CultureInfo.InvariantCulture);
		}

		[ValidateInput("IsValidDate", "Invalid date format! Use: '" + DATE_FORMAT + "'")]
		public string StartsAt;

		[ValidateInput("IsValidDate", "Invalid date format! Use: '" + DATE_FORMAT + "'")]
		public string EndsAt;

		public static bool IsValidDate(string input)
		{
			return DateTime.TryParseExact(input, DATE_FORMAT, CultureInfo.InvariantCulture, DateTimeStyles.None, out _);
		}

		public DateTime GetStartsAtDateTime() => Parse(StartsAt);
		public DateTime GetEndsAtDateTime() => Parse(EndsAt);

		public bool Contains(DateTime dateTime)
		{
			return GetStartsAtDateTime() <= dateTime && GetEndsAtDateTime() > dateTime;
		}
	}
}