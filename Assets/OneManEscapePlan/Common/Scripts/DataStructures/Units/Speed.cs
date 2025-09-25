/// ©2021 Kevin Foley. 
/// See accompanying license file.

using System;
using UnityEngine;
using UnityEngine.Events;

namespace OneManEscapePlan.Common.Scripts.DataStructures {

	public enum SpeedUnit {
		MetersPerSecond,
		KilometersPerHour,
		MilesPerHour
	}

	[System.Serializable] public class SpeedEvent : UnityEvent<Speed> { }

	[System.Serializable]
	public struct Speed {
		#region CONST
		const float MPS_TO_KPH = 3.6f;
		const float KPH_TO_MPS = 1 / MPS_TO_KPH;
		const float MPS_TO_MPH = 2.23694f;
		const float MPH_TO_MPS = 1 / MPS_TO_MPH;
		#endregion

		#region STATIC
		public static Speed FromMetersPerSecond(float mps) {
			return new Speed(mps);
		}
		
		/// <summary>
		/// Create a Speed from a value in kilometers per hour
		/// </summary>
		/// <param name="kph"></param>
		/// <returns></returns>
		public static Speed FromKPH(float kph) {
			return new Speed(kph * KPH_TO_MPS);
		}

		/// <summary>
		/// Create a Speed from a value in miles per hour
		/// </summary>
		/// <param name="mph"></param>
		/// <returns></returns>
		public static Speed FromMPH(float mph) {
			return new Speed(mph * MPH_TO_MPS);
		}

		public static Speed Zero => new Speed(0);

		public static Speed Lerp(Speed a, Speed b, float t) {
			return new Speed(Mathf.Lerp(a.metersPerSecond, b.metersPerSecond, t));
		}

		public static Speed MoveTowards(Speed a, Speed b, Speed maxDelta) {
			if (a == b) return b;

			if (a < b) {
				a += maxDelta;
				if (a > b) return b;
				return a;
			} else { //a > b
				a -= maxDelta;
				if (a < b) return b;
				return a;
			}
		}

		public static Speed Clamp(Speed value, Speed min, Speed max) {
			if (min > max) throw new ArgumentException("min should not exceed max (values were " + min + " and " + max + " respectively)");

			if (value < min) return min;
			if (value > max) return max;
			return value;
		}

		public static Speed MaxValue => new Speed(float.MaxValue);
		public static Speed MinValue => new Speed(float.MinValue);
		#endregion

		[SerializeField] private float metersPerSecond;

		public Speed(SpeedUnit unit, float value) {
			if (unit == SpeedUnit.MetersPerSecond) this.metersPerSecond = value;
			else if (unit == SpeedUnit.KilometersPerHour) this.metersPerSecond = value * KPH_TO_MPS;
			else if (unit == SpeedUnit.MilesPerHour) this.metersPerSecond = value * MPH_TO_MPS;
			else throw new Exception("Unrecognized speed unit " + unit);
		}

		private Speed(float metersPerSecond) {
			this.metersPerSecond = metersPerSecond;
		}

		#region PROPERTIES

		//not abbreviated because the standard abbreviation ("m/s") contains a slash, "ms" looks like "milliseconds",
		//and nobody every writes it as "mps"
		public float MetersPerSecond => metersPerSecond;
		/// <summary>
		/// Kilometers per hour
		/// </summary>
		public float KPH => metersPerSecond * MPS_TO_KPH;
		/// <summary>
		/// Miles per hour
		/// </summary>
		public float MPH => metersPerSecond * MPS_TO_MPH;

		public bool IsZero => metersPerSecond == 0;
		public bool IsPositive => metersPerSecond > 0;
		public bool IsNegative => metersPerSecond < 0;
		#endregion

		#region METHODS
		public float GetValue(SpeedUnit unit) {
			if (unit == SpeedUnit.MetersPerSecond) return metersPerSecond;
			if (unit == SpeedUnit.KilometersPerHour) return KPH;
			if (unit == SpeedUnit.MilesPerHour) return MPH;
			throw new Exception("Unrecognized speed unit " + unit);
		}

		override public string ToString() {
			return metersPerSecond + " " + SpeedUnit.MetersPerSecond.ToShortString();
		}

		public string ToString(string format) {
			return metersPerSecond.ToString(format) + " " + SpeedUnit.MetersPerSecond.ToShortString();
		}

