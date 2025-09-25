/// ©2021 Kevin Foley. 
/// See accompanying license file.

using System;
using UnityEngine;
using UnityEngine.Events;

namespace OneManEscapePlan.Common.Scripts.DataStructures {

	public enum DistanceUnit {
		Meters,
		Kilometers,
		Feet,
		Yards,
		Miles
	}

	[System.Serializable] public class DistanceEvent : UnityEvent<Distance> { }

	[System.Serializable]
	public struct Distance {
		#region CONST
		/// <summary>
		/// Kilometers to meters
		/// </summary>
		const float KM2M = 1000;
		/// <summary>
		/// Meters to kilometers
		/// </summary>
		const float M2KM = 1 / KM2M;
		/// <summary>
		/// Feet to meters
		/// </summary>
		const float F2M = .3048f;
		/// <summary>
		/// Meters to feet
		/// </summary>
		const float M2F = 1 / F2M;
		/// <summary>
		/// Miles to meters
		/// </summary>
		const float MI2M = 1609.34f;
		/// <summary>
		/// Meters to miles
		/// </summary>
		const float M2MI = 1 / MI2M;
		/// <summary>
		/// Yards to meters
		/// </summary>
		const float Y2M = 0.9144f;
		/// <summary>
		/// Meters to yards
		/// </summary>
		const float M2Y = 1 / Y2M;
		#endregion

		#region STATIC
		public static Distance FromMeters(float meters) {
			return new Distance(meters);
		}

		/// <summary>
		/// Create a Distance from a value in kilometers
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		public static Distance FromKilometers(float value) {
			return new Distance(DistanceUnit.Kilometers, value);
		}

		/// <summary>
		/// Create a Distance from a value in miles
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		public static Distance FromMiles(float value) {
			return new Distance(DistanceUnit.Miles, value);
		}

		/// <summary>
		/// Create a Distance from a value in feet
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		public static Distance FromFeet(float value) {
			return new Distance(DistanceUnit.Feet, value);
		}

		/// <summary>
		/// Create a Distance from a value in yards
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		public static Distance FromYards(float value) {
			return new Distance(DistanceUnit.Yards, value);
		}

		public static Distance Zero => new Distance(0);

		public static Distance Lerp(Distance a, Distance b, float t) {
			return new Distance(Mathf.Lerp(a.meters, b.meters, t));
		}

		public static Distance Clamp(Distance value, Distance min, Distance max) {
			if (min > max) throw new ArgumentException("min should not exceed max (values were " + min + " and " + max + " respectively)");

			if (value < min) return min;
			if (value > max) return max;
			return value;
		}

		public static Distance MaxValue => new Distance(float.MaxValue);
		public static Distance MinValue => new Distance(float.MinValue);
		#endregion

		[SerializeField] private float meters;

		/// <summary>
		/// Create a new Distance representing the given distance in the given unit
		/// </summary>
		/// <param name="unit"></param>
		/// <param name="value"></param>
		public Distance(DistanceUnit unit, float value) {
			if (unit == DistanceUnit.Meters) this.meters = value;
			else if (unit == DistanceUnit.Kilometers) this.meters = value * KM2M;
			else if (unit == DistanceUnit.Miles) this.meters = value * MI2M;
			else if (unit == DistanceUnit.Feet) this.meters = value * F2M;
			else if (unit == DistanceUnit.Yards) this.meters = value * Y2M;
			else throw new Exception("Unrecognized Distance unit " + unit);
		}

		private Distance(float meters) {
			this.meters = meters;
		}

		#region PROPERTIES

		public float Meters => meters;
		public float Kilometers => meters * M2KM;
		public float Feet => meters * M2F;
		public float Yards => meters * M2Y;
		public float Miles => meters * M2MI;

		public bool IsZero => meters == 0;
		public bool IsPositive => meters > 0;
		public bool IsNegative => meters < 0;
		#endregion

