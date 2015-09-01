using System;
using System.Drawing;
using BenchmarkDotNet.Logging;
using FastColoredTextBoxNS;

namespace BenchmarkDotNet.Visualizer
{
    public class FctbLogger : IBenchmarkLogger
    {
        private readonly FastColoredTextBox textBox;

        private readonly static Brush backgroundBrush = Brushes.Black;
        private readonly static Style Default = new TextStyle(Brushes.Gray, backgroundBrush, FontStyle.Regular);
        private readonly static Style Help = new TextStyle(Brushes.DarkGreen, backgroundBrush, FontStyle.Regular);
        private readonly static Style Header = new TextStyle(Brushes.Magenta, backgroundBrush, FontStyle.Regular);
        private readonly static Style Result = new TextStyle(Brushes.DarkCyan, backgroundBrush, FontStyle.Regular);
        private readonly static Style Statistic = new TextStyle(Brushes.Cyan, backgroundBrush, FontStyle.Regular);
        private readonly static Style Info = new TextStyle(Brushes.DarkSalmon, backgroundBrush, FontStyle.Regular);
        private readonly static Style Error = new TextStyle(Brushes.Red, backgroundBrush, FontStyle.Regular);

        public FctbLogger(FastColoredTextBox textBox)
        {
            this.textBox = textBox;
        }

        public void Write(BenchmarkLogKind logKind, string format, params object[] args)
        {
            textBox.Invoke(new Action(() => TextBoxWrite(logKind, format, args)));
        }

        public void TextBoxWrite(BenchmarkLogKind logKind, string format, params object[] args)
        {
            var text = string.Format(EnvironmentHelper.MainCultureInfo, format, args);
            switch (logKind)
            {
                case BenchmarkLogKind.Default:
                    textBox.AppendText(text, Default);
                    break;
                case BenchmarkLogKind.Help:
                    textBox.AppendText(text, Help);
                    break;
                case BenchmarkLogKind.Header:
                    textBox.AppendText(text, Header);
                    break;
                case BenchmarkLogKind.Result:
                    textBox.AppendText(text, Result);
                    break;
                case BenchmarkLogKind.Statistic:
                    textBox.AppendText(text, Statistic);
                    break;
                case BenchmarkLogKind.Info:
                    textBox.AppendText(text, Info);
                    break;
                case BenchmarkLogKind.Error:
                    textBox.AppendText(text, Error);
                    break;
            }
        }
    }
}