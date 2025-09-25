/// ©2021 Kevin Foley. 
/// See accompanying license file.

using NUnit.Framework;
using OneManEscapePlan.Common.Scripts.DataStructures;

namespace OneManEscapePlan.Common.Tests {
	public class TimeSpanFTests {
		[Test]
		public void Comparators() {
			TimeSpanF one = TimeSpanF.FromSeconds(1);
			TimeSpanF zero = TimeSpanF.FromSeconds(0);
			TimeSpanF negOne = TimeSpanF.FromSeconds(-1);

			TimeSpanF oneNew = new TimeSpanF(1);

			Assert.IsTrue(one > zero);
			Assert.IsTrue(one >= zero);
			Assert.IsTrue(zero > negOne);
			Assert.IsTrue(zero >= negOne);
			Assert.IsTrue(one > negOne);
			Assert.IsTrue(one >= negOne);
			Assert.IsTrue(one == oneNew);
			Assert.IsTrue(one != zero);
			Assert.IsTrue(one >= oneNew);
			Assert.IsTrue(one <= oneNew);
			Assert.IsTrue(zero < one);
			Assert.IsTrue(zero <= one);
			Assert.IsTrue(negOne < zero);
			Assert.IsTrue(negOne <= zero);
			Assert.IsTrue(negOne < one);
			Assert.IsTrue(negOne <= one);
		}

		[Test]
		public void Properties() {
			TimeSpanF ts = new TimeSpanF(5, 10, 15);
			Assert.IsTrue(ts.Hours == 5);
			Assert.IsTrue(ts.Minutes == 10);
			Assert.IsTrue(ts.Seconds == 15);

			Assert.IsTrue(ts.TotalSeconds == 60 * 60 * 5 + 60 * 10 + 15);

			ts = new TimeSpanF(5, 15, 0);
			Assert.IsTrue(ts.TotalHours == 5.25f);
			Assert.IsTrue(ts.TotalMinutes == 60 * 5 + 15);
		}

		[Test]
		public void Properties2() {
			TimeSpanF one = TimeSpanF.FromSeconds(1);
			Assert.IsTrue(one.IsPositive);
			Assert.IsFalse(one.IsNegative);
			Assert.IsFalse(one.IsZero);

			TimeSpanF zero = TimeSpanF.FromSeconds(0);
			Assert.IsFalse(zero.IsPositive);
			Assert.IsFalse(zero.IsNegative);
			Assert.IsTrue(zero.IsZero);

			TimeSpanF negOne = TimeSpanF.FromSeconds(-1);
			Assert.IsFalse(negOne.IsPositive);
			Assert.IsTrue(negOne.IsNegative);
			Assert.IsFalse(negOne.IsZero);
		}

		[Test]
		public void Constructor() {
			TimeSpanF ts = new TimeSpanF(1, 120, 75);
			Assert.IsTrue(ts.Hours == 3);
			Assert.IsTrue(ts.Minutes == 1);
			Assert.IsTrue(ts.Seconds == 15);

			ts = new TimeSpanF(0, 10, 10);
			TimeSpanF ts2 = new TimeSpanF(0, 0, 610);
			Assert.IsTrue(ts == ts2);

			Assert.IsTrue(new TimeSpanF(72) == TimeSpanF.FromSeconds(72));
			Assert.IsTrue(new TimeSpanF(0, 38, 0) == TimeSpanF.FromMinutes(38));
			Assert.IsTrue(new TimeSpanF(200, 0, 0) == TimeSpanF.FromHours(200));
		}

		[Test]
		public void Arithmetic() {
			TimeSpanF ts = new TimeSpanF(30);
			Assert.IsTrue(ts + ts == new TimeSpanF(0, 1, 0));
			Assert.IsTrue(ts * 5 == new TimeSpanF(150));
			Assert.IsTrue(ts / 5 == new TimeSpanF(6));
			Assert.IsTrue(ts - ts == TimeSpanF.FromSeconds(0));

			ts = TimeSpanF.FromSeconds(136) + TimeSpanF.FromMinutes(2);
			Assert.IsTrue(ts.Minutes == 4);
			Assert.IsTrue(ts.Seconds == 16);
			Assert.IsTrue(ts.TotalSeconds == 136 + 120);
		}

		[Test]
		public void ToStringSeconds() {
			TimeSpanF ts = new TimeSpanF(3.25f);
			string s = "00:00:03.25";
			Assert.IsTrue(ts.ToString() == s);

			ts = new TimeSpanF(3f);
			s = "00:00:03";
			Assert.IsTrue(ts.ToString() == s);
		}

		[Test]
		public void ToStringRoundedSeconds() {
			TimeSpanF ts = new TimeSpanF(3.257f);
			string s = "00:00:03.26";
			Assert.IsTrue(ts.ToString() == s);

			ts = new TimeSpanF(3.252f);
			s = "00:00:03.25";
			Assert.IsTrue(ts.ToString() == s);
		}

		[Test]
		public void ToStringMMSS() {
			TimeSpanF ts = new TimeSpanF(63.25f);
			string s = "00:01:03.25";
			Assert.IsTrue(ts.ToString() == s);

			ts = new TimeSpanF(123f);
			s = "00:02:03";
			Assert.IsTrue(ts.ToString() == s);
		}

		[Test]
		public void ToStringHHMMSS() {
			TimeSpanF ts = new TimeSpanF(3663.25f);
			string s = "01:01:03.25";
			Assert.IsTrue(ts.ToString() == s);

			ts = new TimeSpanF(12, 33, 15);
			s = "12:33:15";
			Assert.IsTrue(ts.ToString() == s);
		}
	}
}
