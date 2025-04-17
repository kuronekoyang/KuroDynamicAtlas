using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace kuro
{
    public static class StringUtils
    {
        [ThreadStatic] private static StringBuilder? s_builder;
        private static StringBuilder SharedStringBuilder => s_builder ??= new();

        public static string AddDoubleQuotation(this string str) => $"\"{str.AddEscapeCharacters()}\"";

        // 添加转义字符
        public static string AddEscapeCharacters(this string str)
        {
            var sb = SharedStringBuilder;
            sb.Clear();
            sb.Append(str);
            sb.Replace("\\", "\\\\");
            sb.Replace("\b", "\\b");
            sb.Replace("\f", "\\f");
            sb.Replace("\t", "\\t");
            sb.Replace("\n", "\\n");
            sb.Replace("\r", "\\r");
            if (IsEquals(sb, str))
                return str;
            return sb.ToString();
        }

        public static bool IsEquals(this StringBuilder? sb, string? str)
        {
            if (sb == null && str == null)
                return true;
            if (sb == null || str == null)
                return false;
            var len = sb.Length;
            if (str.Length != len)
                return false;
            for (int i = 0; i < len; ++i)
            {
                if (sb[i] != str[i])
                    return false;
            }

            return true;
        }
    }
}