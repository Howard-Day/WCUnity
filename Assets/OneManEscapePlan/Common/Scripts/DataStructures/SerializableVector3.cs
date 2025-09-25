/// ©2021 Kevin Foley. 
/// See accompanying license file.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace OneManEscapePlan.Common.Scripts.DataStructures {
	/// <summary>
	/// A serializable structure that represents the same data as a Vector3, with implicit conversion
	/// to and from Vector3.
	/// </summary>
	/// <remarks>
	/// Unity's Vector3 is already serializable. I probably made this struct for a specific reason,
	/// but I can't remember why...
	/// </remarks>
	[System.Serializable]
	public struct SerializableVector3 {
		[SerializeField] private float _x;
		[SerializeField] private float _y;
		[SerializeField] private float _z;

		public SerializableVector3(float x, float y) {
			_x = x;
			_y = y;
			_z = 0;
		}

		public SerializableVector3(float x, float y, float z) {
			_x = x;
			_y = y;
			_z = z;
		}

		public SerializableVector3(Vector3 value) {
			_x = value.x;
			_y = value.y;
			_z = value.z;
		}

		public float x { get => _x; set => _x = value; }
		public float y { get => _y; set => _y = value; }
		public float z { get => _z; set => _z = value; }

		/// <summary>
		/// Returns the length of this vector
		/// </summary>
		public float magnitude => Mathf.Sqrt(_x * _x + _y * _y + _z * _z);

		public override string ToString() {
			return string.Format("({0:F1}, {1:F1}, {2:F1}", _x, _y, _z);
		}

		public string ToString(string format) {
			return string.Format("({0}, {1}, {2}", _x.ToString(format), _y.ToString(format), _z.ToString(format));
		}

		public Vector3 ToVector3() {
			return new Vector3(_x, _y, _z);
		}

		public static implicit operator Vector3(SerializableVector3 sv3) => sv3.ToVector3();
		public static implicit operator SerializableVector3(Vector3 v3) => new SerializableVector3(v3);
	}
}
