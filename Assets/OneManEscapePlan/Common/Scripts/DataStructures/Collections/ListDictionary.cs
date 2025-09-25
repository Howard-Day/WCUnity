/// ©2021 Kevin Foley. 
/// See accompanying license file.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace OneManEscapePlan.Common.Scripts.DataStructures {

	/// <summary>
	/// Wraps a dictionary of Lists.
	/// </summary>
	/// <typeparam name="K"></typeparam>
	/// <typeparam name="V"></typeparam>
	public class ListDictionary<K, V> : IEnumerable<KeyValuePair<K, List<V>>>, IEnumerable {
		#region FIELDS
		protected Dictionary<K, List<V>> dict;
		private int listInitialCapacity;
		#endregion

		#region PROPERTIES
		public int ListInitialCapacity { get => listInitialCapacity; set => listInitialCapacity = value; }
		/// <summary>
		/// The number of lists in the collection
		/// </summary>
		public int ListCount => dict.Count;
		/// <summary>
		/// The total number of values across the lists in the collection
		/// </summary>
		public int ValueCount {
			get {
				int count = 0;
				foreach (var value in dict.Values) {
					count += value.Count;
				}
				return count;
			}
		}
		public Dictionary<K, List<V>>.KeyCollection Keys => dict.Keys;
		#endregion

		public ListDictionary(int dictionaryInitialCapacity = 0, int listInitialCapacity = 1) {
			if (dictionaryInitialCapacity < 0) throw new System.ArgumentOutOfRangeException(nameof(dictionaryInitialCapacity));
			if (listInitialCapacity < 0) throw new System.ArgumentOutOfRangeException(nameof(listInitialCapacity));
			dict = new Dictionary<K, List<V>>(dictionaryInitialCapacity);
			this.listInitialCapacity = listInitialCapacity;
		}

		/// <summary>
		/// Return the list corresponding to the given key if it exists, or null if
		/// no items have been added for this key.
		/// </summary>
		/// <param name="key"></param>
		/// <returns></returns>
		virtual public List<V> GetListOrNull(K key) {
			dict.TryGetValue(key, out var list);
			return list;
		}

		virtual public List<V> GetOrCreateList(K key) {
			if (!dict.TryGetValue(key, out var list)) {
				list = new List<V>(listInitialCapacity);
				dict[key] = list;
			}
			return list;
		}

		/// <summary>
		/// Add a new item to the list corresponding to the given key, and return the list.
		/// </summary>
		/// <param name="key">Key of the list</param>
		/// <param name="value">Value to add to the list</param>
		/// <returns>The updated list</returns>
		virtual public List<V> Add(K key, V value) {
			var list = GetOrCreateList(key);
			list.Add(value);
			return list;
		}

		/// <summary>
		/// Add new items to the list corresponding to the given key, and return the list.
		/// </summary>
		/// <param name="key">Key of the list</param>
		/// <param name="value">Value to add to the list</param>
		/// <returns>The updated list</returns>
		virtual public List<V> AddRange(K key, IReadOnlyCollection<V> values) {
			var list = GetOrCreateList(key);
			list.AddRange(values);
			return list;
		}

		/// <summary>
		/// Remove the given value from the list at the given key
		/// </summary>
		/// <param name="key"></param>
		/// <param name="value"></param>
		/// <returns></returns>
		virtual public bool RemoveValue(K key, V value) {
			var list = GetOrCreateList(key);
			return list.Remove(value);
		}

		/// <summary>
		/// Remove the list with the given key, if it exists
		/// </summary>
		/// <param name="key"></param>
		/// <returns></returns>
		virtual public bool RemoveList(K key) {
			return dict.Remove(key);
		}

		/// <summary>
		/// Clear the list with the given key, if it exists
		/// </summary>
		/// <param name="key"></param>
		/// <returns><c>true</c> if the list exists and was cleared, <c>false</c> otherwise</returns>
		virtual public bool ClearList(K key) {
			if (dict.TryGetValue(key, out var list)) {
				list.Clear();
				return true;
			}
			return false;
		}

		virtual public void ClearAll() {
			dict.Clear();
		}

		public IEnumerator<KeyValuePair<K, List<V>>> GetEnumerator() {
			return (dict as IEnumerable<KeyValuePair<K, List<V>>>).GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator() {
			return dict.GetEnumerator();
		}
	}
}
