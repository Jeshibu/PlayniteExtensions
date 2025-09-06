using System;
using System.Text;

namespace PlayniteExtensions.Common;

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

        x = x.Normalize(NormalizationForm.FormKD);
        y = y.Normalize(NormalizationForm.FormKD);

        int ix = 0, iy = 0;
        bool xEnd, yEnd;
        while (true)
        {
            char? cx = GetNextLetterOrNumber(x, ref ix, out xEnd);
            char? cy = GetNextLetterOrNumber(y, ref iy, out yEnd);

            if (xEnd || yEnd)
                break;

            var charComparison = char.ToUpperInvariant(cx!.Value).CompareTo(char.ToUpperInvariant(cy!.Value));
            if (charComparison != 0)
                return charComparison;

            //bump the indexes to account for the characters that have been read
            ix++;
            iy++;
        }

        if (xEnd && !yEnd && HasLettersOrNumbersFromIndex(y, iy))
            return -1;
        if (!xEnd && yEnd && HasLettersOrNumbersFromIndex(x, ix))
            return 1;

        return 0;
    }

    private static char? GetNextLetterOrNumber(string str, ref int index, out bool endOfString)
    {
        while (index < str.Length)
        {
            char c = str[index];
            if (char.IsLetter(c) || char.IsDigit(c))
            {
                endOfString = false;
                return c;
            }
            index++;
        }
        endOfString = true;
        return null;
    }

    private static bool HasLettersOrNumbersFromIndex(string str, int index)
    {
        var c = GetNextLetterOrNumber(str, ref index, out bool endOfString);
        return !endOfString;
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
