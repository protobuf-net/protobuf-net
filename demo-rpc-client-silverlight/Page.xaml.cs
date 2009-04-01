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
using demo_rpc_client_silverlight.NorthwindService;

namespace demo_rpc_client_silverlight
{
    public partial class Page : UserControl
    {
        public Page()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var client = new NorthwindClient();
            client.GetCustomersCompleted += new EventHandler<GetCustomersCompletedEventArgs>(client_GetCustomersCompleted);
            client.GetCustomersAsync();
        }

        void client_GetCustomersCompleted(object sender, GetCustomersCompletedEventArgs e)
        {
            Dispatcher.BeginInvoke(() =>
            {
                list.ItemsSource = e.Result;
            });
        }
    }
}
