using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TDSProtocolTests
{
	public static class EnumerableAssert
	{
		public static void AreEqual<T>(IEnumerable<T> expected, IEnumerable<T> actual) where T : IComparable<T>
		{
			AreEqual(expected, actual, Comparer<T>.Default);
		}

		public static void AreEqual<T>(IEnumerable<T> expected, IEnumerable<T> actual, IComparer<T> comparer)
		{
			// Both null? That's equal!
			if (expected is null && actual is null)
				return;

			// Both should be non-null, then
			if (expected is null)
				Assert.Fail("Expected null, actual non-null");
			if (actual is null)
				Assert.Fail("Expected non-null, actual null");

			// Iterator over each enumerable, comparing each element, until at least one iterator is exhausted
			using (var expectedIterator = expected.GetEnumerator())
			using (var actualIterator = actual.GetEnumerator())
			{
				bool moreExpected, moreActual;
				uint count = 0;
				for (uint idx = 1;
					 // NOTE: use of & not && is deliberate because we DO NOT want to shortcut
				     (moreExpected = expectedIterator.MoveNext()) & (moreActual = actualIterator.MoveNext());
				     idx++, count++)
				{
					if (comparer.Compare(expectedIterator.Current, actualIterator.Current) != 0)
					{
						var lastDigit = idx % 10;
						Assert.AreEqual(
							expectedIterator.Current,
							actualIterator.Current,
							"The {0}{1} element in the sequences differed",
							idx,
							(lastDigit > 3 || lastDigit == 0 || ((idx / 10) == 1)) ? "th" :
							lastDigit == 1 ? "st" :
							lastDigit == 2 ? "nd" : "rd");
					}
				}

				// Check neither iterator has more

				if (moreExpected)
				{
					uint expectedCount = count + 1;
					while (expectedIterator.MoveNext())
						expectedCount++;
					Assert.AreEqual(expectedCount, count, "Sequences were not of same length");
				}

				if (moreActual)
				{
					uint actualCount = count + 1;
					while (actualIterator.MoveNext())
						actualCount++;
					Assert.AreEqual(count, actualCount, "Sequences were not of same length");
				}
			}
		}
	}
}
