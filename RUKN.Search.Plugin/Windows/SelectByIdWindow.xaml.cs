using Autodesk.Navisworks.Gui.Roamer;
using RUKN.Search.Plugin.Utils;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;



namespace RUKN.Search.Plugin
{
    /// <summary>
    /// Interaction logic for SelectByIdWindow.xaml
    /// </summary>
    public partial class SelectByIdWindow : Window
    {
        private MainWindowViewModel MainWindowViewModel { get; set; }

        public static SelectByIdWindow MainView { get; set; }

        public SelectByIdWindow()
        {
            this.MainWindowViewModel = new MainWindowViewModel();
            this.DataContext = this.MainWindowViewModel;
            MainView = this;

            InitializeComponent();
            LabelVersion.Update(this.MainWindowViewModel);
        }

        private T FindChild<T>(DependencyObject parent, string childName) where T : DependencyObject
        {
            // Check if parent is null
            if (parent == null) return null;

            // Check if parent is the child we're looking for
            var frameworkElement = parent as FrameworkElement;
            if (frameworkElement != null && frameworkElement.Name == childName)
            {
                return parent as T;
            }

            // Recursively search for the child
            int numChildren = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < numChildren; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                var result = FindChild<T>(child, childName);
                if (result != null) return result;
            }

            return null;
        }

        private void OkButton_Click(object sender, EventArgs e)
        {
            var s = Tools.splitString(textBox.Text);
            Tools.getElements(s);
        }

        private void Close_Button(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Border_MouseDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        private void Title_Link(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://Rukn.com/");
        }

        private void Ruknbim_Link(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://www.ruknbim.com/");
        }

        private void TextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            TextBox textBox = (TextBox)sender;
            if (textBox.Text == "Insert Element Id")
            {
                textBox.Text = "";
            }
        }

        private void TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            TextBox textBox = (TextBox)sender;
            if (string.IsNullOrWhiteSpace(textBox.Text))
            {
                textBox.Text = "Insert Element Id";
            }
        }
    }
}