		#region METHODS
		public float GetValue(DistanceUnit unit) {
			if (unit == DistanceUnit.Meters) return meters;
			if (unit == DistanceUnit.Kilometers) return Kilometers;
			if (unit == DistanceUnit.Feet) return Feet;
			if (unit == DistanceUnit.Yards) return Yards;
			if (unit == DistanceUnit.Miles) return Miles;
			throw new Exception("Unrecognized Distance unit " + unit);
		}

		override public string ToString() {
			return meters + " " + DistanceUnit.Meters.ToShortString();
		}
		
		public string ToString(string format) {
			return meters.ToString(format) + " " + DistanceUnit.Meters.ToShortString();
		}

		public string ToString(DistanceUnit unit) {
			return GetValue(unit) + " " + unit.ToShortString();
		}
		
		public string ToString(DistanceUnit unit, string format) {
			return GetValue(unit).ToString(format) + " " + unit.ToShortString();
			throw new Exception("Unrecognized Distance unit " + unit);
		}
		#endregion

		#region EQUALITY/HASHCODE
		public override bool Equals(object obj) {
			return obj is Distance Distance &&
				   meters == Distance.meters;
		}

		public override int GetHashCode() {
			return -1514495301 + meters.GetHashCode();
		}

		#endregion

		#region OPERATORS
		public static Distance operator +(Distance a, Distance b) {
			return new Distance(a.meters + b.meters);
		}
		
		public static Distance operator -(Distance a, Distance b) {
			return new Distance(a.meters - b.meters);
		}

		//note we don't have an operator for multiplying Distance by Distance
		//because that would give us area and I don't want to create another struct for area

		public static Distance operator *(Distance a, float b) {
			return new Distance(a.meters * b);
		}

		public static Distance operator /(Distance a, float b) {
			return new Distance(a.meters / b);
		}

		/// <summary>
		/// Dividing one Distance by another returns a unitless ratio, because the units cancel out
		/// (e.g. 100 kph / 50kph = 2)
		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <returns></returns>
		public static float operator /(Distance a, Distance b) {
			return a.meters / b.meters;
		}

		/// <summary>
		/// Dividing a distance by a time gives us a speed
		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <returns></returns>
		public static Speed operator /(Distance a, TimeSpanF b) {
			return Speed.FromMetersPerSecond(a.meters / b.TotalSeconds);
		}

		/// <summary>
		/// Dividing a distance by a speed gives us a time
		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <returns></returns>
		public static TimeSpanF operator /(Distance a, Speed b) {
			return TimeSpan.FromSeconds(a.meters / b.MetersPerSecond);
		}


		public static bool operator >(Distance a, Distance b) {
			return a.meters > b.meters;
		}
		
		public static bool operator <(Distance a, Distance b) {
			return a.meters < b.meters;
		}
		
		public static bool operator >=(Distance a, Distance b) {
			return a.meters >= b.meters;
		}
		
		public static bool operator <=(Distance a, Distance b) {
			return a.meters <= b.meters;
		}
		
		public static bool operator ==(Distance a, Distance b) {
			return a.meters == b.meters;
		}
		
		public static bool operator !=(Distance a, Distance b) {
			return a.meters != b.meters;
		}
		#endregion
	}

	public static class DistanceUnitExtensionMethods {
		public static string ToLongString(this DistanceUnit unit) {
			if (unit == DistanceUnit.Meters) return "meters";
			if (unit == DistanceUnit.Kilometers) return "kilometers";
			if (unit == DistanceUnit.Feet) return "feet";
			if (unit == DistanceUnit.Yards) return "yards";
			if (unit == DistanceUnit.Miles) return "miles";
			throw new Exception("Unrecognized Distance unit " + unit);
		}
		
		public static string ToShortString(this DistanceUnit unit) {
			if (unit == DistanceUnit.Meters) return "m";
			if (unit == DistanceUnit.Kilometers) return "km";
			if (unit == DistanceUnit.Feet) return "ft";
			if (unit == DistanceUnit.Yards) return "yd";
			if (unit == DistanceUnit.Miles) return "mi";
			throw new Exception("Unrecognized Distance unit " + unit);
		}
	}
}
