/*
================================================================================
TextFormatter – Usage Examples
================================================================================

| Method                          | Input        | Output (en-US)   | Output (pl-PL)  |
|---------------------------------|--------------|------------------|-----------------|
| ToStringFast(int)               | 1234         | "1234"           | "1234"          |
| ToStringFast(float, 2)          | 1234.56f     | "1234.56"        | "1234,56"       |
| ToCompact(long)                 | 1234         | "1.2k"           | "1.2k"          |
| ToCompactRound(long)            | 1234         | "1k"             | "1k"            |
| ToCompact(long)                 | 987654321    | "987.7M"         | "987.7M"        |
| ToCompactRound(long)            | 987654321    | "988M"           | "988M"          |
| ToSeparated(long)               | 1234567      | "1,234,567"      | "1 234 567"     |
| ToSeparated(long)               | 1234567.89   | "1,234,568"      | "1 234 568"     |
| ToSeparatedWithSuffix           | 1234, "$"    | "1,234$"         | "1 234$"        |
| ToSeparatedWithSuffix           | 987654, " €" | "987,654 €"      | "987 654 €"     |
| ToSeparatedWithPrefix           | 1234, "$"    | "$1,234"         | "$1 234"        |
| ToSeparatedWithPrefix           | 5000, "XP "  | "XP 5,000"       | "XP 5 000"      |

Notes:
- en-US > thousands = comma (","), decimal = dot (".")
- pl-PL > thousands = non-breaking space (" "), decimal = comma (",")
================================================================================
*/


namespace CorePro.Utils
{
    using System;
    using System.Globalization;

    /// <summary>
    /// Provides GC-friendly text formatting for UI (numbers, caching, etc).
    /// </summary>
    public static class TextFormatter
    {
        private const int CacheMin = -9999;
        private const int CacheMax = 99999;
        private static readonly string[] IntCache;

        static TextFormatter()
        {
            int size = CacheMax - CacheMin + 1;
            IntCache = new string[size];
            for (int i = 0; i < size; i++)
            {
                int val = CacheMin + i;
                IntCache[i] = val.ToString();
            }
        }

        // --------------------
        // BASE
        // --------------------

        /// <summary>
        /// Converts int to string without GC (cached if between -9999 and 99999).
        /// Example: <c>ToStringFast(123)</c> -> "123"
        /// </summary>
        public static string ToStringFast(int value)
        {
            if (value >= CacheMin && value <= CacheMax)
                return IntCache[value - CacheMin];

            Span<char> buffer = stackalloc char[11];
            if (value.TryFormat(buffer, out int written))
                return new string(buffer.Slice(0, written));

            return string.Empty;
        }

        /// <summary>
        /// Converts float to string with fixed decimals.
        /// Example: <c>ToStringFast(12.345f, 2)</c> -> "12.35"
        /// </summary>
        public static string ToStringFast(float value, int decimals = 2)
        {
            Span<char> buffer = stackalloc char[32];
            string format = "F" + decimals; // e.g. "F2"

            if (value.TryFormat(buffer, out int written, format))
                return new string(buffer.Slice(0, written));

            return string.Empty;
        }

        // --------------------
        // COMPACT
        // --------------------

        /// <summary>
        /// Formats number with suffix (k/M/B/T) and decimals.
        /// Example: <c>ToCompact(12345)</c> → "12.3k"
        /// </summary>
        public static string ToCompact(long value, int decimals = 1) =>
            ToCompactInternal(value, decimals, rounded: false);

        /// <summary>
        /// Formats number with suffix (k/M/B/T) and decimals.
        /// Example: <c>ToCompact(987654321)</c> → "987.7M"
        /// </summary>
        public static string ToCompact(double value, int decimals = 1) =>
            ToCompactInternal(value, decimals, rounded: false);

        /// <summary>
        /// Formats number with suffix (k/M/B/T), rounded to whole units.
        /// Example: <c>ToCompactRound(12345)</c> → "12k"
        /// </summary>
        public static string ToCompactRound(long value) =>
            ToCompactInternal(value, decimals: 0, rounded: true);

