/// ©2021 Kevin Foley. 
/// See accompanying license file.

using NUnit.Framework;
using OneManEscapePlan.Common.Scripts.DataStructures;

namespace OneManEscapePlan.Common.Tests {
	public class FloatRangeTests {
		[Test]
		public void Clamp() {
			TestClamp(new FloatRange(5, 10));
			TestClamp(new FloatRangeValue(5, 10));
			TestClamp(new FloatRange(-10, 10));
			TestClamp(new FloatRangeValue(-10, 10));
		}

		private void TestClamp(IReadOnlyFloatRange range) {
			Assert.AreEqual(range.Clamp(range.Min - 1), range.Min);
			Assert.AreEqual(range.Clamp(range.Max + 1), range.Max);
			Assert.AreEqual(range.Clamp(range.Mid), range.Mid);
		}

		[Test]
		public void Normalize() {
			FloatRange range = new FloatRange(0, 20);
			TestNormalize1(range);
			FloatRangeValue rangeValue = new FloatRangeValue(0, 20);
			TestNormalize1(rangeValue);

			range = new FloatRange(-20, 5);
			rangeValue = new FloatRangeValue(-20, 5);

			TestNormalize2(range);
			TestNormalize2(rangeValue);
		}

		private void TestNormalize1(IReadOnlyFloatRange range) {
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
		
		private void TestNormalize2(IReadOnlyFloatRange range) {
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
			FloatRange range = new FloatRange(-15, 20);
			FloatRangeValue rangeValue = new FloatRangeValue(-15, 20);
			TestContains(range);
			TestContains(rangeValue);
		}

		private void TestContains(IReadOnlyFloatRange range) {
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
			FloatRange range = new FloatRange(-20, 20);
			TestLerp(range);
			FloatRangeValue rangeValue = new FloatRangeValue(-20, 20);
			TestLerp(rangeValue);
		}

		private void TestLerp(IReadOnlyFloatRange range) {
			Assert.IsTrue(range.Lerp(0) == -20);
			Assert.IsTrue(range.Lerp(.25f) == -10);
			Assert.IsTrue(range.Lerp(.5f) == 0);
			Assert.IsTrue(range.Lerp(.75f) == 10);
			Assert.IsTrue(range.Lerp(1f) == 20);
		}

		[Test]
		public void Operators() {
			Assert.IsTrue(new FloatRange(22, 33).Equals(new FloatRange(22, 33)));
			Assert.IsFalse(new FloatRange(22, 33).Equals(new FloatRange(-22, 33)));
		}

		[Test]
		public void ValueAndEquality() {
			FloatRange range = new FloatRange(-3.33f, 39.4f);
			Assert.AreEqual(range.Value, new FloatRangeValue(-3.33f, 39.4f));
			Assert.IsTrue(range.Value == new FloatRangeValue(-3.33f, 39.4f));
			Assert.IsTrue(range.Value.Equals(new FloatRangeValue(-3.33f, 39.4f)));

			FloatRange range2 = new FloatRange(range.Min, range.Max);
			Assert.IsTrue(range2.Equals(range));

			range.SetRange(0.1f, 10.7f);
			Assert.AreEqual(range.Value, new FloatRangeValue(0.1f, 10.7f));
			range.Value = new FloatRangeValue(15.38f, 160.96f);
			Assert.AreEqual(range.Value, new FloatRangeValue(15.38f, 160.96f));
		}
	}
}
