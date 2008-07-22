using System;
using System.Collections.Generic;
using System.Text;

namespace Nuxleus.MetaData {

    [AttributeUsage(AttributeTargets.Enum | AttributeTargets.Field)]
    public class LabelAttribute : Attribute {

        public readonly string Label;

        public LabelAttribute(string message) {
            Label = message;
        }

        public static string FromMember (object o) {
            return ((LabelAttribute)
            o.GetType().GetMember(o.ToString())[0].GetCustomAttributes(typeof(LabelAttribute), false)[0]).Label;
        }

        public static string FromType (object o) {
            return ((LabelAttribute)
            o.GetType().GetCustomAttributes(typeof(LabelAttribute), false)[0]).Label;
        }
    }
}