        /// <summary>
        /// Formats number with suffix (k/M/B/T), rounded to whole units.
        /// Example: <c>ToCompactRound(987654321)</c> → "988M"
        /// </summary>
        public static string ToCompactRound(double value) =>
            ToCompactInternal(value, decimals: 0, rounded: true);

        private static string ToCompactInternal(double value, int decimals, bool rounded)
        {
            string suffix;
            double scaled;

            if (Math.Abs(value) >= 1_000_000_000_000d)
            {
                scaled = value / 1_000_000_000_000d;
                suffix = "T";
            }
            else if (Math.Abs(value) >= 1_000_000_000d)
            {
                scaled = value / 1_000_000_000d;
                suffix = "B";
            }
            else if (Math.Abs(value) >= 1_000_000d)
            {
                scaled = value / 1_000_000d;
                suffix = "M";
            }
            else if (Math.Abs(value) >= 1_000d)
            {
                scaled = value / 1_000d;
                suffix = "k";
            }
            else
            {
                return ToStringFast((int)value);
            }

            Span<char> buffer = stackalloc char[32];
            string format = rounded ? "F0" : "F" + decimals;

            if (scaled.TryFormat(buffer, out int written, format))
                return new string(buffer.Slice(0, written)) + suffix;

            return string.Empty;
        }

        // --------------------
        // SEPARATED
        // --------------------

        /// <summary>
        /// Formats number with thousands separators using current culture.
        /// Example (en-US): <c>ToSeparated(1234567)</c> → "1,234,567"
        /// Example (pl-PL): <c>ToSeparated(1234567)</c> → "1 234 567"
        /// </summary>
        public static string ToSeparated(long value, CultureInfo culture = null)
        {
            culture ??= CultureInfo.CurrentCulture;
            return value.ToString("N0", culture);
        }

        /// <summary>
        /// Formats number with thousands separators using a custom character.
        /// Example: <c>ToSeparated(1234567, " ")</c> → "1 234 567"
        /// Example: <c>ToSeparated(1234567, ".")</c> → "1.234.567"
        /// </summary>
        public static string ToSeparated(long value, string customSeparator)
        {
            var formatted = value.ToString("N0", CultureInfo.InvariantCulture);
            return formatted.Replace(",", customSeparator).Replace(".", customSeparator);
        }

        /// <summary>
        /// Formats number with separators and a suffix (e.g. currency, unit).
        /// Example: <c>ToSeparatedWithSuffix(1234, "$", " ")</c> → "1 234$"
        /// Example: <c>ToSeparatedWithSuffix(987654, " coins", ".")</c> → "987.654 coins"
        /// </summary>
        public static string ToSeparatedWithSuffix(long value, string suffix, string customSeparator = null, CultureInfo culture = null)
        {
            string core;
            if (customSeparator != null)
            {
                core = value.ToString("N0", CultureInfo.InvariantCulture)
                    .Replace(",", customSeparator)
                    .Replace(".", customSeparator);
            }
            else
            {
                culture ??= CultureInfo.CurrentCulture;
                core = value.ToString("N0", culture);
            }

            return core + suffix;
        }

        /// <summary>
        /// Formats number with separators and a prefix (e.g. currency, unit).
        /// Example: <c>ToSeparatedWithPrefix(1234, "$", " ")</c> → "$1 234"
        /// Example: <c>ToSeparatedWithPrefix(5000, "XP ", " ")</c> → "XP 5 000"
        /// </summary>
        public static string ToSeparatedWithPrefix(long value, string prefix, string customSeparator = null, CultureInfo culture = null)
        {
            string core;
            if (customSeparator != null)
            {
                core = value.ToString("N0", CultureInfo.InvariantCulture)
                    .Replace(",", customSeparator)
                    .Replace(".", customSeparator);
            }
            else
            {
                culture ??= CultureInfo.CurrentCulture;
                core = value.ToString("N0", culture);
            }

            return prefix + core;
        }
    }
}
