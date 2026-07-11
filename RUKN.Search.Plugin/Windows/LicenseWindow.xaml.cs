using System;
using System.Windows;
using System.Windows.Input;

namespace RUKN.Search.Plugin.Windows
{
    public partial class LicenseWindow : Window
    {
        public LicenseWindow()
        {
            InitializeComponent();
        }

        private void Border_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                this.DragMove();
            }
        }

        private void Close_Button(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
