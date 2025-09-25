/// ©2022 Kevin Foley.
/// See accompanying license file.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Events;

namespace OneManEscapePlan.Common.Scripts.UI {
	/// <summary>
	/// Use for tracking a single selection in a list
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public class SelectionController<T> {
		[System.Serializable] public class ItemEvent : UnityEvent<T> { }

		#region FIELDS
		protected List<T> list;
		protected ItemEvent selectionChangedEvent = new ItemEvent();

		protected int selectedIndex = -1;
		#endregion

		#region PROPERTIES
		virtual public int SelectedIndex {
			get => selectedIndex;
			set {
				if (list == null) throw new System.Exception($"Tried to set current index to {value}, but underlying list is null");
				if (value < 0 || value >= list.Count) throw new System.IndexOutOfRangeException($"Invalid index {value} (range 0 - {list.Count - 1})");
				selectedIndex = value;
				selectionChangedEvent.Invoke(SelectedItem);
			}
		}

		virtual public T SelectedItem {
			get {
				if (list == null || selectedIndex < 0) return default;
				return list[selectedIndex];
			}
			set {
				if (list == null) throw new System.Exception("Tried to select item in null list");
				int index = list.IndexOf(value);
				if (index < 0) throw new System.Exception($"Tried to select item {value} which is not present in the list");
				SelectedIndex = index;
			}
		}
		#endregion

		public SelectionController(List<T> list) {
			Assert.IsNotNull(list);
			this.list = list;
		}

		public SelectionController(IReadOnlyCollection<T> collection) {
			Assert.IsNotNull(collection);
			list = new List<T>(collection);
		}

		virtual public void SelectNext() {
			if (list == null) return;
			int newIndex = selectedIndex + 1;
			if (newIndex > list.Count - 1) newIndex = 0;
			SelectedIndex = newIndex;
		}	
		
		virtual public void SelectPrevious() {
			if (list == null) return;
			int newIndex = selectedIndex - 1;
			if (newIndex < 0) newIndex = list.Count - 1;
			SelectedIndex = newIndex;
		}
	}
}
