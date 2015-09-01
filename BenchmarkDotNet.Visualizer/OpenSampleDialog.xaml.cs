using System.Linq;
using System.Reflection;
using System.Windows;

namespace BenchmarkDotNet.Visualizer
{
    public partial class OpenSampleDialog : Window
    {
        public OpenSampleDialog()
        {
            InitializeComponent();
            const string prefix = "BenchmarkDotNet.Visualizer.Samples";
            SamplesListBox.ItemsSource = Assembly.GetExecutingAssembly().GetManifestResourceNames().
                Where(name => name.StartsWith(prefix)).
                Select(name => name.Substring(prefix.Length + 1));
        }

        private void OnOkButtonClick(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        public string SampleName { get; set; }
    }
}
