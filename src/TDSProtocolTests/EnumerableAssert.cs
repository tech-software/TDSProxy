using System;
using System.Collections.Generic;
using System.Linq;
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
			if (expected == null && actual == null)
				return;

			// Both should be non-null, then
			if (expected == null)
				Assert.Fail("Expected null, actual non-null");
			if (actual == null)
				Assert.Fail("Expected non-null, actual null");

			// Iterator over each enumerable, comparing each element, until at least one iterator is exhausted
			var expectedIterator = expected.GetEnumerator();
			var actualIterator = actual.GetEnumerator();
			var moreExpected = expectedIterator.MoveNext();
			var moreActual = actualIterator.MoveNext();
			for (uint idx = 1; moreExpected && moreActual; moreExpected = expectedIterator.MoveNext(), moreActual = actualIterator.MoveNext(), idx++)
			{
				if (comparer.Compare(expectedIterator.Current, actualIterator.Current) != 0)
				{
					var lastDigit = idx % 10;
					Assert.AreEqual(
						expectedIterator.Current,
						actualIterator.Current,
						"The {0}{1} element in the sequences differed",
						idx,
						(lastDigit > 3 || lastDigit == 0 || ((idx / 10) == 1)) ? "th" : lastDigit == 1 ? "st" : lastDigit == 2 ? "nd" : "rd");
				}
			}

			// Check neither iterator has more
			if (moreExpected || moreActual)
				Assert.AreEqual(expected.Count(), actual.Count(), "Sequences were not of same length");
		}
	}
}
