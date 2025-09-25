/// ©2021 Kevin Foley. 
/// See accompanying license file.

using System;
using UnityEngine;
using UnityEngine.Events;

namespace OneManEscapePlan.Common.Scripts.DataStructures {

	public enum MassUnit {
		Kilograms,
		Pounds,
		ShortTons,
		LongTons,
		MetricTons
	}

	[System.Serializable] public class MassEvent : UnityEvent<Mass> { }

	[System.Serializable]
	public struct Mass {
		#region CONST
		/// <summary>
		/// Pounds to Kilograms
		/// </summary>
		const float LBS_2_KG = .453592f;
		/// <summary>
		/// Kilograms to pounds
		/// </summary>
		const float KG_2_LBS = 1 / LBS_2_KG;
		/// <summary>
		/// Short tons to kilograms
		/// </summary>
		const float ST_2_KG = 907.185f;
		/// <summary>
		/// Kilograms to short tons
		/// </summary>
		const float KG_2_ST = 1 / ST_2_KG;

		/// <summary>
		/// Long tons to kilograms
		/// </summary>
		const float LT_2_KG = 1016.05f;
		/// <summary>
		/// Kilograms to long tons
		/// </summary>
		const float KG_2_LT = 1 / LT_2_KG;

		/// <summary>
		/// Metric tons to kilograms
		/// </summary>
		const float MT_2_KG = 1000f;
		/// <summary>
		/// Kilograms to metric tons
		/// </summary>
		const float KG_2_MT = 1 / MT_2_KG;
		#endregion

		#region STATIC
		public static Mass FromGrams(float grams) {
			return new Mass(grams / 1000f);
		}
		
		public static Mass FromKilograms(float kg) {
			return new Mass(kg);
		}
		
		public static Mass FromPounds(float pounds) {
			return new Mass(pounds * LBS_2_KG);
		}

		public static Mass FromShortTons(float tons) {
			return new Mass(tons * ST_2_KG);
		}
		
		public static Mass FromLongTons(float tons) {
			return new Mass(tons * LT_2_KG);
		}
		
		public static Mass FromMetricTons(float tons) {
			return new Mass(tons * MT_2_KG);
		}

		public static Mass Zero => new Mass(0);

		public static Mass Lerp(Mass a, Mass b, float t) {
			return new Mass(Mathf.Lerp(a.kilograms, b.kilograms, t));
		}

		public static Mass Clamp(Mass value, Mass min, Mass max) {
			if (min > max) throw new ArgumentException("min should not exceed max (values were " + min + " and " + max + " respectively)");

			if (value < min) return min;
			if (value > max) return max;
			return value;
		}

		public static Mass MaxValue => new Mass(float.MaxValue);
		public static Mass MinValue => new Mass(float.MinValue);
		#endregion

		[SerializeField] private float kilograms;

		public Mass(MassUnit unit, float value) {
			if (unit == MassUnit.Kilograms) this.kilograms = value;
			else if (unit == MassUnit.Pounds) this.kilograms = value * LBS_2_KG;
			else if (unit == MassUnit.ShortTons) this.kilograms = value * ST_2_KG;
			else if (unit == MassUnit.LongTons) this.kilograms = value * LT_2_KG;
			else if (unit == MassUnit.MetricTons) this.kilograms = value * MT_2_KG;
			else throw new Exception("Unrecognized Mass unit " + unit);
		}

		private Mass(float kilograms) {
			this.kilograms = kilograms;
		}

		#region PROPERTIES

		public float Kilograms => kilograms;
		public float Pounds => kilograms * KG_2_LBS;
		public float ShortTons => kilograms * KG_2_ST;
		public float LongTons => kilograms * KG_2_LT;
		public float MetricTons => kilograms * KG_2_MT;

		public bool IsZero => kilograms == 0;
		public bool IsPositive => kilograms > 0;
		public bool IsNegative => kilograms < 0;
		#endregion

		#region METHODS
		public float GetValue(MassUnit unit) {
			if (unit == MassUnit.Kilograms) return kilograms;
			if (unit == MassUnit.Pounds) return Pounds;
			if (unit == MassUnit.ShortTons) return ShortTons;
			if (unit == MassUnit.LongTons) return LongTons;
			if (unit == MassUnit.MetricTons) return MetricTons;
			throw new Exception("Unrecognized Mass unit " + unit);
		}

		override public string ToString() {
			return kilograms + " " + MassUnit.Kilograms.ToShortString();
		}

		public string ToString(string format) {
			return kilograms.ToString(format) + " " + MassUnit.Kilograms.ToShortString();
		}

		public string ToString(MassUnit unit) {
			return GetValue(unit) + " " + unit.ToShortString();
		}

		public string ToString(MassUnit unit, string format) {
			return GetValue(unit).ToString(format) + " " + unit.ToShortString();
			throw new Exception("Unrecognized Mass unit " + unit);
		}
		#endregion

		#region EQUALITY/HASHCODE
		public override bool Equals(object obj) {
			return obj is Mass Mass &&
				   kilograms == Mass.kilograms;
		}

		public override int GetHashCode() {
			return -1108920976 + kilograms.GetHashCode();
		}
		#endregion

		#region OPERATORS
		public static Mass operator +(Mass a, Mass b) {
			return new Mass(a.kilograms + b.kilograms);
		}
		
		public static Mass operator -(Mass a, Mass b) {
			return new Mass(a.kilograms - b.kilograms);
		}

		public static Mass operator -(Mass a) {
			return new Mass(-a.kilograms);
		}

		//note we don't have an operator for multiplying Mass by Mass
		//because that would give us meters² over seconds²

		public static Mass operator *(Mass a, float b) {
			return new Mass(a.kilograms * b);
		}

		public static Mass operator /(Mass a, float b) {
			return new Mass(a.kilograms / b);
		}

		/// <summary>
		/// Dividing one Mass by another returns a unitless ratio, because the units cancel out
		/// (e.g. 100 kg / 50 kg = 2)
		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <returns></returns>
		public static float operator /(Mass a, Mass b) {
			return a.kilograms / b.kilograms;
		}

		public static bool operator >(Mass a, Mass b) {
			return a.kilograms > b.kilograms;
		}
		
		public static bool operator <(Mass a, Mass b) {
			return a.kilograms < b.kilograms;
		}
		
		public static bool operator >=(Mass a, Mass b) {
			return a.kilograms >= b.kilograms;
		}
		
		public static bool operator <=(Mass a, Mass b) {
			return a.kilograms <= b.kilograms;
		}
		
		public static bool operator ==(Mass a, Mass b) {
			return a.kilograms == b.kilograms;
		}
		
		public static bool operator !=(Mass a, Mass b) {
			return a.kilograms != b.kilograms;
		}
		#endregion
	}

	public static class MassUnitExtensionMethods {
		public static string ToLongString(this MassUnit unit) {
			if (unit == MassUnit.Kilograms) return "grams";
			if (unit == MassUnit.Pounds) return "pounds";
			if (unit == MassUnit.ShortTons) return "short tons";
			if (unit == MassUnit.LongTons) return "long tons";
			if (unit == MassUnit.MetricTons) return "metric tons";
			throw new Exception("Unrecognized Mass unit " + unit);
		}
		
		public static string ToShortString(this MassUnit unit) {
			if (unit == MassUnit.Kilograms) return "kg";
			if (unit == MassUnit.Pounds) return "lbs";
			if (unit == MassUnit.ShortTons) return "t";
			if (unit == MassUnit.LongTons) return "t";
			if (unit == MassUnit.MetricTons) return "t";
			throw new Exception("Unrecognized Mass unit " + unit);
		}
	}
}
