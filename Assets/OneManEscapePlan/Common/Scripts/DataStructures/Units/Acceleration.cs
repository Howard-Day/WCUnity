/// ©2021 Kevin Foley. 
/// See accompanying license file.

using System;
using UnityEngine;
using UnityEngine.Events;

namespace OneManEscapePlan.Common.Scripts.DataStructures {

	public enum AccelerationUnit {
		MetersPerSecondSquared,
		FeetPerSecondSquared
	}

	[System.Serializable] public class AccelerationEvent : UnityEvent<Acceleration> { }

	[System.Serializable]
	public struct Acceleration {
		#region CONST
		/// <summary>
		/// Converts fps² to m/s²
		/// </summary>
		const float FPSS_TO_MPSS = 0.3048f;
		/// <summary>
		/// Converts m/s² to fps²
		/// </summary>
		const float MPSS_TO_FPSS = 1 / FPSS_TO_MPSS;
		#endregion

		#region STATIC
		/// <summary>
		/// Create an Acceleration from a value in meters/s²
		/// </summary>
		/// <param name="mpss"></param>
		/// <returns></returns>
		public static Acceleration FromMetersPerSecondSquared(float mpss) {
			return new Acceleration(mpss);
		}

		/// <summary>
		/// Create an Acceleration from a value in feet/s²
		/// </summary>
		/// <param name="kph"></param>
		/// <returns></returns>
		public static Acceleration FromFeetPerSecondSquared(float fpss) {
			return new Acceleration(fpss * FPSS_TO_MPSS);
		}

		public static Acceleration Zero => new Acceleration(0);

		public static Acceleration Lerp(Acceleration a, Acceleration b, float t) {
			return new Acceleration(Mathf.Lerp(a.metersPerSecSquared, b.metersPerSecSquared, t));
		}

		public static Acceleration Clamp(Acceleration value, Acceleration min, Acceleration max) {
			if (min > max) throw new ArgumentException("min should not exceed max (values were " + min + " and " + max + " respectively)");

			if (value < min) return min;
			if (value > max) return max;
			return value;
		}

		public static Acceleration MaxValue => new Acceleration(float.MaxValue);
		public static Acceleration MinValue => new Acceleration(float.MinValue);
		#endregion

		[SerializeField] private float metersPerSecSquared;

		public Acceleration(AccelerationUnit unit, float value) {
			if (unit == AccelerationUnit.MetersPerSecondSquared) this.metersPerSecSquared = value;
			else if (unit == AccelerationUnit.FeetPerSecondSquared) this.metersPerSecSquared = value * FPSS_TO_MPSS;
			else throw new Exception("Unrecognized Acceleration unit " + unit);
		}

		private Acceleration(float metersPerSecond) {
			this.metersPerSecSquared = metersPerSecond;
		}

		#region PROPERTIES

		/// <summary>
		/// Meters/s²
		/// </summary>
		public float MetersPerSecondSquared => metersPerSecSquared;
		/// <summary>
		/// Feet/s²
		/// </summary>
		public float FeetPerSecondSquared => metersPerSecSquared * MPSS_TO_FPSS;

		/// <summary>
		/// Shorthand for meters/s²
		/// </summary>
		public float MPSS => MetersPerSecondSquared;

		/// <summary>
		/// Shorthand for feet/s²
		/// </summary>
		public float FPSS => FeetPerSecondSquared;

		public bool IsZero => metersPerSecSquared == 0;
		public bool IsPositive => metersPerSecSquared > 0;
		public bool IsNegative => metersPerSecSquared < 0;
		#endregion

		#region METHODS
		public float GetValue(AccelerationUnit unit) {
			if (unit == AccelerationUnit.MetersPerSecondSquared) return metersPerSecSquared;
			if (unit == AccelerationUnit.FeetPerSecondSquared) return FeetPerSecondSquared;
			throw new Exception("Unrecognized Acceleration unit " + unit);
		}

		override public string ToString() {
			return metersPerSecSquared + " " + AccelerationUnit.MetersPerSecondSquared.ToShortString();
		}

