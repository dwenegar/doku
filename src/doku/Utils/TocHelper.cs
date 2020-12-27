// Copyright 2021 Simone Livieri. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Text;

namespace Dwenegar.Doku.Utils
{
    internal static class TocHelper
    {
        private static readonly string[] s_shouldBeCapitalized =
        {
            "i", "we", "you", "he", "she", "our", "him", "her", "his"
        };

        private static readonly string[] s_shouldBeAllCaps =
        {
            "http", "url"
        };

        public static string GetTitleForFile(string path)
        {
            string? header = FindFirstHeader(path);
            if (header != null)
            {
                string title = header.TrimStart('#').TrimStart();
                if (!string.IsNullOrEmpty(title))
                {
                    return title;
                }
            }

            string fileName = Path.GetFileNameWithoutExtension(path);
            return ToTitleCase(fileName);
        }

        // Implement title case loosely based on the APA Style
        public static string ToTitleCase(string s)
        {
            if (string.IsNullOrEmpty(s))
            {
                return s;
            }

            char[] chars = s.ToCharArray();

            // first pass: convert all characters to lowercase
            ToTitleCase_ToLower(chars);

            // second pass: convert all single '-' to spaces
            ToTitleCase_SingleHyphenToSpace(chars);

            // third pass: convert consecutive '-' into a single '-'
            int n = ToTitleCase_CompressHyphens(chars);

            // change capitalization
            var sb = new StringBuilder();

            var title = new string(chars, 0, n);
            string[] parts = title.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0)
            {
                return s;
            }

            AppendPart(sb, parts[0]);
            for (var i = 1; i < parts.Length; i++)
            {
                sb.Append(' ');
                AppendPart(sb, parts[i]);
            }

            return sb.ToString();
        }

        private static void AppendPart(StringBuilder sb, string s)
        {
            if (Array.IndexOf(s_shouldBeAllCaps, s) != -1)
            {
                sb.Append(s.ToUpper());
            }
            else if (Array.IndexOf(s_shouldBeCapitalized, s) == -1 && s.Length < 4)
            {
                sb.Append(s);
            }
            else
            {
                sb.Append(char.ToUpper(s[0]));
                if (s.Length > 1)
                {
                    sb.Append(s.Substring(1));
                }
            }
        }

        private static int ToTitleCase_CompressHyphens(char[] chars)
        {
            var n = 0;
            for (var i = 0; i < chars.Length; i++)
            {
                if (chars[i] != '-' || i + 1 == chars.Length || chars[i + 1] != '-')
                {
                    chars[n++] = chars[i];
                }
            }

            return n;
        }

        private static void ToTitleCase_SingleHyphenToSpace(char[] chars)
        {
            for (var i = 0; i < chars.Length; i++)
            {
                if (chars[i] == '-'
                    && (i == 0 || chars[i - 1] != '-')
                    && (i + 1 == chars.Length || chars[i + 1] != '-'))
                {
                    chars[i] = ' ';
                }
            }
        }

        private static void ToTitleCase_ToLower(char[] chars)
        {
            for (var i = 0; i < chars.Length; i++)
            {
                chars[i] = char.ToLower(chars[i]);
            }
        }

        private static string? FindFirstHeader(string path)
        {
            foreach (string line in File.ReadLines(path))
            {
                if (line.StartsWith('#'))
                {
                    return line;
                }

                if (!string.IsNullOrWhiteSpace(line))
                {
                    break;
                }
            }

            return null;
        }
    }
}
