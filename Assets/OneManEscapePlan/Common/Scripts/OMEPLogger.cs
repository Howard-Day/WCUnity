/// ©2024 Kevin Foley.
/// See accompanying license file.

using System.Runtime.CompilerServices;
using UnityEngine;

/// <summary>
/// Logs messages with a custom format, while retaining the behavior that 
/// double-clicking the message in the Unity console will take you to the
/// line of code you want to see, rather than to this class.
/// </summary>
public static class OMEPLogger {
	
	private static string Format(object context, object message, string args, string caller) {
		if (context == null) {
			return $"{caller}({args}) {message}";
		} else {
			return $"[{context.GetType()}] {caller}({args}) {message}";
		}
	}

	public static void Log(Object context, string message, string args = null, [CallerMemberName]string caller = null) {
		Debug.Log(Format(context, message, args, caller), context);
	}
	
	public static void Log(object context, string message, string args = null, [CallerMemberName]string caller = null) {
		Debug.Log(Format(context, message, args, caller));
	}
	
	public static void Log(Object context, object value, string args = null, [CallerMemberName]string caller = null) {
		Debug.Log(Format(context, value, args, caller), context);
	}
	
	public static void Log(object context, object value, string args = null, [CallerMemberName]string caller = null) {
		Debug.Log(Format(context, value, args, caller));
	}

	public static void LogWarning(Object context, string message, string args = null, [CallerMemberName] string caller = null) {
		Debug.Log(Format(context, message, args, caller), context);
	}
	
	public static void LogWarning(object context, string message, string args = null, [CallerMemberName] string caller = null) {
		Debug.Log(Format(context, message, args, caller));
	}
	
	public static void LogError(Object context, string message, string args = null, [CallerMemberName] string caller = null) {
		Debug.LogError(Format(context, message, args, caller), context);
	}
	
	public static void LogError(object context, string message, string args = null, [CallerMemberName] string caller = null) {
		Debug.LogError(Format(context, message, args, caller));
	}
}