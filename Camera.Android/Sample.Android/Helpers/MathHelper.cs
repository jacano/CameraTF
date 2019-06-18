using System;

namespace TailwindTraders.Mobile.Features.Scanning.AR
{
    public static class MathHelper
    {
        public static bool Between<T>(this T actual, T lower, T upper)
            where T : IComparable<T>
        {
            return actual.CompareTo(lower) >= 0 && actual.CompareTo(upper) <= 0;
        }
    }
}
