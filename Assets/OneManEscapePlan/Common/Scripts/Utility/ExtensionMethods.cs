/// ©2018 - 2024 Kevin Foley.
/// See accompanying license file.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Events;

namespace OneManEscapePlan.Common {
	/// <summary>
	/// Unity extension methods. These add new functions to existing classes.
	/// See https://unity3d.com/learn/tutorials/topics/scripting/extension-methods
	/// </summary>
	/// COMPLEXITY: Advanced
	/// CONCEPTS: Extension methods, Generics, Pooling, UnityEvents
	public static class ExtensionMethods {

		#region COLLECTIONS
		public static T GetRandom<T>(this IReadOnlyList<T> list) {
			if (list.Count == 0) return default(T);
			int index = UnityEngine.Random.Range(0, list.Count);
			return list[index];
		}

		public static T GetRandom<T>(this T[] array) {
			if (array.Length == 0) return default(T);
			int index = UnityEngine.Random.Range(0, array.Length);
			return array[index];
		}

		public static int GetRandomIndex<T>(this IReadOnlyList<T> list) {
			if (list.Count == 0) return -1;
			return UnityEngine.Random.Range(0, list.Count);
		}

		public static int GetRandomIndex<T>(this T[] array) {
			if (array.Length == 0) return -1;
			return UnityEngine.Random.Range(0, array.Length);
		}

		// Use the version in System.Collections instead.
		//public static V GetValueOrDefault<K, V>(this IDictionary<K, V> dict, K key) {
		//	V value = default(V);
		//	dict.TryGetValue(key, out value);
		//	return value;
		//}

		/// <summary>
		/// Whether the list contains duplicates (ignoring null entries)
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="list"></param>
		/// <returns></returns>
		public static bool ContainsDuplicates<T>(this IReadOnlyList<T> list) {
			for (int i = 0; i < list.Count - 1; i++) {
				T item = list[i];
				if (item == null) continue;
				for (int j = i + 1; j < list.Count; j++) {
					if (item.Equals(list[j])) return true;
				}
			}
			return false;
		}
		#endregion

		#region GAMEOBJECTS
		/// <summary>
		/// Check if we are facing the given position, within the specified tolerance.
		/// </summary>
		/// <param name="target"></param>
		/// <param name="tolerance">Maximum angle in degrees that is considered valid</param>
		/// <returns>Whether the target position is less than <c>tolerance</c> degrees straight in front of us</returns>
		public static bool IsFacing(this Transform source, Vector3 target, float tolerance) {
			Vector3 targetDir = target - source.position;
			float angle = Vector3.Angle(targetDir, source.forward);
			return angle < tolerance;
		}

		/// <summary>
		/// Rotate the given number of degrees towards the given position.
		/// </summary>
		/// <param name="target">A position in world coordinates</param>
		/// <param name="degrees">We will rotate up to this many degrees towards the target</param>
		/// <param name="minDistance">If we are closer than this distance, don't rotate</param>
		public static void RotateTowards(this Transform transform, Vector3 target, float degrees, float minDistance = .15f) {
			if (Vector3.Distance(transform.position, target) > minDistance) {
				//calculate the angle that would be looking directly at the target
				Quaternion targetRotation = Quaternion.LookRotation(target - transform.position);
				//rotate towards the target angle
				transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, degrees);
			}
		}

		/// <summary>
		/// Rotate the given number of degrees towards the given position.
		/// </summary>
		/// <param name="target">A position in world coordinates</param>
		/// <param name="degrees">We will rotate up to this many degrees towards the target</param>
		/// <param name="minDistance">If we are closer than this distance, don't rotate</param>
		public static void RotateTowards(this Rigidbody rigidbody, Vector3 target, float degrees, float minDistance = .15f) {
			if (Vector3.Distance(rigidbody.position, target) > minDistance) {
				// Calculate the angle that would be looking directly at the target
				Quaternion targetRotation = Quaternion.LookRotation(target - rigidbody.position);
				// Rotate towards the target angle.
				rigidbody.rotation = Quaternion.RotateTowards(rigidbody.rotation, targetRotation, degrees);
			}
		}

