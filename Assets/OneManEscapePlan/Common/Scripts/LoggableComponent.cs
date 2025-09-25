/// ©2022 Kevin Foley.
/// See accompanying license file.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace OneManEscapePlan.Common.Scripts {
	public class LoggableComponent : MonoBehaviour {
		#region FIELDS
		[SerializeField] private bool debugLogging = false;
		#endregion

		#region PROPERTIES
		virtual protected bool IsLoggingEnabled => Debug.isDebugBuild && debugLogging;
		virtual public string DebugName => $"[{GetType().Name}] {name}:";
		#endregion

		virtual protected void Log(string value) {
			Debug.Log($"{DebugName} {value}", this);
		}

		virtual protected void Log(object value) {
			Debug.Log($"{DebugName} {value}", this);
		}

		virtual protected void Log(params object[] list) {
			Debug.Log($"{DebugName} {string.Join(", ", list)}", this);
		}
	}
}