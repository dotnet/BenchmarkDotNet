using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace ChangeLogBuilder
{
    public static class Program
    {
        public static async Task Main(string[] args)
        {
            try
            {
                var config = new Config(args);
                var releaseNotes = await MarkdownBuilder.Build(config);
                File.WriteAllText(config.CurrentMilestone + ".md", releaseNotes);
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e.Demystify().ToString());
            }
        }
    }
}