		/// <summary>
		/// Get the given component type; it will be added if not already present on this GameObject.
		/// </summary>
		public static T GetOrAddComponent<T>(this GameObject go) where T : Component {
			T component = go.GetComponent<T>();
			if (component == null) component = go.AddComponent<T>();
			return component;
		}

		/// <summary>
		/// Get the given component type; it will be added if not already present on this GameObject.
		/// </summary>
		public static T GetOrAddComponent<T>(this Component co) where T : Component {
			return GetOrAddComponent<T>(co.gameObject);
		}

		/// <summary>
		/// If the given reference is null, try to find it on this GameObject or its children. If
		/// the component is not found, add it to a child with the given <c>containerName</c>. If
		/// the container does not exist, it will be created.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="go"></param>
		/// <param name="component"></param>
		/// <param name="containerName"></param>
		public static void FindOrAddChildComponent<T>(this GameObject go, ref T component, string containerName) where T : Component {
			Assert.IsNotNull(go);
			Assert.IsFalse(string.IsNullOrWhiteSpace(containerName));

			go.transform.FindComponentInChildren(ref component);

			if (component == null) {
				Transform container = go.transform.Find(containerName);
				if (container == null) {
					GameObject containerGO = new GameObject(containerName);
					container = containerGO.transform;
					container.SetParent(go.transform, false);
				}

				component = container.gameObject.AddComponent<T>();
			}
		}

		/// <summary>
		/// If the given reference is null, try to find it on this GameObject or its children. If
		/// the component is not found, add it to a child with the given <c>containerName</c>. If
		/// the container does not exist, it will be created.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="co"></param>
		/// <param name="component"></param>
		/// <param name="containerName"></param>
		public static void FindOrAddChildComponent<T>(this Component co, ref T component, string containerName) where T : Component {
			Assert.IsNotNull(co);
			Assert.IsFalse(string.IsNullOrWhiteSpace(containerName));

			co.transform.FindComponentInChildren(ref component);

			if (component == null) {
				Transform container = co.transform.Find(containerName);
				if (container == null) {
					GameObject containerGO = new GameObject(containerName);
					container = containerGO.transform;
					container.SetParent(co.transform, false);
				}

				component = container.gameObject.AddComponent<T>();
			}
		}

		/// <summary>
		/// If the given reference is null, try to find it on this GameObject.
		/// </summary>
		/// <typeparam name="T">Type of component to look for</typeparam>
		/// <param name="co"></param>
		/// <param name="reference">A reference variable (most likely a class field) we want to populate</param>
		/// <returns><c>true</c> if the reference was already set or is now set, <c>false</c> otherwise</returns>
		public static bool FindComponent<T>(this Component co, ref T reference) where T : Component {
			if (reference == null) reference = co.GetComponent<T>();
			return (reference != null);
		}

		/// <summary>
		/// If the given reference is null, try to find it on this GameObject or its children.
		/// </summary>
		/// <typeparam name="T">Type of component to look for</typeparam>
		/// <param name="co"></param>
		/// <param name="reference">A reference variable (most likely a class field) we want to populate</param>
		/// <returns><c>true</c> if the reference was already set or is now set, <c>false</c> otherwise</returns>
		public static bool FindComponentInChildren<T>(this Component co, ref T reference) where T : Component {
			if (reference == null) reference = co.GetComponentInChildren<T>();
			return (reference != null);
		}

		/// <summary>
		/// If the given reference is null, try to find it on this GameObject or its parent
		/// </summary>
		/// <typeparam name="T">Type of component to look for</typeparam>
		/// <param name="co"></param>
		/// <param name="reference">A reference variable (most likely a class field) we want to populate</param>
		/// <returns><c>true</c> if the reference was already set or is now set, <c>false</c> otherwise</returns>
		public static bool FindComponentInParent<T>(this Component co, ref T reference) where T : Component {
			if (reference == null) reference = co.GetComponentInParent<T>();
			return (reference != null);
		}
		#endregion

		public static bool Contains(this LayerMask mask, int layer) {
			return mask == (mask | 1 << layer);
		}
	}
}