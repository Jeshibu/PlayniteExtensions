using System;
using System.Collections.Generic;
using System.Diagnostics;
using Xunit;

namespace PlayniteExtensions.Common.Tests
{
    public class TitleComparerTests
    {
        [Theory]
        [InlineData("FEAR", "F.E.A.R.", 0)]
        [InlineData("S.T.A.L.K.E.R.: Shadow of Chernobyl", "S.T.A.L.K.E.R - SHADOW OF CHERNOBYL", 0)]
        [InlineData("T.E.S.T", "test-----------------", 0)]
        [InlineData("T.E.S.T", "test-----------------2", -1)]
        [InlineData("test-----------------2", "T.E.S.T", 1)]
        [InlineData("XA", "XB", -1)]
        [InlineData("A1", "A2", -1)]
        [InlineData("B2", "B1", 1)]
        [InlineData("A", "A 2", -1)]
        public void TestTitleComparison(string title1, string title2, int expected)
        {
            var titleComparer = new TitleComparer();
            var output = titleComparer.Compare(title1, title2);
            Assert.Equal(expected, output);
        }

        public void Benchmark()
        {
            var titles = new List<Tuple<string, string>>
            {
                Tuple.Create("FEAR", "F.E.A.R."),
                Tuple.Create("FEAR: First Encounter Assault Recon", "F.E.A.R. - First Encounter Assault Recon"),
                Tuple.Create("S.T.A.L.K.E.R.: Shadow of Chernobyl", "S.T.A.L.K.E.R - SHADOW OF CHERNOBYL"),
            };
            int runCount = 100000;
            var titleComparer = new TitleComparer();

            var sw = Stopwatch.StartNew();
            for (int i = 0; i < runCount; i++)
            {
                int j = i % titles.Count;
                var t = titles[j];
                titleComparer.Equals(t.Item1, t.Item2);
            }
            sw.Stop();

            var titleComparerTime = sw.Elapsed;

            sw.Restart();
            for (int i = 0; i < runCount; i++)
            {
                int j = i % titles.Count;
                var t = titles[j];
                t.Item1.Deflate().Equals(t.Item2.Deflate(), StringComparison.InvariantCultureIgnoreCase);
            }
            sw.Stop();

            var deflateComparerTime = sw.Elapsed;
        }
    }
}
