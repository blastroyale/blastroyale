using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FirstLight.UIService;
using QuickEye.UIToolkit;
using UnityEngine.UIElements;

namespace FirstLight.Modules.UIService.Runtime
{
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
	public sealed class QViewAttribute : Attribute
	{
		public string Name { get; set; }
		public bool Existing { get; set; }

		public QViewAttribute(string name, bool existing = false)
		{
			Name = name;
			Existing = existing;
		}
	}

	public static class QViewUtils
	{
		public static void AssignElementResults(this VisualElement root, object target)
		{
			foreach (var (member, att) in target.GetType().GetQAttributeMembers())
			{
				Action<object, object> setMemberValue;
				var fieldName = member.Name;
				if (member is FieldInfo field)
					(_, setMemberValue) = (field.FieldType, field.SetValue);
				else if (member is PropertyInfo property)
					(_, setMemberValue) = (property.PropertyType, property.SetValue);
				else continue;

				if (fieldName.StartsWith("_"))
				{
					fieldName = char.ToUpperInvariant(fieldName[1]) + fieldName[2..];
				}
				

				var queryResult = string.IsNullOrEmpty(att.Name) && att.Classes == null
					? root.Q(fieldName)
					: root.Q(att.Name, att.Classes);
				if (queryResult == null)
				{
					throw new Exception("Couldn't find element with name " + fieldName);
				}
				setMemberValue(target, queryResult);
			}
		}

		public static void AssignQueryViews(this VisualElement root, UIPresenter presenter, object target)
		{
			foreach (var (member, att) in target.GetType().GetQViewAttributeMembers())
			{
				Type returnType;
				Action<object, object> setMemberValue;
				Func<object, object> getMemberValue;

				if (member is FieldInfo field)
					(returnType, setMemberValue, getMemberValue) = (field.FieldType, field.SetValue, field.GetValue);
				else if (member is PropertyInfo property)
					(returnType, setMemberValue, getMemberValue) = (property.PropertyType, property.SetValue, property.GetValue);
				else continue;

				var queryResult = root.Q(att.Name);
				object instance;
				if (att.Existing)
				{
					instance = getMemberValue(target);
				}
				else
				{
					instance = Activator.CreateInstance(returnType);
					setMemberValue(target, instance);
				}

				queryResult.AttachExistingView(presenter, (UIView) instance);
			}
		}

		private const BindingFlags QAttributeTargetFlags = BindingFlags.FlattenHierarchy
			| BindingFlags.Instance
			| BindingFlags.Static
			| BindingFlags.Public
			| BindingFlags.NonPublic;

		public static (MemberInfo, QViewAttribute)[] GetQViewAttributeMembers(this Type type)
		{
			return (from member in GetFieldsAndProperties()
				let att = member.GetCustomAttribute<QViewAttribute>()
				where att != null
				select (member, att)).ToArray();

			IEnumerable<MemberInfo> GetFieldsAndProperties()
			{
				foreach (var field in type.GetFields(QAttributeTargetFlags))
					yield return field;
				foreach (var prop in type.GetProperties(QAttributeTargetFlags))
					yield return prop;
			}
		}

		public static (MemberInfo, QAttribute)[] GetQAttributeMembers(this Type type)
		{
			return (from member in GetFieldsAndProperties()
				let att = member.GetCustomAttribute<QAttribute>()
				where att != null
				select (member, att)).ToArray();

			IEnumerable<MemberInfo> GetFieldsAndProperties()
			{
				foreach (var field in type.GetFields(QAttributeTargetFlags))
					yield return field;
				foreach (var prop in type.GetProperties(QAttributeTargetFlags))
					yield return prop;
			}
		}
	}
}