		public string ToString(string format) {
			return metersPerSecSquared.ToString(format) + " " + AccelerationUnit.MetersPerSecondSquared.ToShortString();
		}

		public string ToString(AccelerationUnit unit) {
			return GetValue(unit) + " " + unit.ToShortString();
		}

		public string ToString(AccelerationUnit unit, string format) {
			return GetValue(unit).ToString(format) + " " + unit.ToShortString();
			throw new Exception("Unrecognized Distance unit " + unit);
		}
		#endregion

		#region EQUALITY/HASHCODE
		public override bool Equals(object obj) {
			return obj is Acceleration Acceleration &&
				   metersPerSecSquared == Acceleration.metersPerSecSquared;
		}

		public override int GetHashCode() {
			return 756758072 + metersPerSecSquared.GetHashCode();
		}
		#endregion

		#region OPERATORS
		public static Acceleration operator +(Acceleration a, Acceleration b) {
			return new Acceleration(a.metersPerSecSquared + b.metersPerSecSquared);
		}
		
		public static Acceleration operator -(Acceleration a, Acceleration b) {
			return new Acceleration(a.metersPerSecSquared - b.metersPerSecSquared);
		}

		//note we don't have an operator for multiplying Acceleration by Acceleration
		//because that would give us meters^2 over seconds^4

		public static Acceleration operator *(Acceleration a, float b) {
			return new Acceleration(a.metersPerSecSquared * b);
		}

		public static Acceleration operator /(Acceleration a, float b) {
			return new Acceleration(a.metersPerSecSquared / b);
		}

		/// <summary>
		/// Dividing one Acceleration by another returns a unitless ratio, because the units cancel out
		/// e.g. (100 m/s²) / (50m/s²) = 2
		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <returns></returns>
		public static float operator /(Acceleration a, Acceleration b) {
			return a.metersPerSecSquared / b.metersPerSecSquared;
		}

		/// <summary>
		/// Multiplying an acceleration by a time gives us a speed (e.g. 10m/s² * 5s = 50m/s)
		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <returns></returns>
		public static Speed operator *(Acceleration a, TimeSpanF b) {
			return Speed.FromMetersPerSecond(a.metersPerSecSquared * b.TotalSeconds);
		}

		/// <summary>
		/// Multiplying an acceleration by a time gives us a speed (e.g. 10m/s² * 5s = 50m/s)
		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <returns></returns>
		public static Speed operator *(TimeSpanF b, Acceleration a) {
			return Speed.FromMetersPerSecond(a.metersPerSecSquared * b.TotalSeconds);
		}

		public static bool operator >(Acceleration a, Acceleration b) {
			return a.metersPerSecSquared > b.metersPerSecSquared;
		}
		
		public static bool operator <(Acceleration a, Acceleration b) {
			return a.metersPerSecSquared < b.metersPerSecSquared;
		}
		
		public static bool operator >=(Acceleration a, Acceleration b) {
			return a.metersPerSecSquared >= b.metersPerSecSquared;
		}
		
		public static bool operator <=(Acceleration a, Acceleration b) {
			return a.metersPerSecSquared <= b.metersPerSecSquared;
		}
		
		public static bool operator ==(Acceleration a, Acceleration b) {
			return a.metersPerSecSquared == b.metersPerSecSquared;
		}
		
		public static bool operator !=(Acceleration a, Acceleration b) {
			return a.metersPerSecSquared != b.metersPerSecSquared;
		}
		#endregion
	}

	public static class AccelerationUnitExtensionMethods {
		public static string ToLongString(this AccelerationUnit unit) {
			if (unit == AccelerationUnit.MetersPerSecondSquared) return "meters/second²";
			if (unit == AccelerationUnit.FeetPerSecondSquared) return "feet/second²";
			throw new Exception("Unrecognized Acceleration unit " + unit);
		}
		
		public static string ToShortString(this AccelerationUnit unit) {
			if (unit == AccelerationUnit.MetersPerSecondSquared) return "m/s²";
			if (unit == AccelerationUnit.FeetPerSecondSquared) return "fps²";
			throw new Exception("Unrecognized Acceleration unit " + unit);
		}
	}
}
