/// ©2021 Kevin Foley. 
/// See accompanying license file.

using NUnit.Framework;
using OneManEscapePlan.Common.Scripts.DataStructures;

namespace OneManEscapePlan.Common.Tests {
	public class IntRangeTests {
		[Test]
		public void Clamp() {
			TestClamp(new IntRange(5, 10));
			TestClamp(new IntRangeValue(5, 10));
			TestClamp(new IntRange(-10, 10));
			TestClamp(new IntRangeValue(-10, 10));
		}

		private void TestClamp(IReadOnlyIntRange range) {
			Assert.AreEqual(range.Clamp(range.Min - 1), range.Min);
			Assert.AreEqual(range.Clamp(range.Max + 1), range.Max);
			Assert.AreEqual(range.Clamp(range.Mid), range.Mid);
		}

		[Test]
		public void Normalize() {
			IntRange range = new IntRange(0, 20);
			TestNormalize1(range);
			IntRangeValue rangeValue = new IntRangeValue(0, 20);
			TestNormalize1(rangeValue);

			range = new IntRange(-20, 5);
			rangeValue = new IntRangeValue(-20, 5);

			TestNormalize2(range);
			TestNormalize2(rangeValue);
		}

		private void TestNormalize1(IReadOnlyIntRange range) {
			Assert.IsTrue(range.Normalize(-5, true) == 0);
			Assert.IsTrue(range.Normalize(-5, false) == -.25f);
			Assert.IsTrue(range.Normalize(5, true) == .25f);
			Assert.IsTrue(range.Normalize(5, false) == .25f);
			Assert.IsTrue(range.Normalize(10, true) == .5f);
			Assert.IsTrue(range.Normalize(10, false) == .5f);
			Assert.IsTrue(range.Normalize(20, true) == 1);
			Assert.IsTrue(range.Normalize(20, false) == 1);
			Assert.IsTrue(range.Normalize(25, true) == 1);
			Assert.IsTrue(range.Normalize(25, false) == 1.25);
		}	
		
		private void TestNormalize2(IReadOnlyIntRange range) {
			Assert.IsTrue(range.Normalize(0, true) == .8f);
			Assert.IsTrue(range.Normalize(0, false) == .8f);
			Assert.IsTrue(range.Normalize(-20, true) == 0f);
			Assert.IsTrue(range.Normalize(-20, false) == 0f);
			Assert.IsTrue(range.Normalize(-25, true) == 0f);
			Assert.IsTrue(range.Normalize(-25, false) == -.2f);
			Assert.IsTrue(range.Normalize(5, true) == 1f);
			Assert.IsTrue(range.Normalize(5, false) == 1f);
			Assert.IsTrue(range.Normalize(10, true) == 1);
			Assert.IsTrue(range.Normalize(10, false) == 1.2f);
		}

		[Test]
		public void Contains() {
			IntRange range = new IntRange(-15, 20);
			IntRangeValue rangeValue = new IntRangeValue(-15, 20);
			TestContains(range);
			TestContains(rangeValue);
		}

		private void TestContains(IReadOnlyIntRange range) {
			Assert.IsTrue(range.Contains(-15));
			Assert.IsTrue(range.Contains(-10));
			Assert.IsTrue(range.Contains(0));
			Assert.IsTrue(range.Contains(10));
			Assert.IsTrue(range.Contains(20));

			Assert.IsFalse(range.Contains(-20));
			Assert.IsFalse(range.Contains(25));
		}

		[Test]
		public void Lerp() {
			IntRange range = new IntRange(-20, 20);
			TestLerp(range);
			IntRangeValue rangeValue = new IntRangeValue(-20, 20);
			TestLerp(rangeValue);
		}

		private void TestLerp(IReadOnlyIntRange range) {
			Assert.IsTrue(range.Lerp(0) == -20);
			Assert.IsTrue(range.Lerp(.25f) == -10);
			Assert.IsTrue(range.Lerp(.5f) == 0);
			Assert.IsTrue(range.Lerp(.75f) == 10);
			Assert.IsTrue(range.Lerp(1f) == 20);
		}

		[Test]
		public void Operators() {
			Assert.IsTrue(new IntRange(22, 33).Equals(new IntRange(22, 33)));
			Assert.IsFalse(new IntRange(22, 33).Equals(new IntRange(-22, 33)));
		}

		[Test]
		public void ValueAndEquality() {
			IntRange range = new IntRange(-3, 39);
			Assert.AreEqual(range.Value, new IntRangeValue(-3, 39));
			Assert.IsTrue(range.Value == new IntRangeValue(-3, 39));
			Assert.IsTrue(range.Value.Equals(new IntRangeValue(-3, 39)));

			IntRange range2 = new IntRange(range.Min, range.Max);
			Assert.IsTrue(range2.Equals(range));

			range.SetRange(0, 10);
			Assert.AreEqual(range.Value, new IntRangeValue(0, 10));
			range.Value = new IntRangeValue(15, 160);
			Assert.AreEqual(range.Value, new IntRangeValue(15, 160));
		}
	}
}
