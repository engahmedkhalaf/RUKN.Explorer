using System;
using System.Windows;
using System.Windows.Input;

namespace RUKN.Search.Plugin.Windows
{
    public partial class TrialWindow : Window
    {
        public bool IsTrialStarted { get; private set; } = false;

        public TrialWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
        }

        private async void BtnStartTrial_Click(object sender, RoutedEventArgs e)
        {
            string email = EmailInput.Text.Trim();
            if (string.IsNullOrEmpty(email) || !email.Contains("@"))
            {
                MessageBox.Show("Please enter a valid business email address.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            BtnStartTrial.IsEnabled = false;
            string machineName = System.Environment.MachineName;

            // Registers the trial server-side (one per machine, enforced by the database) and
            // returns the real start date so a local config wipe can't grant a fresh 14 days.
            var result = await RUKN.Search.Plugin.Utils.SupabaseService.RegisterTrialAsync(email, machineName);
            BtnStartTrial.IsEnabled = true;

            if (!result.Success)
            {
                MessageBox.Show($"Could not start trial:\n{result.Message}", "Trial Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            SettingsConfig.SetValue("TrialStartDate", result.StartDate);
            SettingsConfig.SetValue("LicenseEmail", email);

            MessageBox.Show(
                result.AlreadyExisted
                    ? "This machine already has a trial on record — resuming it."
                    : "14-day Free Trial started successfully!",
                "Trial Activated", MessageBoxButton.OK, MessageBoxImage.Information);
            IsTrialStarted = true;
            this.DialogResult = true;
            this.Close();
        }

        private void ActivateLicense_Click(object sender, MouseButtonEventArgs e)
        {
            LicenseWindow licenseWindow = new LicenseWindow();
            licenseWindow.Owner = this;
            licenseWindow.ShowDialog();

            // Check if they successfully activated a paid key inside LicenseWindow
            if (!string.IsNullOrEmpty(SettingsConfig.GetValue("LicenseKey")))
            {
                IsTrialStarted = true; // Act as authorized to bypass welcome
                this.DialogResult = true;
                this.Close();
            }
        }

        private void RequestEmail_Click(object sender, MouseButtonEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "mailto:sales@ruknbim.com?subject=RUKN Insight Pro License Inquiry",
                    UseShellExecute = true
                });
            }
            catch { }
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

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}
