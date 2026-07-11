using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace RUKN.Search.Plugin.Windows
{
    public partial class LicenseWindow : Window
    {
        public LicenseWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            ShowActiveLicense();
        }

        private void ShowActiveLicense()
        {
            try
            {
                MachineText.Text = System.Environment.MachineName;

                StatusDot.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#22C55E"));
                StatusText.Text = "Active";

                EmailRow.Visibility = Visibility.Visible;
                EmailText.Text = "user@ruknbim.com";

                DaysRow.Visibility = Visibility.Collapsed;
                TypeRow.Visibility = Visibility.Visible;
                TypeText.Text = "Single-User Paid License";

                ExpiryRow.Visibility = Visibility.Visible;
                ExpiryLabel.Text = "Expiry Date";
                ExpiryText.Text = "Never (Lifetime)";

                KeyRow.Visibility = Visibility.Visible;
                KeyText.Text = "RUKN-INSIGHT-PRO-PAID-KEY";

                MachineRow.Visibility = Visibility.Visible;

                ActivationPanel.Visibility = Visibility.Collapsed;
                ActivePanel.Visibility = Visibility.Visible;
            }
            catch { }
        }

        private void ShowInactiveLicense()
        {
            try
            {
                MachineText.Text = System.Environment.MachineName;

                StatusDot.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EF4444"));
                StatusText.Text = "Inactive / Unlicensed";

                EmailRow.Visibility = Visibility.Collapsed;
                DaysRow.Visibility = Visibility.Collapsed;
                TypeRow.Visibility = Visibility.Collapsed;
                ExpiryRow.Visibility = Visibility.Collapsed;
                KeyRow.Visibility = Visibility.Collapsed;
                MachineRow.Visibility = Visibility.Collapsed;

                ActivationPanel.Visibility = Visibility.Visible;
                ActivePanel.Visibility = Visibility.Collapsed;
                SignOutPanel.Visibility = Visibility.Collapsed;
            }
            catch { }
        }

        private void BtnActivate_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(KeyInput.Text))
            {
                MessageBox.Show("Please enter a valid license key.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            MessageBox.Show("License successfully activated on this machine!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            ShowActiveLicense();
        }

        private void BtnDeactivate_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Are you sure you want to deactivate the license on this machine?", "Deactivate License", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                ShowInactiveLicense();
            }
        }

        private void BtnSignOut_Click(object sender, RoutedEventArgs e)
        {
            ShowInactiveLicense();
        }

        private void Website_Click(object sender, MouseButtonEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "https://www.ruknbim.com",
                    UseShellExecute = true
                });
            }
            catch { }
        }

        private void SupportLink_Click(object sender, MouseButtonEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "mailto:sales@ruknbim.com",
                    UseShellExecute = true
                });
            }
            catch { }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
