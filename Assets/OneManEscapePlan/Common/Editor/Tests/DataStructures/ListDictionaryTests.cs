/// ©2021 Kevin Foley. 
/// See accompanying license file.

using NUnit.Framework;
using OneManEscapePlan.Common.Scripts.DataStructures;

namespace OneManEscapePlan.Common.Tests {
	public class ListDictionaryTests {
		const string CATS = "Cats";
		const string DOGS = "Dogs";

		[Test]
		public void General() {
			ListDictionary<string, string> ld = new ListDictionary<string, string>(1, 5);
			Assert.IsTrue(ld.ListInitialCapacity == 5);

			Assert.IsNull(ld.GetListOrNull(CATS));
			Assert.IsNull(ld.GetListOrNull(DOGS));
			ld.GetOrCreateList(CATS);
			var cats = ld.GetListOrNull(CATS);
			Assert.IsNotNull(cats);
			Assert.IsTrue(cats.Count == 0);
			Assert.IsTrue(cats.Capacity == 5);

			ld.ListInitialCapacity = 3;
			Assert.IsTrue(ld.ListInitialCapacity == 3);
		}

		[Test]
		public void Methods() {
			ListDictionary<string, string> ld = new ListDictionary<string, string>(1, 5);
			int catCount = 0;
			Add(ld, CATS, "Calico", ref catCount);
			Add(ld, CATS, "Tabby", ref catCount);
			Assert.IsTrue(ld.ListCount == 1);
			Assert.IsTrue(ld.ValueCount == 2);

			ld.Add(DOGS, "Cocker Spaniel");
			Assert.IsTrue(ld.ListCount == 2);
			Assert.IsTrue(ld.ValueCount == 3);

			Add(ld, CATS, "Shorthair", ref catCount);
			Assert.IsTrue(ld.ValueCount == 4);
			Remove(ld, CATS, "Tabby", ref catCount);
			Assert.IsTrue(ld.ValueCount == 3);
			Remove(ld, CATS, "Tabby", ref catCount);
			Assert.IsTrue(ld.ValueCount == 3);

			var newCats = new string[] { "Siamese", "Persian" };
			AddRange(ld, CATS, newCats, ref catCount);
			Assert.IsTrue(ld.ValueCount == 5);

			ld.ClearList(CATS);
			Assert.IsTrue(ld.ListCount == 2);
			Assert.IsTrue(ld.ValueCount == 1); //Cocker Spaniel
			Assert.IsTrue(ld.RemoveList(CATS));
			Assert.IsTrue(ld.ListCount == 1); //dogs
			Assert.IsNull(ld.GetListOrNull(CATS));
			Assert.IsFalse(ld.RemoveList(CATS));
			ld.ClearAll();
			Assert.IsTrue(ld.ListCount == 0);
			Assert.IsTrue(ld.ValueCount == 0);
		}

		private void Add(ListDictionary<string, string> ld, string key, string value, ref int expectedCount) {
			var list = ld.Add(key, value);
			expectedCount++;
			Assert.IsTrue(list.Count == expectedCount);
			Assert.IsTrue(ld.GetListOrNull(key) == list);
		}

		private void AddRange(ListDictionary<string, string> ld, string key, string[] values, ref int expectedCount) {
			var list = ld.AddRange(key, values);
			expectedCount += values.Length;
			Assert.IsTrue(list.Count == expectedCount);
			Assert.IsTrue(ld.GetListOrNull(key) == list);
		}

		private void Remove(ListDictionary<string, string> ld, string key, string value, ref int expectedCount) {
			if (ld.RemoveValue(key, value)) {
				expectedCount--;
			}
			Assert.IsTrue(ld.GetListOrNull(key).Count == expectedCount);
		}
	}
}