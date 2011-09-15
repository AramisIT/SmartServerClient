using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SmartServerClient
    {
    public static class Extensions
        {
        public static string ArrayToString(this char[] array, string format = null)
            {
            format = format == null? "{0}" : "{0:" + format + "}";
            StringBuilder sb = new StringBuilder();
            for ( int i = 0; i < array.Length; i++ )
                {
                sb.AppendFormat(format, array[ i ].ToString());
                }
            return sb.ToString();
            }
        }
    }
