using System;
using System.Collections.Generic;
using System.Text;

namespace PlayniteExtensions.Common
{
    public class TitleComparer : StringComparer
    {
        public override int Compare(string x, string y)
        {
            if (x == null || y == null)
            {
                if (x == null && y != null)
                    return -1;
                if (x == null && y == null)
                    return 0;
                if (x != null && y == null)
                    return 1;
            }

            int ix = 0, iy = 0;
            while (ix < x.Length && iy < y.Length)
            {
                char cx = x[ix], cy = y[iy];
                while (!char.IsDigit(cx) && !char.IsLetter(cx))
                {
                    ix++;
                    if (ix >= x.Length)
                        break;

                    cx = x[ix];
                }
                while (!char.IsDigit(cy) && !char.IsLetter(cy))
                {
                    iy++;
                    if (iy >= y.Length)
                        break;

                    cy = y[iy];
                }
                bool xend = ix >= x.Length, yend = iy >= y.Length;

                if (xend || yend)
                {
                    if (xend && !yend)
                        return -1;
                    if (!xend && yend)
                        return 1;
                }
                var charComparison = char.ToUpperInvariant(cx).CompareTo(char.ToUpperInvariant(cy));
                if (charComparison != 0)
                    return charComparison;
                ix++;
                iy++;
            }
            return 0;
        }

        public override bool Equals(string x, string y)
        {
            return Compare(x, y) == 0;
        }

        public override int GetHashCode(string obj)
        {
            return obj.Deflate().GetHashCode();
        }
    }
}
