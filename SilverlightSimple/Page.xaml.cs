using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using ProtoBuf;
using System.Reflection;

namespace SilverlightSimple
{

    public enum SomeEnum
    {
        Value0, Value1
    }
    [ProtoContract]
    public class SomeData
    {
        [ProtoMember(1)]
        public int SomeProperty { get; set; }

        [ProtoMember(2)]
        public SomeEnum SomeOtherProperty { get; set; }
    }
    public static class ListUtil
    {
        public static object GetList<T>(PropertyInfo property)
        {
            return new List<T>();
        }
    }

    public partial class Page : UserControl
    {
        public Page()
        {
            InitializeComponent();

            Type type = typeof(SomeData);
            object list = typeof(ListUtil)
                .GetMethod("GetList")
                .MakeGenericMethod(type)
                .Invoke(null, new object[] {null});

            SomeData orig = new SomeData { SomeProperty = 12345 };
            SomeData clone = Serializer.DeepClone(orig);

            this.txtBefore.Text = orig.SomeProperty.ToString();
            this.txtAfter.Text = clone.SomeProperty.ToString();
        }
    }
}
