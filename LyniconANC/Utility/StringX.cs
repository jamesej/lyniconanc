using System;
using System.Collections.Generic;
using System.Text;
using System.Security.Cryptography;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Lynicon.Utility
{
    public static class StringX
    {
        /// <summary>
        /// Leftmost n characters of a string (or the whole string if shorter than n)
        /// </summary>
        /// <param name="s">The string</param>
        /// <param name="n">The number of characters</param>
        /// <returns>The leftmost n characters</returns>
        public static string Left(this string s, int n)
        {
            return s.Substring(0, Math.Min(s.Length, n));
        }

        /// <summary>
        /// Rightmost n characters of a string (or the whole string if shorter than n)
        /// </summary>
        /// <param name="s">The string</param>
        /// <param name="n">The number of characters</param>
        /// <returns>The rightmost n characters</returns>
        public static string Right(this string s, int n)
        {
            int len = Math.Min(s.Length, n);
            return s.Substring(s.Length - len, len);
        }

        /// <summary>
        /// The part of the string before the first occurrence of the supplied substring, or the whole string
        /// if the substring does not exist within it
        /// </summary>
        /// <param name="s">The string</param>
        /// <param name="upTo">The substring</param>
        /// <returns>The string up to the first occurrence of the substring</returns>
        public static string UpTo(this string s, string upTo)
        {
            return s.UpTo(upTo, StringComparison.CurrentCulture);
        }
        /// <summary>
        /// The part of the string before the first occurrence of the supplied substring, or the whole string
        /// if the substring does not exist within it
        /// </summary>
        /// <param name="s">The string</param>
        /// <param name="upTo">The substring</param>
        /// <param name="comparisonType">The comparison type to use when matching the substring</param>
        /// <returns>The string up to the first occurrence of the substring</returns>
        public static string UpTo(this string s, string upTo, StringComparison comparisonType)
        {
            int pos = s.IndexOf(upTo, comparisonType);
            if (pos == -1)
                return s;
            else
                return s.Substring(0, pos);
        }

        /// <summary>
        /// The part of the string before the last occurrence of the supplied substring, or the whole string
        /// if the substring does not exist within it
        /// </summary>
        /// <param name="s">The string</param>
        /// <param name="upTo">The substring</param>
        /// <returns>The string up to the last occurrence of the substring</returns>
        public static string UpToLast(this string s, string upTo)
        {
            return s.UpToLast(upTo, StringComparison.CurrentCulture);
        }
        /// <summary>
        /// The part of the string before the last occurrence of the supplied substring, or the whole string
        /// if the substring does not exist within it
        /// </summary>
        /// <param name="s">The string</param>
        /// <param name="upTo">The substring</param>
        /// <param name="comparisonType">The comparison type to use when matching the substring</param>
        /// <returns>The string up to the last occurrence of the substring</returns>
        public static string UpToLast(this string s, string upTo, StringComparison comparisonType)
        {
            int pos = s.LastIndexOf(upTo, comparisonType);
            if (pos == -1)
                return s;
            else
                return s.Substring(0, pos);
        }

        /// <summary>
        /// The part of the string before the last occurrence of the supplied substring, throws an exception if
        /// the substring does not exist within it
        /// </summary>
        /// <param name="s">The string</param>
        /// <param name="upTo">The substring</param>
        /// <returns>The string up to the last occurrence of the substring</returns>
        public static string UpToRequired(this string s, string upTo)
        {
            int pos = s.IndexOf(upTo);
            if (pos < 0)
                throw new FormatException("No '" + upTo + "' in string");
            else
                return s.Substring(0, pos);
        }

        /// <summary>
        /// The string after the first occurrence of the substring, or the empty string if the substring does not
        /// exist within it
        /// </summary>
        /// <param name="s">The string</param>
        /// <param name="after">The substring</param>
        /// <returns>The string after the first occurrence of the substring</returns>
        public static string After(this string s, string after)
        {
            return s.After(after, StringComparison.CurrentCulture);
        }
        /// <summary>
        /// The string after the first occurrence of the substring, or the empty string if the substring does not
        /// exist within it
        /// </summary>
        /// <param name="s">The string</param>
        /// <param name="after">The substring</param>
        /// <param name="comparisonType">The comparison type used when matching the substring</param>
        /// <returns>The string after the first occurrence of the substring</returns>
        public static string After(this string s, string after, StringComparison comparisonType)
        {
            int pos = s.IndexOf(after, comparisonType);
            if (pos == -1)
                return "";
            else
                return s.Substring(pos + after.Length);
        }

        /// <summary>
        /// The string after the last occurrence of the substring, throws an exception if
        /// the substring does not exist within it
        /// </summary>
        /// <param name="s">The string</param>
        /// <param name="after">The substring</param>
        /// <returns>The string after the last occurrence of the substring</returns>
        public static string AfterRequired(this string s, string after)
        {
            int pos = s.IndexOf(after);
            if (pos < 0)
                throw new FormatException("No '" + after + "' in string");
            else
                return s.Substring(pos + after.Length);
        }

        /// <summary>
        /// The string after the last occurrence of the substring, or the empty string if the substring does not
        /// exist within it
        /// </summary>
        /// <param name="s">The string</param>
        /// <param name="after">The substring</param>
        /// <returns>The string after the last occurrence of the substring</returns>
        public static string LastAfter(this string s, string after)
        {
            int pos = s.LastIndexOf(after);
            if (pos == -1)
                return "";
            else
                return s.Substring(pos + after.Length);
        }

        /// <summary>
        /// Strip anything not a letter, digit, punctuation or space from a string
        /// </summary>
        /// <param name="s">The string</param>
        /// <returns>String reduced to letters, digits, punctuation or spaces</returns>
        public static string StandardCharsOnly(this string s)
        {
            StringBuilder sb = new StringBuilder();
            foreach (char c in s)
            {
                if (char.IsLetterOrDigit(c) || char.IsPunctuation(c) || c == ' ')
                    sb.Append(c);
            }
            return sb.ToString();
        }

        /// <summary>
        /// Replaces any diacritic characters in a string with the non-diacritic equivalents
        /// </summary>
        /// <param name="text">The string</param>
        /// <returns>The converted string</returns>
        public static string RemoveDiacritics(this string text)
        {
            var normalizedString = text.Normalize(NormalizationForm.FormD);
            var stringBuilder = new StringBuilder();

            foreach (var c in normalizedString)
            {
                var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
                if (unicodeCategory != UnicodeCategory.NonSpacingMark)
                {
                    stringBuilder.Append(c);
                }
            }

            return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
        }

        /// <summary>
        /// Converts the string to proper case (e.g. capitalized initial letter on each word, lower case rest of word)
        /// </summary>
        /// <param name="s">The string</param>
        /// <returns>string converted to proper case</returns>
        public static string ToProper(this string s)
        {
            StringBuilder sb = new StringBuilder();
            bool isInitial = true;
            foreach (char c in s)
            {
                if (isInitial && char.IsLower(c))
                {
                    sb.Append(char.ToUpper(c));
                    isInitial = false;
                }
                else if (!isInitial && char.IsUpper(c))
                    sb.Append(char.ToLower(c));
                else
                {
                    sb.Append(c);
                    isInitial = char.IsWhiteSpace(c);
                }
            }
            return sb.ToString();
        }

        /// <summary>
        /// Remove the plural ending from a word
        /// </summary>
        /// <param name="s">The word</param>
        /// <returns>Depluralised word</returns>
        public static string Depluralise(this string s)
        {
            if (s.EndsWith("s"))
            {
                if (s.EndsWith("ss"))
                    return s;
                else if (s.EndsWith("ches") || s.EndsWith("shes") || s.EndsWith("xes"))
                    return s.Substring(0, s.Length - 2);
                else if (s.EndsWith("ies"))
                    return s.Substring(0, s.Length - 3) + "y";
                else
                    return s.Substring(0, s.Length - 1);
            }

            return s;
        }

        /// <summary>
        /// Puts spaces between parts of a camel case word
        /// </summary>
        /// <param name="s">The camel case word</param>
        /// <returns>Camel case word expanded with spaces</returns>
        public static string ExpandCamelCase(this string s)
        {
            return Regex.Replace(s, "[a-z][A-Z]", m => m.ToString().Substring(0, 1) + " " + m.ToString().Substring(1));
        }

        /// <summary>
        /// Returns true if a string starts with any of a list of strings, and returns the rest of the
        /// string in an out parameter
        /// </summary>
        /// <param name="s">The string</param>
        /// <param name="starts">List of possible starts of the string</param>
        /// <param name="rem">Output remainder of string after matched start (if any)</param>
        /// <returns>True if start matches any of options</returns>
        public static bool StartsWithAny(this string s, IEnumerable<string> starts, out string rem)
        {
            foreach (string start in starts)
                if (s.StartsWith(start))
                {
                    rem = s.After(start);
                    return true;
                }
            rem = null;
            return false;
        }

        /// <summary>
        /// Strip out regions of a string between a pair of delimiters
        /// </summary>
        /// <param name="s">The string</param>
        /// <param name="delimStart">Region start delimiter</param>
        /// <param name="delimEnd">Region end delimiter</param>
        /// <returns>String with delimited regions removed</returns>
        public static string StripRegions(this string s, string delimStart, string delimEnd)
        {
            if (s == null)
                return null;

            StringBuilder sb = new StringBuilder();
            int pos = 0, pos1 = 0;
            while (pos < s.Length)
            {
                pos1 = s.IndexOf(delimStart, pos, StringComparison.InvariantCultureIgnoreCase);
                if (pos1 == -1) pos1 = s.Length;
                sb.Append(s.Substring(pos, pos1 - pos));
                pos = pos1;
                if (pos1 < s.Length)
                {
                    pos1 = s.IndexOf(delimEnd, pos, StringComparison.InvariantCultureIgnoreCase);
                    if (pos1 == -1)
                        pos = s.Length;
                    else
                        pos = pos1 + delimEnd.Length;
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Convert a string to an int or return null if it can't be parsed
        /// </summary>
        /// <param name="s">The string</param>
        /// <returns>Int or null if can't be parsed</returns>
        public static int? AsIntOrNull(this string s)
        {
            int i;
            if (int.TryParse(s, out i))
                return i;
            else
                return null;
        }

        /// <summary>
        /// Convert a string to a guid or return null if it can't be parsed
        /// </summary>
        /// <param name="s">The string</param>
        /// <returns>Guid or null if it can't be parsed</returns>
        public static Guid? AsGUIDOrNull(this string s)
        {
            Guid guid;
            if (Guid.TryParse(s, out guid))
                return guid;
            else
                return null;
        }

        /// <summary>
        /// The the MD5 hash of a string
        /// </summary>
        /// <param name="str">The string</param>
        /// <returns>MD5 hash</returns>
        public static string GetMd5Sum(this string str)
        {
            // First we need to convert the string into bytes, which
            // means using a text encoder.
            Encoder enc = System.Text.Encoding.Unicode.GetEncoder();

            // Create a buffer large enough to hold the string
            byte[] unicodeText = new byte[str.Length * 2];
            enc.GetBytes(str.ToCharArray(), 0, str.Length, unicodeText, 0, true);

            // Now that we have a byte array we can ask the CSP to hash it
            MD5 md5 = new MD5CryptoServiceProvider();
            byte[] result = md5.ComputeHash(unicodeText);

            // Build the final string by converting each byte
            // into hex and appending it to a StringBuilder
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < result.Length; i++)
            {
                sb.Append(result[i].ToString("X2"));
            }

            // And return it
            return sb.ToString();
        }

        /// <summary>
        /// Gets the part of a string right of a position marker and left of the leftmost of a set of
        /// possible terminators, either including the terminator in the result or not, and
        /// as a side effect advancing the marker to just after the terminator
        /// </summary>
        /// <param name="s">string to scan</param>
        /// <param name="pos">position marker (set to -1 if no terminator match)</param>
        /// <param name="terminators">list of terminators</param>
        /// <param name="includeTerm">whether to include terminator in return</param>
        /// <returns>section before/including matched terminator, or whole string if no terminator found</returns>
        static public string GetHead(this string s, ref int pos, string[] terminators, bool includeTerm)
        {
            return s.GetHead(ref pos, terminators, includeTerm, true);
        }
        /// <summary>
        /// Gets the part of a string right of a position marker and left of the leftmost of a set of
        /// possible terminators, either including the terminator in the result or not, and
        /// as a side effect advancing the marker to before or just after the terminator
        /// </summary>
        /// <param name="s">string to scan</param>
        /// <param name="pos">position marker (set to -1 if no terminator match)</param>
        /// <param name="terminators">list of terminators</param>
        /// <param name="includeTerm">whether to include terminator in return</param>
        /// <param name="skipTerm">whether to position the marker after the terminator (or else at the terminator)</param>
        /// <returns>section before/including matched terminator, or whole string if no terminator found</returns>
        static public string GetHead(this string s, ref int pos, string[] terminators, bool includeTerm, bool skipTerm)
        {
            string tf;
            return s.GetHead(ref pos, terminators, includeTerm, skipTerm, out tf);
        }
        /// <summary>
        /// Gets the part of a string right of a position marker and left of the leftmost of a terminator and
        /// as a side effect advancing the marker to just after the terminator
        /// </summary>
        /// <param name="s">string to scan</param>
        /// <param name="pos">position marker (set to -1 if no terminator match)</param>
        /// <param name="terminator">terminator</param>
        /// <returns>section before/including matched terminator, or whole string if no terminator found</returns>
        static public string GetHead(this string s, ref int pos, string terminator)
        {
            string tf;
            return s.GetHead(ref pos, new string[] {terminator}, false, true, out tf);
        }
        /// <summary>
        /// Gets the part of a string right of a position marker and left of the leftmost of a set of
        /// possible terminators and as a side effect advancing the marker to just after the terminator,
        /// returning the terminator found in a parameter
        /// </summary>
        /// <param name="s">string to scan</param>
        /// <param name="pos">position marker (set to -1 if no terminator match)</param>
        /// <param name="terminators">list of terminators</param>
        /// <param name="tf">output of the terminator matched from the list</param>
        /// <returns>section before matched terminator, or whole string if no terminator found</returns>
        static public string GetHead(this string s, ref int pos, string[] terminators, out string tf)
        {
            return s.GetHead(ref pos, terminators, false, true, out tf);
        }
        /// <summary>
        /// Gets the part of a string right of a position marker and left of the leftmost of a set of
        /// possible terminators, either including the terminator in the result or not, and
        /// as a side effect advancing the marker to before or just after the terminator, returning the matched terminator
        /// </summary>
        /// <param name="s">string to scan</param>
        /// <param name="pos">position marker (set to -1 if no terminator match)</param>
        /// <param name="terminators">list of terminators</param>
        /// <param name="includeTerm">whether to include terminator in return</param>
        /// <param name="skipTerm">whether to position the marker after the terminator (or else at the terminator)</param>
        /// <param name="termFound">output of the terminator matched from the list</param>
        /// <returns>section before/including matched terminator, or whole string if no terminator found</returns>
        static public string GetHead(this string s, ref int pos, string[] terminators, bool includeTerm, bool skipTerm, out string termFound)
        {
            return s.GetHead(ref pos, terminators, includeTerm, skipTerm, out termFound, StringComparison.InvariantCultureIgnoreCase);
        }
        /// <summary>
        /// Gets the part of a string right of a position marker and left of the leftmost of a set of
        /// possible terminators, either including the terminator in the result or not, and
        /// as a side effect advancing the marker to before or just after the terminator, returning the matched terminator
        /// and specifying the kind of string comparison to use
        /// </summary>
        /// <param name="s">string to scan</param>
        /// <param name="pos">position marker (set to -1 if no terminator match)</param>
        /// <param name="terminators">list of terminators</param>
        /// <param name="includeTerm">whether to include terminator in return</param>
        /// <param name="skipTerm">whether to position the marker after the terminator (or else at the terminator)</param>
        /// <param name="termFound">output of the terminator matched from the list</param>
        /// <param name="comp">the type of string comparison to use</param>
        /// <returns>section before/including matched terminator, or whole string if no terminator found</returns>
        static public string GetHead(this string s, ref int pos, string[] terminators, bool includeTerm, bool skipTerm, out string termFound, StringComparison comp)
        {
            termFound = null;
            if (pos < 0)
                return "";

            string res;

            int firstPos = int.MaxValue;
            int newPos = 0;
            int termLen = 0;
            int endPos;

            // Find leftmost match of a terminator (preferring first in list)
            foreach (string term in terminators)
            {
                newPos = s.IndexOf(term, pos, comp);
                if (newPos != -1 && newPos < firstPos)
                {
                    firstPos = newPos;
                    termLen = term.Length;
                    termFound = term;
                }
            }

            if (firstPos != int.MaxValue)
            {
                if (includeTerm)
                    endPos = firstPos + termLen;
                else
                    endPos = firstPos;
                res = s.Substring(pos, endPos - pos);
                pos = firstPos;
                if (skipTerm)
                    pos += termLen;
                if (pos >= s.Length) pos = -1;
                return res;
            }
            else
            {
                endPos = pos;
                pos = -1;
                return s.Substring(endPos);
            }
        }
    }
}
