/// ©2021 Kevin Foley.
/// See accompanying license file.

using NUnit.Framework;
using OneManEscapePlan.Common.Scripts.DataStructures;

namespace OneManEscapePlan.Common.Tests {
	public class AngleTests {
		[Test]
		public void Degrees() {
			Assert.IsTrue(new Angle(190).Degrees180 == -170);
			Assert.IsTrue(new Angle(-190).Degrees180 == 170);
			Assert.IsTrue(new Angle(360).Degrees180 == 0);
			Assert.IsTrue(new Angle(270).Degrees180 == -90);

			Assert.IsTrue(new Angle(-10).Degrees360 == 350);
			Assert.IsTrue(new Angle(370).Degrees360 == 10);
			Assert.IsTrue(new Angle(360).Degrees360 == 0);
			Assert.IsTrue(new Angle(-90).Degrees360 == 270);

			for (float i = -360; i < 360; i++) {
				Angle a = new Angle(i);
				if (i < -180 || (i >= 0 && i < 180)) {
					Assert.IsTrue(a.Degrees180 == a.Degrees360);
				} else if (i < 0 || i > 180) {
					Assert.IsTrue(a.Degrees360 == a.Degrees180 + 360);
				}
			}
		}

		[Test]
		public void CWandCCW() {
			for (float i = -360; i < 360; i++) {
				Angle a = new Angle(i);
				Assert.IsFalse(a.IsClockwiseFrom(a));
				Assert.IsFalse(a.IsCounterClockwiseFrom(a));
				for (float j = 1; j < 180; j++) {
					Angle b = new Angle(i + j);
					Assert.IsTrue(b.IsClockwiseFrom(a));
					Assert.IsTrue(a.IsCounterClockwiseFrom(b));
					Assert.IsFalse(b.IsCounterClockwiseFrom(a));
					Assert.IsFalse(a.IsClockwiseFrom(b));

					Angle c = new Angle(i - j);
					Assert.IsFalse(c.IsClockwiseFrom(a));
					Assert.IsTrue(c.IsCounterClockwiseFrom(a));
				}
			}
		}

		[Test]
		public void Equality() {
			Assert.IsTrue(new Angle(350) == new Angle(-10));
			Assert.IsTrue(new Angle(720) == new Angle(0));
			Assert.IsTrue(new Angle(-90) == new Angle(270));

			for (float i = -360; i < 360; i++) {
				Angle a = new Angle(i);
				Angle b = new Angle(i);

				Assert.IsTrue(a == b);
				Assert.IsTrue(a.Equals(b));
				Assert.IsFalse(a != b);

				for (float j = 1; j < 360; j++) {
					Angle c = new Angle(i + j);
					Assert.IsFalse(a == c);
					Assert.IsFalse(a.Equals(c));
					Assert.IsTrue(a != c);

					Angle d = new Angle(i - j);
					Assert.IsFalse(a == d);
					Assert.IsFalse(a.Equals(d));
					Assert.IsTrue(a != d);
				}
			}
		}

		[Test]
		public void Arithmetic() {
			Assert.IsTrue(new Angle(359) + new Angle(1) == new Angle(0));
			Assert.IsTrue(new Angle(10) - new Angle(20) == new Angle(350));
			Assert.IsTrue(new Angle(350) + new Angle(20) == new Angle(10));

			for (float i = -720; i < 720; i++) {
				Angle a = new Angle(i);

				for (float j = 1; j < 360; j++) {
					Angle b = new Angle(j);
					Assert.IsTrue(a + b == new Angle(a.Degrees360 + j));
					Assert.IsTrue(a - b == new Angle(a.Degrees360 - j));
				}
			}
		}

	}
}