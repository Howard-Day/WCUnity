/// ©2021 Kevin Foley.
/// See accompanying license file.

using NUnit.Framework;
using OneManEscapePlan.Common.Scripts.DataStructures;

namespace OneManEscapePlan.Common.Tests {
	public class AngleRangeTests {
		[Test]
		public void General() {
			AngleRangeValue arv;

			Assert.Catch<System.ArgumentException>(() => { arv = new AngleRangeValue(new Angle(0), -30); });

			arv = new AngleRangeValue(new Angle(0), 30);
			Assert.IsTrue(arv.Start == new Angle(0));
			Assert.IsTrue(arv.End == new Angle(30));
			Assert.IsTrue(arv.Size == 30);
			Assert.IsTrue(arv.Mid == new Angle(15));

			arv = new AngleRangeValue(new Angle(-30), 60);
			Assert.IsTrue(arv.Start == new Angle(-30));
			Assert.IsTrue(arv.End == new Angle(30));
			Assert.IsTrue(arv.Size == 60);
			Assert.IsTrue(arv.Mid == new Angle(0));

			//special case when size >= 360
			arv = new AngleRangeValue(new Angle(129), 370);
			Assert.IsTrue(arv.Start == new Angle(0));
			Assert.IsTrue(arv.End == new Angle(360));
			Assert.IsTrue(arv.Size == 360);
			Assert.IsTrue(arv.Mid == new Angle(180));

			arv = new AngleRangeValue(new Angle(30), new Angle(50));
			Assert.IsTrue(arv.Start == new Angle(30));
			Assert.IsTrue(arv.End == new Angle(50));
			Assert.IsTrue(arv.Size == 20);
			
			arv = new AngleRangeValue(new Angle(30), new Angle(-20));
			Assert.IsTrue(arv.Start == new Angle(30));
			Assert.IsTrue(arv.End == new Angle(-20));
			Assert.IsTrue(arv.Size == 310);
			
			arv = new AngleRangeValue(new Angle(-20), new Angle(15));
			Assert.IsTrue(arv.Start == new Angle(-20));
			Assert.IsTrue(arv.End == new Angle(15));
			Assert.IsTrue(arv.Size == 35);
		}

		[Test]
		public void Clamp() {
			var arv = new AngleRangeValue(new Angle(30), 60);
			Assert.IsTrue(arv.Clamp(new Angle(20)) == new Angle(30));
			Assert.IsTrue(arv.Clamp(new Angle(50)) == new Angle(50));
			Assert.IsTrue(arv.Clamp(new Angle(95)) == new Angle(90));
			Assert.IsTrue(arv.Clamp(new Angle(209)) == new Angle(90));
			Assert.IsTrue(arv.Clamp(new Angle(350)) == new Angle(30));
			Assert.IsTrue(arv.Clamp(new Angle(211)) == new Angle(90));
			Assert.IsTrue(arv.Clamp(new Angle(91)) == new Angle(90));
			Assert.IsTrue(arv.Clamp(new Angle(-269)) == new Angle(90));
			Assert.IsTrue(arv.Clamp(new Angle(-30)) == new Angle(30));

			TestClamp(arv);
			var ar = new AngleRange(arv.Start, arv.Size);
			TestClamp(ar);
			arv = new AngleRangeValue(new Angle(-20), 40);
			TestClamp(arv);
			ar.SetRange(arv.Start, arv.Size);
			TestClamp(ar);
			arv = new AngleRangeValue(new Angle(-50), 180);
			TestClamp(arv);
			ar.SetRange(arv.Start, arv.Size);
			TestClamp(ar);
		}

		[Test]
		public void Contains() {
			var arv = new AngleRangeValue(new Angle(30), 60);
			var ar = new AngleRange(arv.Start, arv.Size);
			for (float f = 0; f < 360; f++) {
				Assert.IsTrue(arv.Contains(new Angle(f)) == (f >= 30 && f <= 90));
				Assert.IsTrue(ar.Contains(new Angle(f)) == (f >= 30 && f <= 90));
			}

			arv = new AngleRangeValue(new Angle(-20), 40);
			ar.SetRange(arv.Start, arv.Size);
			for (float f = 0; f < 360; f++) {
				Assert.IsTrue(arv.Contains(new Angle(f)) == (f <= 20 || f >= 340));
				Assert.IsTrue(ar.Contains(new Angle(f)) == (f <= 20 || f >= 340));
			}
		}

		private void TestClamp(IReadOnlyAngleRange range) {
			float inverseSize = 360 - range.Size;

			for (float f = range.Start.Degrees360; f < range.Start.Degrees360 + range.Size; f++) {
				Angle a = new Angle(f);
				Assert.IsTrue(range.Clamp(a) == a);
			}

			for (float f = 0; f < inverseSize; f++) {
				Angle a = new Angle(range.End.Degrees360 + f);
				bool wrap = f >= inverseSize / 2;
				if (wrap) Assert.IsTrue(range.Clamp(a) == range.Start, $"{inverseSize} {wrap} {f} {a}");
				else Assert.IsTrue(range.Clamp(a) == range.End, $"{range} {inverseSize} {wrap} {f} {a} {range.Clamp(a)}");
			}
		}

		[Test]
		public void Lerp() {
			var arv = new AngleRangeValue(new Angle(-30), 30);
			TestLerp(arv);
			var ar = new AngleRange(arv.Start, arv.Size);
			TestLerp(ar);

			arv = new AngleRangeValue(new Angle(40), 181);
			TestLerp(arv);
			ar.SetRange(arv.Start, arv.Size);
			TestLerp(ar);
		}

		private void TestLerp(IReadOnlyAngleRange arv) {
			Assert.IsTrue(arv.Lerp(0) == arv.Start);
			Assert.IsTrue(arv.Lerp(1) == arv.End);
			Assert.IsTrue(arv.Lerp(.5f) == arv.Mid);

			float step = .01f;
			for (float i = step; i < 1; i += step) {
				Assert.IsTrue(arv.Lerp(i).IsClockwiseFrom(arv.Lerp(i - step)));
			}
		}
	}
}