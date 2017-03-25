using System;
using System.Windows;
using System.Windows.Controls;

namespace SilverlightExtended
{
    public partial class SummaryDetailsView : UserControl
    {
        public SummaryDetailsView()
        {
            InitializeComponent();
        }

        void CloseBtn_Click(object sender, RoutedEventArgs e)
        {
            Visibility = Visibility.Collapsed;
        }
    }
}


