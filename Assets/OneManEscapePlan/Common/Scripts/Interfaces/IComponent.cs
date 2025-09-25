using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace OneManEscapePlan.Common.Scripts {
	public interface IComponent { 
		GameObject gameObject { get; }
		Transform transform { get; }
		bool enabled { get; set; }
		T GetComponent<T>();
		T GetComponentInParent<T>();
		T GetComponentInChildren<T>();
		T[] GetComponentsInParent<T>();
		T[] GetComponentsInChildren<T>();
	}
}
