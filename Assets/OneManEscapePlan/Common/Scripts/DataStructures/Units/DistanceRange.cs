/// ©2021 Kevin Foley. 
/// See accompanying license file.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace OneManEscapePlan.Common.Scripts.DataStructures {

	public interface IReadOnlyDistanceRange {
		Distance Min { get; }
		Distance Max { get; }

		Distance Clamp(Distance value);
		bool Contains(Distance value);
		Distance GetRandom();
		Distance Lerp(float t);
		float Normalize(Distance value, bool clamp = false);
	}

	[System.Serializable]
	public class DistanceRange : IReadOnlyDistanceRange {
		#region STATIC
		public static DistanceRange FromMeters(float min, float max) {
			return new DistanceRange(Distance.FromMeters(min), Distance.FromMeters(max));
		}
		#endregion

		[SerializeField] protected Distance min;
		[SerializeField] protected Distance max;

		public DistanceRange(Distance min, Distance max) {
			this.min = min;
			this.max = max;
		}

		public Distance Min {
			get => min;
		}

		public Distance Max { 
			get => max;
		}

		public void SetRange(Distance min, Distance max) {
			if (max < min) throw new ArgumentException("Max cannot be less than min (provided values: " + min + ", " + max + ")");
			this.min = min;
			this.max = max;
		}

		public Distance Clamp(Distance value) {
			return Distance.Clamp(value, min, max);
		}

		public Distance GetRandom() {
			float meters = UnityEngine.Random.Range(min.Meters, max.Meters);
			return Distance.FromMeters(meters);
		}

		public Distance Lerp(float t) {
			return Distance.Lerp(min, max, t);
		}

		/// <summary>
		/// Convert the given value to a normalized value relative to the range
		/// </summary>
		/// <param name="value"></param>
		/// <param name="clamp">If true, the normalized return value is clamped in range 0 - 1.
		/// If false, the return value may be less than 0 or greater than 1 if the input value is outside of the range.</param>
		/// <returns>A normalized value</returns>
		public float Normalize(Distance value, bool clamp = false) {
			float t = (value - min) / (max - min);
			if (clamp) t = Mathf.Clamp01(t);
			return t;
		}

		public bool Contains(Distance value) {
			return (value >= min && value <= max);
		}

		public override bool Equals(object obj) {
			return obj is DistanceRange range &&
				   min == range.min &&
				   max == range.max;
		}

		public override int GetHashCode() {
			var hashCode = -897720056;
			hashCode = hashCode * -1521134295 + min.GetHashCode();
			hashCode = hashCode * -1521134295 + max.GetHashCode();
			return hashCode;
		}

		public DistanceRange Clone() {
			return new DistanceRange(min, max);
		}

		public override string ToString() {
			return "(" + min + " - " + max + ")";
		}
		
		public string ToString(string format) {
			return "(" + min.ToString(format) + " - " + max.ToString(format) + ")";
		}
	}
}
