/// ©2024 Kevin Foley. 
/// See accompanying license file.

using UnityEngine;

namespace OneManEscapePlan.Common {
	/// <summary>
	/// String and Reference-type fields with this attribute will be colored red in the inspector if they do not have a value.
	/// Also works for array and List{T} fields.
	/// </summary>
	/// <remarks>
	/// This is similar to <seealso cref="NonNullAttribute"/>, but also supports strings.
	/// </remarks>
	[System.AttributeUsage(System.AttributeTargets.Field)]
	public class RequiredAttribute : PropertyAttribute {
	}
}