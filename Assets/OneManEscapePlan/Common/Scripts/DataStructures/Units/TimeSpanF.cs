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
	/// Serializable float-based TimeSpan. Base unit is in seconds, not intended for millisecond accuracy. 
	/// If millisecond accuracy is needed, use <see cref="System.TimeSpan"/>.
	/// </summary>
	[System.Serializable]
	public struct TimeSpanF {
		#region CONST
		/// <summary>
		/// Hours to minutes
		/// </summary>
		const float H2M = 60;
		/// <summary>
		/// Minutes to seconds
		/// </summary>
		const float M2S = 60;
		/// <summary>
		/// Seconds to minutes
		/// </summary>
		const float S2M = 1 / M2S;
		/// <summary>
		/// Hours to seconds
		/// </summary>
		const float H2S = H2M * M2S;
		/// <summary>
		/// Seconds to hours
		/// </summary>
		const float S2H = 1 / H2S;
		#endregion

		#region STATIC
		public static TimeSpanF FromSeconds(float s) {
			return new TimeSpanF(s);
		}

		public static TimeSpanF FromMinutes(float m) {
			return new TimeSpanF(m * M2S);
		}
		
		public static TimeSpanF FromHours(float h) {
			return new TimeSpanF(h * H2S);
		}

		public static TimeSpanF Lerp(TimeSpanF a, TimeSpanF b, float t) {
			float s = Mathf.Lerp(a.seconds, b.seconds, t);
			return new TimeSpanF(s);
		}

		public static TimeSpanF Clamp(TimeSpanF value, TimeSpanF min, TimeSpanF max) {
			if (min > max) throw new ArgumentException("min should not exceed max (values were " + min + " and " + max + " respectively)");

			if (value < min) return min;
			if (value > max) return max;
			return value;
		}
		#endregion

		[SerializeField] private float seconds;

		public TimeSpanF(float seconds) {
			this.seconds = seconds;
		}

		public TimeSpanF(int hours, int minutes, float seconds) {
			this.seconds = H2S * hours + M2S * minutes + seconds;
		}

		#region METHODS
		override public string ToString() {
			float partialSeconds = seconds % 1;
			if (partialSeconds == 0) return string.Format("{0:00}:{1:00}:{2:00}", Hours, Minutes, Seconds);
			else return string.Format("{0:00}:{1:00}:{2:00}{3:.00}", Hours, Minutes, Seconds, partialSeconds);
		}
		#endregion

		#region PROPERTIES

		public float Seconds => (seconds % 60);
		public int Minutes => (int)((seconds * S2M) % 60);
		public int Hours => (int)(seconds * S2H);

		public float TotalSeconds => seconds;
		public float TotalMinutes => seconds / M2S;
		public float TotalHours => seconds / H2S;

		public bool IsZero => seconds == 0;
		public bool IsPositive => seconds > 0;
		public bool IsNegative => seconds < 0;
		#endregion

		#region EQUALITY/HASHCODE
		public override bool Equals(object obj) {
			return obj is TimeSpanF timeSpan &&
				   seconds == timeSpan.seconds;
		}

		public override int GetHashCode() {
			return 1448586362 + seconds.GetHashCode();
		}
		#endregion

		#region OPERATORS
		public static TimeSpanF operator +(TimeSpanF a, TimeSpanF b) {
			return new TimeSpanF(a.seconds + b.seconds);
		}

		public static TimeSpanF operator -(TimeSpanF a, TimeSpanF b) {
			return new TimeSpanF(a.seconds - b.seconds);
		}

		//note we don't have an operator for multiplying time by time
		//because that would give us seconds²

		public static TimeSpanF operator *(TimeSpanF a, float b) {
			return new TimeSpanF(a.seconds * b);
		}

		public static TimeSpanF operator /(TimeSpanF a, float b) {
			return new TimeSpanF(a.seconds / b);
		}

		/// <summary>
		/// Dividing one time by another returns a unitless ratio, because the units cancel out
		/// (e.g. 60s / 10s = 6)
		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <returns></returns>
		public static float operator /(TimeSpanF a, TimeSpanF b) {
			return a.seconds / b.seconds;
		}

		public static bool operator >(TimeSpanF a, TimeSpanF b) {
			return a.seconds > b.seconds;
		}

		public static bool operator <(TimeSpanF a, TimeSpanF b) {
			return a.seconds < b.seconds;
		}

		public static bool operator >=(TimeSpanF a, TimeSpanF b) {
			return a.seconds >= b.seconds;
		}

		public static bool operator <=(TimeSpanF a, TimeSpanF b) {
			return a.seconds <= b.seconds;
		}

		public static bool operator ==(TimeSpanF a, TimeSpanF b) {
			return a.seconds == b.seconds;
		}

		public static bool operator !=(TimeSpanF a, TimeSpanF b) {
			return a.seconds != b.seconds;
		}

		/// <summary>
		/// We can implictly convert back and forth between System.TimeSpan and TimeSpanF. There is technically loss of
		/// precision when converting from System.TimeSpan to TimeSpanF, and if we were really going by-the-book we wouldn't
		/// allow implicit conversions that can cost us precision, but Unity does everything in floats so who cares.
		/// </summary>
		/// <param name="tsf"></param>
		public static implicit operator TimeSpan(TimeSpanF tsf) => TimeSpan.FromSeconds(tsf.seconds);
		public static implicit operator TimeSpanF(TimeSpan ts) => TimeSpanF.FromSeconds((float)ts.TotalSeconds);
		#endregion
	}
}
