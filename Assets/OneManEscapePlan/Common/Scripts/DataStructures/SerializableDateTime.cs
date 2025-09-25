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
	/// A serializable structure that represents the same data as a DateTime, with implicit conversion
	/// to and from DateTime.
	/// </summary>
	/// <remarks>
	/// DateTime is a C# struct that is not serializable in Unity.
	/// </remarks>
	[System.Serializable]
	public struct SerializableDateTime {
		[SerializeField] private long ticks;

		public long Ticks => ticks;

		public SerializableDateTime(long ticks) {
			this.ticks = ticks;
		}

		public static implicit operator DateTime(SerializableDateTime date) => new DateTime(date.Ticks);
		public static implicit operator SerializableDateTime(DateTime date) => new SerializableDateTime(date.Ticks);
		public static bool operator ==(SerializableDateTime date1, SerializableDateTime date2) => date1.Ticks == date2.Ticks;
		public static bool operator !=(SerializableDateTime date1, SerializableDateTime date2) => date1.Ticks != date2.Ticks;
		public static bool operator >(SerializableDateTime date1, SerializableDateTime date2) => date1.Ticks > date2.Ticks;
		public static bool operator >=(SerializableDateTime date1, SerializableDateTime date2) => date1.Ticks >= date2.Ticks;
		public static bool operator <(SerializableDateTime date1, SerializableDateTime date2) => date1.Ticks < date2.Ticks;
		public static bool operator <=(SerializableDateTime date1, SerializableDateTime date2) => date1.Ticks <= date2.Ticks;

		public override bool Equals(object obj) {
			return obj is SerializableDateTime time &&
				   ticks == time.ticks;
		}

		public override int GetHashCode() {
			return HashCode.Combine(ticks);
		}

		public override string ToString() {
			return ((DateTime)this).ToString();
		}
		
		public string ToString(string format) {
			return ((DateTime)this).ToString(format);
		}
		
		public string ToString(IFormatProvider formatProvider) {
			return ((DateTime)this).ToString(formatProvider);
		}
		
		public string ToString(string format, IFormatProvider formatProvider) {
			return ((DateTime)this).ToString(format, formatProvider);
		}
	}
}
