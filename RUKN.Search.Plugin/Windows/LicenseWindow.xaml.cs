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
            LoadLicenseStatus();
        }

        private void LoadLicenseStatus()
        {
            try
            {
                MachineText.Text = System.Environment.MachineName;

                string licKey = SettingsConfig.GetValue("LicenseKey");
                string trialStartStr = SettingsConfig.GetValue("TrialStartDate");

                if (licKey == "RUKN-INSIGHT-PRO-PAID-KEY")
                {
                    // Paid Active License
                    StatusDot.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#22C55E"));
                    StatusText.Text = "Active";

                    EmailRow.Visibility = Visibility.Visible;
                    string email = SettingsConfig.GetValue("LicenseEmail");
                    EmailText.Text = string.IsNullOrEmpty(email) ? "user@ruknbim.com" : email;

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
                else if (!string.IsNullOrEmpty(trialStartStr) && DateTime.TryParse(trialStartStr, out DateTime trialStart))
                {
                    DateTime trialExpiry = trialStart.AddDays(14);
                    int daysLeft = (trialExpiry.Date - DateTime.Now.Date).Days;

                    if (daysLeft >= 0)
                    {
                        // Active Trial License
                        StatusDot.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#22C55E"));
                        StatusText.Text = "Active (Trial)";

                        EmailRow.Visibility = Visibility.Visible;
                        string email = SettingsConfig.GetValue("LicenseEmail");
                        EmailText.Text = string.IsNullOrEmpty(email) ? "trial@ruknbim.com" : email;

                        DaysRow.Visibility = Visibility.Visible;
                        DaysText.Text = $"{daysLeft} days remaining";

                        TypeRow.Visibility = Visibility.Visible;
                        TypeText.Text = "14-Day Free Trial";

                        ExpiryRow.Visibility = Visibility.Visible;
                        ExpiryLabel.Text = "Trial Expiry";
                        ExpiryText.Text = trialExpiry.ToString("yyyy-MM-dd");

                        KeyRow.Visibility = Visibility.Collapsed;
                        MachineRow.Visibility = Visibility.Visible;

                        ActivationPanel.Visibility = Visibility.Visible; // Let them activate key if they want
                        SignOutPanel.Visibility = Visibility.Visible;
                        ActivePanel.Visibility = Visibility.Collapsed;
                    }
                    else
                    {
                        // Expired Trial
                        StatusDot.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EF4444"));
                        StatusText.Text = "Trial Expired";

                        EmailRow.Visibility = Visibility.Visible;
                        string email = SettingsConfig.GetValue("LicenseEmail");
                        EmailText.Text = string.IsNullOrEmpty(email) ? "trial@ruknbim.com" : email;

                        DaysRow.Visibility = Visibility.Collapsed;
                        TypeRow.Visibility = Visibility.Visible;
                        TypeText.Text = "14-Day Free Trial (Expired)";

                        ExpiryRow.Visibility = Visibility.Visible;
                        ExpiryLabel.Text = "Expired On";
                        ExpiryText.Text = trialExpiry.ToString("yyyy-MM-dd");

                        KeyRow.Visibility = Visibility.Collapsed;
                        MachineRow.Visibility = Visibility.Visible;

                        ActivationPanel.Visibility = Visibility.Visible;
                        SignOutPanel.Visibility = Visibility.Visible;
                        ActivePanel.Visibility = Visibility.Collapsed;
                    }
                }
                else
                {
                    // No license / Unlicensed
                    ShowInactiveLicense();
                }
            }
            catch 
            {
                ShowInactiveLicense();
            }
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
                SignOutPanel.Visibility = Visibility.Collapsed;
                ActivePanel.Visibility = Visibility.Collapsed;
            }
            catch { }
        }

        private async void BtnActivate_Click(object sender, RoutedEventArgs e)
        {
            string enteredKey = KeyInput.Text.Trim();
            if (string.IsNullOrEmpty(enteredKey))
            {
                MessageBox.Show("Please enter a valid license key.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string email = SettingsConfig.GetValue("LicenseEmail");
            if (string.IsNullOrEmpty(email))
            {
                email = "user@ruknbim.com";
            }

            string machineName = System.Environment.MachineName;

            StatusText.Text = "Activating Online...";
            BtnActivate.IsEnabled = false;

            var result = await RUKN.Search.Plugin.Utils.SupabaseService.ActivateLicenseAsync(enteredKey, email, machineName);

            BtnActivate.IsEnabled = true;

            if (result.Success)
            {
                SettingsConfig.SetValue("LicenseKey", enteredKey);
                SettingsConfig.SetValue("LicenseEmail", email);

                MessageBox.Show("License successfully activated on this machine!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                LoadLicenseStatus();
            }
            else
            {
                MessageBox.Show($"License validation failed:\n{result.Message}", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                LoadLicenseStatus();
            }
        }

        private void BtnDeactivate_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Are you sure you want to deactivate the license on this machine?", "Deactivate License", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                SettingsConfig.SetValue("LicenseKey", "");
                SettingsConfig.SetValue("LicenseEmail", "");
                LoadLicenseStatus();
            }
        }

        private void BtnSignOut_Click(object sender, RoutedEventArgs e)
        {
            SettingsConfig.SetValue("TrialStartDate", "");
            SettingsConfig.SetValue("LicenseEmail", "");
            LoadLicenseStatus();
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
