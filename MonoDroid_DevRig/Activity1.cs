using System;

using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using MonoDto;
using System.IO;
using ProtoBuf.Meta;

namespace MonoDroid_DevRig
{
    [Activity(Label = "My Activity", MainLauncher = true)]
    public class Activity1 : Activity
    {
        public Activity1(IntPtr handle)
            : base(handle)
        {
        }

        protected override void OnCreate(Bundle bundle)
        {
            
            base.OnCreate(bundle);

            // Set our view from the "main" layout resource
            SetContentView(Resource.layout.main);

            // Get our button from the layout resource,
            // and attach an event to it
            Button button = FindViewById<Button>(Resource.id.myButton);
            
            button.Click += delegate
            {

                var serializer = TypeModel.Create();

                var order = new OrderHeader
                {
                    Id = 1234,
                    CustomerRef = "123 / abcd",
                    OrderDate = DateTime.Today,
                    DueDate = DateTime.Today.AddDays(20),
                    Lines = {
                            new OrderDetail {
                                    LineNumber = 1, Quantity = 3, SKU = "ablka/123-23", UnitPrice = 12.34M, Notes = "FAO Fred"
                                },
                            new OrderDetail {
                                    LineNumber = 2, Quantity = 10, SKU = "dclkasd/23e13", UnitPrice = 4.99M
                                }
                        }

                };
                
                byte[] raw;
                using (var ms = new MemoryStream()) {
                    serializer.Serialize(ms, order);
                    raw = ms.ToArray();
                }
                button.Text = raw.Length + " bytes";
                OrderHeader clone;
                using (var ms = new MemoryStream(raw))
                {
                    clone = (OrderHeader) serializer.Deserialize(ms, null, typeof (OrderHeader));
                }
                decimal sum = 0M, oldSum = 0M;
                foreach (var line in clone.Lines) sum += line.UnitPrice*line.Quantity;
                foreach (var line in order.Lines) oldSum += line.UnitPrice * line.Quantity;
                button.Text += "; id=" + clone.Id + "; ref=" + clone.CustomerRef + "; ordered=" + clone.OrderDate +
                               "; lines=" + clone.Lines.Count
                               + "; value=" + sum + " (vs " + oldSum + ")";

                //new OrderSerializer();
            };
        }
    }
}

