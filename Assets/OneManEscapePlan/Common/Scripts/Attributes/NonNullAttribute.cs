/// ©2021 Kevin Foley. 
/// See accompanying license file.

using UnityEngine;

namespace OneManEscapePlan.Common {
	/// <summary>
	/// Reference-type fields with this attribute will be colored red in the inspector if they do not have a value.
	/// Also works for array and List{T} fields.
	/// </summary>
	[System.AttributeUsage(System.AttributeTargets.Field)]
	public class NonNullAttribute : PropertyAttribute {
	}
}