using OneManEscapePlan.Common.Scripts.DataStructures;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace OneManEscapePlan.Common.Scripts.DataStructures {
	public interface IReadOnlyAngleRange {
		Angle Start { get; }
		Angle Mid { get; }
		Angle End { get; }
		float Size { get; }

		Angle Clamp(Angle value);
		bool Contains(Angle value);
		Angle GetRandom();
		Angle Lerp(float t);
	}

	/// <summary>
	/// Immutable struct representing a contiguous range of angles (i.e. an arc).
	/// </summary>
	/// <remarks>
	/// We can't use a FloatRange to represent angles, because it won't account for wrapping from 360°
	/// to 0°. For example, we could define a float range from 350 to 370 (-10° to 10°) but if we tried
	/// to clamp 11, it would give us 350 (-10°) because 11 is closer to 350 than to 370. In contrast,
	/// AngleRangeValue correctly accounts for wrapping from 360° to 0°.
	/// </remarks>
	[System.Serializable]
	public struct AngleRangeValue : IReadOnlyAngleRange {
		#region FIELDS
		[SerializeField] private Angle start;
		[SerializeField] private float size;

		/// <summary>
		/// Create a new AngleRangeValue
		/// </summary>
		/// <param name="start">Starting angle</param>
		/// <param name="sizeDegrees">Size of the range in degrees; must be positive</param>
		/// <exception cref="System.ArgumentException"></exception>
		public AngleRangeValue(Angle start, float sizeDegrees) {
			if (sizeDegrees < 0) throw new System.ArgumentException($"Size cannot be negative (given {sizeDegrees})");
			if (sizeDegrees >= 360) {
				this.start = new Angle(0);
				this.size = 360;
			} else {
				this.start = start;
				this.size = sizeDegrees;
			}
		}

		/// <summary>
		/// Create a new AngleRangeValue
		/// </summary>
		/// <param name="start">Starting angle</param>
		/// <param name="end">End angle; will be treated as being in the positive direction from <c>start</c></param>
		/// <remarks>
		/// <c>end</c> is always treated as being clockwise (in the positive direction from)
		/// <c>start</c>. For example, if we use 20° as the start and 10° as the end, the
		/// range will cover a 350° arc from 20° to 10°, NOT a 10° arc from 10° to 20°
		/// </remarks>
		public AngleRangeValue(Angle start, Angle end) {
			var s = start.Degrees360;
			var e = end.Degrees360;
			while (e < s) e += 360;
			var size = e - s;
			if (size >= 360) {
				this.start = new Angle(0);
				this.size = 360;
			} else {
				this.start = start;
				this.size = size;
			}
		}

		public Angle Start => start;

		public Angle Mid => start + new Angle(size / 2);

		public Angle End => start + new Angle(size);

		/// <summary>
		/// Size of the range (arc), in degrees
		/// </summary>
		public float Size => size;

		public Angle Clamp(Angle value) {
			if (Size >= 360) return value;
			float min = Start.Degrees360;
			float mid = min + Size / 2;
			float v = value.Degrees360;
			float deltaMid = Mathf.DeltaAngle(v, mid);
			//Debug.Log($"Min: {min}, Mid: {mid}, v: {v}, deltaMid: {deltaMid}, size/2: {Size / 2}");
			if (Mathf.Abs(deltaMid) <= Size / 2) {
				return value;
			}
			if (deltaMid < 0) return End;
			return Start;
		}

		public bool Contains(Angle value) {
			return Clamp(value) == value;
		}

		public Angle GetRandom() {
			return Lerp(UnityEngine.Random.value);
		}

		public Angle Lerp(float t) {
			return start + new Angle(size * t);
		}

		public override string ToString() {
			return $"({Start}-{End})";
		}
		#endregion
	}

	/// <summary>
	/// Class representing a contiguous range of angles (i.e. an arc).
	/// </summary>
	/// <remarks>
	/// A mutable, reference-type wrapper for AngleRangeValue.
	/// </remarks>
	[System.Serializable]
	public class AngleRange : IReadOnlyAngleRange {
		[SerializeField] private AngleRangeValue value;

		/// <summary>
		/// Create a new AngleRange.
		/// </summary>
		/// <param name="start">Starting angle</param>
		/// <param name="sizeDegrees">Size of the range in degrees; must be positive</param>
		/// <exception cref="System.ArgumentException">Thrown if size is negative</exception>
		public AngleRange(Angle start, float sizeDegrees) {
			SetRange(start, sizeDegrees);
		}

		/// <summary>
		/// Create a new AngleRange.
		/// </summary>
		/// <param name="start"></param>
		/// <param name="end">End angle; will be treated as being in the positive direction from <c>start</c></param>
		public AngleRange(Angle start, Angle end) {
			SetRange(start, end);
		}

		public Angle Start => value.Start;

		public Angle Mid => value.Mid;

		public Angle End => value.End;

		/// <summary>
		/// Size of the range (arc), in degrees
		/// </summary>
		public float Size => value.Size;

		public AngleRangeValue Value { get => value; set => this.value = value; }

		/// <summary>
		/// 
		/// </summary>
		/// <param name="start">Starting angle</param>
		/// <param name="size">Size of the range in degrees; must be positive</param>
		/// <exception cref="System.ArgumentException">Thrown if size is negative</exception>
		public void SetRange(Angle start, float size) {
			value = new AngleRangeValue(start, size);
		}

		/// <summary>
		/// Set the range.
		/// </summary>
		/// <param name="start"></param>
		/// <param name="end">End angle; will be treated as being in the positive direction from <c>start</c></param>
		/// <remarks>
		/// <c>end</c> is always treated as being clockwise (in the positive direction from)
		/// <c>start</c>. For example, if we use 20° as the start and 10° as the end, the
		/// range will cover a 350° arc from 20° to 10°, NOT a 10° arc from 10° to 20°
		/// </remarks>
		public void SetRange(Angle start, Angle end) {
			value = new AngleRangeValue(start, end);
		}

		public Angle Clamp(Angle value) {
			return this.value.Clamp(value);
		}

		public bool Contains(Angle value) {
			return this.value.Contains(value);
		}

		public Angle GetRandom() {
			return value.GetRandom();
		}

		public Angle Lerp(float t) {
			return value.Lerp(t);
		}
	}
}