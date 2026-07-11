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

            SettingsConfig.SetValue("TrialStartDate", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            SettingsConfig.SetValue("LicenseEmail", email);

            // Best-effort: mirror the trial into Supabase so it's visible alongside paid licenses.
            // The local trial clock above already started, so a network failure here doesn't block anything.
            await RUKN.Search.Plugin.Utils.SupabaseLicensing.RegisterTrialAsync(email, System.Environment.MachineName);

            MessageBox.Show("14-day Free Trial started successfully!", "Trial Activated", MessageBoxButton.OK, MessageBoxImage.Information);
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
            if (SettingsConfig.GetValue("LicenseKey") == "RUKN-INSIGHT-PRO-PAID-KEY")
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
