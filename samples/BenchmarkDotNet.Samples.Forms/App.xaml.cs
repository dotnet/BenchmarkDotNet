using System;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace BenchmarkDotNet.Samples.Forms
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            MainPage = new MainPage();
        }
    }
}
