using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace OneManEscapePlan.Common.Scripts.DataStructures {

	/// <summary>
	/// Immutable struct representing an angle, in degrees.
	/// </summary>
	[System.Serializable]
	public struct Angle {
		#region STATIC
		public static Angle FromRadians(float rads) {
			return new Angle(rads * Mathf.Rad2Deg);
		}
		public static Angle North => new Angle(0);
		public static Angle East => new Angle(90);
		public static Angle South => new Angle(180);
		public static Angle West => new Angle(270);
		#endregion

		[SerializeField] private float rawDegrees;

		public Angle(float degrees = 0) {
			this.rawDegrees = degrees;
		}

		#region PROPERTIES
		/// <summary>
		/// The original value that was passed into the constructor
		/// </summary>
		public float RawDegrees => rawDegrees;

		/// <summary>
		/// The original value that was passed into the constructor, converted to radians
		/// </summary>
		public float RawRadians => rawDegrees * Mathf.Deg2Rad;

		/// <summary>
		/// Get this angle represented in the range -180° to 180°
		/// </summary>
		public float Degrees180 {
			get {
				var degrees = rawDegrees % 360;
				if (degrees < -180) degrees += 360;
				else if (degrees > 180) degrees -= 360;
				return degrees;
			}
		}

		/// <summary>
		/// Get this angle represented in the range 0° - 360°
		/// </summary>
		public float Degrees360 {
			get {
				var degrees = rawDegrees % 360;
				if (degrees < 0) degrees += 360;
				return degrees;
			}
		}
		#endregion

		#region OPERATORS
		public static bool operator==(Angle a, Angle b) {
			return Mathf.Approximately(a.Degrees360, b.Degrees360);
		}
		public static bool operator!=(Angle a, Angle b) {
			return !Mathf.Approximately(a.Degrees360, b.Degrees360);
		}
		public static Angle operator +(Angle a, Angle b) {
			return new Angle(a.rawDegrees + b.rawDegrees);
		}
		public static Angle operator -(Angle a, Angle b) {
			return new Angle(a.rawDegrees - b.rawDegrees);
		}
		public static Angle operator -(Angle a) {
			return new Angle(-a.rawDegrees);
		}
		#endregion

		/// <summary>
		/// Treats positive rotation as clockwise. Returns false if this angle == b.
		/// </summary>
		/// <param name="b"></param>
		/// <returns></returns>
		public bool IsClockwiseFrom(Angle b) {
			var delta = Mathf.DeltaAngle(b.rawDegrees, this.rawDegrees);
			return (delta > 0);
		}

		/// <summary>
		/// Treats negative rotation as counter-clockwise. Returns false if this angle == b.
		/// </summary>
		/// <param name="b"></param>
		/// <returns></returns>
		public bool IsCounterClockwiseFrom(Angle b) {
			var delta = Mathf.DeltaAngle(b.rawDegrees, this.rawDegrees);
			return (delta < 0);
		}

		public override string ToString() {
			return $"{Degrees360:F1}°";
		}

		public string ToString(string format = "F1") {
			return Degrees360.ToString(format);
		}

		public string ToString180(string format = "F1") {
			return $"{Degrees180.ToString(format)}°";
		}
		
		public string ToString360(string format = "F1") {
			return $"{Degrees360.ToString(format)}°";
		}

		public override bool Equals(object obj) {
			return obj is Angle angle &&
				   Mathf.Approximately(Degrees360, angle.Degrees360);
		}

		public override int GetHashCode() {
			return HashCode.Combine(Degrees360);
		}
	}
}
