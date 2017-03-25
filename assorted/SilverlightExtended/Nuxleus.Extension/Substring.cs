using System;
using System.Collections.Generic;
using System.Text;
using System.Globalization;

namespace Nuxleus.Extension {

    public static partial class StringUtils {

        ///// <summary>
        ///// Modified from Oleg Tkachenko's SubstringBefore and SubstringAfter extension functions
        ///// @ http://www.tkachenko.com/blog/archives/000684.html
        ///// This will be moved into an appropriate class once I have the time.
        ///// </summary>
        ///// <param name="source"></param>
        ///// <param name="value"></param>
        ///// <returns></returns>
        //public static string SubstringAfter ( this string source, string value ) {
        //    if (string.IsNullOrEmpty(value)) {
        //        return source;
        //    }
        //    CompareInfo compareInfo = CultureInfo.InvariantCulture.CompareInfo;
        //    int index = compareInfo.IndexOf(source, value, CompareOptions.Ordinal);
        //    if (index < 0) {
        //        //No such substring
        //        return string.Empty;
        //    }
        //    return source.Substring(index + value.Length);
        //}

        //public static string SubstringBefore ( this string source, string value ) {
        //    if (string.IsNullOrEmpty(value)) {
        //        return value;
        //    }
        //    CompareInfo compareInfo = CultureInfo.InvariantCulture.CompareInfo;
        //    int index = compareInfo.IndexOf(source, value, CompareOptions.Ordinal);
        //    if (index < 0) {
        //        //No such substring
        //        return string.Empty;
        //    }
        //    return source.Substring(0, index);
        //}
    }

}
