using System.Windows;

namespace BenchmarkDotNet.Visualizer
{
    public partial class OpenUrlDialog : Window
    {
        public OpenUrlDialog()
        {
            InitializeComponent();
        }

        private void OnOkButtonClick(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        public string Url { get; set; }
    }
}
