/// ©2023 Kevin Foley

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Assertions;
using UnityEngine.Serialization;

namespace OneManEscapePlan.Common.Scripts.DataStructures.Collections {
	abstract public class ListAsset<T> : ScriptableObject, IReadOnlyList<T> {
		#region FIELDS
		[SerializeField] private List<T> list;

		public T this[int index] => list[index];

		public int Count => list.Count;

		public IEnumerator<T> GetEnumerator() {
			return ((IEnumerable<T>)list).GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator() {
			return ((IEnumerable)list).GetEnumerator();
		}
		#endregion
	}
}