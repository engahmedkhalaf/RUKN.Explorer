namespace RUKN.Search.Plugin
{
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;
    
    /// <summary>
    /// Feedback Window.
    /// </summary>
    public partial class FeedbackWindow : Window
    {
        public FeedbackWindow()
        {
            this.InitializeComponent();
            this.DataContext = this;
        }


        public static FeedbackWindow Create()
        {
            var progressBar = new FeedbackWindow();
            progressBar.Show();
            return progressBar;
        }

        private void Border_MouseDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        private void CancelProcess_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Title_Link(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://Rukn.com/contact/");
        }
        private void Leia_Link(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://Rukn.com/leia/");
        }
    }
}