		public string ToString(SpeedUnit unit) {
			return GetValue(unit) + " " + unit.ToShortString();
		}

		public string ToString(SpeedUnit unit, string format) {
			return GetValue(unit).ToString(format) + " " + unit.ToShortString();
			throw new Exception("Unrecognized speed unit " + unit);
		}
		#endregion

		#region EQUALITY/HASHCODE
		public override bool Equals(object obj) {
			return obj is Speed speed &&
				   metersPerSecond == speed.metersPerSecond;
		}

		public override int GetHashCode() {
			return 1958294206 + metersPerSecond.GetHashCode();
		}
		#endregion

		#region OPERATORS
		public static Speed operator +(Speed a, Speed b) {
			return new Speed(a.metersPerSecond + b.metersPerSecond);
		}
		
		public static Speed operator -(Speed a, Speed b) {
			return new Speed(a.metersPerSecond - b.metersPerSecond);
		}

		public static Speed operator -(Speed a) {
			return new Speed(-a.metersPerSecond);
		}

		//note we don't have an operator for multiplying speed by speed
		//because that would give us meters² over seconds²

		public static Speed operator *(Speed a, float b) {
			return new Speed(a.metersPerSecond * b);
		}

		public static Speed operator /(Speed a, float b) {
			return new Speed(a.metersPerSecond / b);
		}

		/// <summary>
		/// Dividing one speed by another returns a unitless ratio, because the units cancel out
		/// (e.g. 100 kph / 50kph = 2)
		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <returns></returns>
		public static float operator /(Speed a, Speed b) {
			return a.metersPerSecond / b.metersPerSecond;
		}

		/// <summary>
		/// Dividing a speed by a time gives us an acceleration, e.g. (50 m/s) / (5s) = 10 m/s²
		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <returns></returns>
		public static Acceleration operator/(Speed a, TimeSpanF b) {
			return Acceleration.FromMetersPerSecondSquared(a.metersPerSecond / b.TotalSeconds);
		}

		/// <summary>
		/// Dividing a speed by an acceleration gives us a time, e.g. (50m/s) / (10m/s²) = 5s
		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <returns></returns>
		public static TimeSpanF operator/(Speed a, Acceleration b) {
			return TimeSpanF.FromSeconds(a.metersPerSecond / b.MetersPerSecondSquared);
		}

		/// <summary>
		/// Multiplying a speed by a time gives us a distance
		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <returns></returns>
		public static Distance operator*(Speed a, TimeSpanF b) {
			return Distance.FromMeters(a.metersPerSecond * b.TotalSeconds);
		}

		/// <summary>
		/// Multiplying a speed by a time gives us a distance
		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <returns></returns>
		public static Distance operator *(TimeSpanF a, Speed b) {
			return Distance.FromMeters(a.TotalSeconds * b.MetersPerSecond);
		}

		public static bool operator >(Speed a, Speed b) {
			return a.metersPerSecond > b.metersPerSecond;
		}
		
		public static bool operator <(Speed a, Speed b) {
			return a.metersPerSecond < b.metersPerSecond;
		}
		
		public static bool operator >=(Speed a, Speed b) {
			return a.metersPerSecond >= b.metersPerSecond;
		}
		
		public static bool operator <=(Speed a, Speed b) {
			return a.metersPerSecond <= b.metersPerSecond;
		}
		
		public static bool operator ==(Speed a, Speed b) {
			return a.metersPerSecond == b.metersPerSecond;
		}
		
		public static bool operator !=(Speed a, Speed b) {
			return a.metersPerSecond != b.metersPerSecond;
		}
		#endregion
	}

	public static class SpeedUnitExtensionMethods {
		public static string ToLongString(this SpeedUnit unit) {
			if (unit == SpeedUnit.KilometersPerHour) return "kilometers/hour";
			if (unit == SpeedUnit.MetersPerSecond) return "meters/second";
			if (unit == SpeedUnit.MilesPerHour) return "miles/hour";
			throw new Exception("Unrecognized speed unit " + unit);
		}
		
		public static string ToShortString(this SpeedUnit unit) {
			if (unit == SpeedUnit.KilometersPerHour) return "KPH";
			if (unit == SpeedUnit.MetersPerSecond) return "m/s";
			if (unit == SpeedUnit.MilesPerHour) return "MPH";
			throw new Exception("Unrecognized speed unit " + unit);
		}
	}
}
