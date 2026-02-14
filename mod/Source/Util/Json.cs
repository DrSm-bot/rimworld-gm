using System;
using System.Globalization;
using System.Text;

namespace RimworldGM.Util
{
    /// <summary>
    /// Minimal JSON helpers compatible with RimWorld's older runtime.
    /// </summary>
    public static class Json
    {
        public static string Quote(string value)
        {
            if (value == null)
            {
                return "null";
            }

            var sb = new StringBuilder(value.Length + 2);
            sb.Append('"');
            for (var i = 0; i < value.Length; i++)
            {
                var c = value[i];
                switch (c)
                {
                    case '\\': sb.Append("\\\\"); break;
                    case '"': sb.Append("\\\""); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default:
                        if (c < 32)
                        {
                            sb.Append("\\u");
                            sb.Append(((int)c).ToString("x4"));
                        }
                        else
                        {
                            sb.Append(c);
                        }
                        break;
                }
            }
            sb.Append('"');
            return sb.ToString();
        }

        public static string Bool(bool value)
        {
            return value ? "true" : "false";
        }

        public static string Number(long value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }
    }
}
