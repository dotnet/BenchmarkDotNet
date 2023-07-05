using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Cake.Frosting;

namespace BenchmarkDotNet.Build;

public class CommandLineParser
{
    public static readonly CommandLineParser Instance = new();

    private record Option(string ShortName, string FullName, string Arg, string Description, string CakeOption);

    private readonly Option[] options =
    {
        new("-v",
            "--verbosity",
            "<LEVEL>",
            "Specifies the amount of information to be displayed\n(Quiet, Minimal, Normal, Verbose, Diagnostic)",
            "--verbosity"),
        new("-e",
            "--exclusive",
            "",
            "Executes the target task without any dependencies",
            "--exclusive")
    };

    private void PrintHelp(bool skipWelcome = false)
    {
        const string scriptName = "build.cmd";
        if (!skipWelcome)
        {
            WriteHeader("Welcome to the BenchmarkDotNet build script!");
            WriteLine();
        }

        WriteHeader("USAGE:");

        WritePrefix();
        Write(scriptName + " ");
        WriteTask("<TASK> ");
        WriteOption("[OPTIONS]");
        WriteLine();

        WriteLine();

        WriteHeader("EXAMPLES:");

        WritePrefix();
        Write(scriptName + " ");
        WriteTask("restore");
        WriteLine();

        WritePrefix();
        Write(scriptName + " ");
        WriteTask("build ");
        WriteOption("/p:");
        WriteArg("Configuration");
        WriteOption("=");
        WriteArg("Debug");
        WriteLine();

        WritePrefix();
        Write(scriptName + " ");
        WriteTask("pack ");
        WriteOption("/p:");
        WriteArg("Version");
        WriteOption("=");
        WriteArg("0.1.1729-preview");
        WriteLine();
        
        WritePrefix();
        Write(scriptName + " ");
        WriteTask("unittests ");
        WriteOption("--exclusive --verbosity ");
        WriteArg("Diagnostic");
        WriteLine();

        WritePrefix();
        Write(scriptName + " ");
        WriteTask("docsupdate ");
        WriteOption("/p:");
        WriteArg("Depth");
        WriteOption("=");
        WriteArg("3");
        WriteLine();

        WriteLine();

        WriteLine("OPTIONS:", ConsoleColor.DarkCyan);

        var shortNameWidth = options.Max(it => it.ShortName.Length);
        var targetWidth = options.Max(it => it.FullName.Length + it.Arg.Length);

        foreach (var (shortName, fullName, arg, description, _) in options)
        {
            WritePrefix();
            WriteOption(shortName.PadRight(shortNameWidth));
            WriteOption(shortName != "" ? "," : " ");
            WriteOption(fullName);
            Write(" ");
            WriteArg(arg);
            Write(new string(' ', targetWidth - fullName.Length - arg.Length + 3));
            var descriptionLines = description.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
            Write(descriptionLines.FirstOrDefault() ?? "");
            for (int i = 1; i < descriptionLines.Length; i++)
            {
                WriteLine();
                WritePrefix();
                Write(new string(' ', shortNameWidth + 2 + targetWidth + 3));
                Write(descriptionLines[i]);
            }

            WriteLine();
        }
        
        WritePrefix();
        WriteOption("/p:");
        WriteArg("<KEY>");
        WriteOption("=");
        WriteArg("<VALUE>");
        Write(new string(' ', targetWidth + shortNameWidth - 11));
        Write("Passes custom properties to MSBuild");
        WriteLine();

        WriteLine();

        WriteHeader("TASKS:");
        var taskWidth = GetTaskNames().Max(name => name.Length) + 3;
        foreach (var (taskName, taskDescription) in GetTasks())
        {
            if (taskName.Equals("Default", StringComparison.OrdinalIgnoreCase))
                continue;

            if (taskDescription.StartsWith("OBSOLETE", StringComparison.OrdinalIgnoreCase))
            {
                WriteObsolete("    " + taskName.PadRight(taskWidth));
                WriteObsolete(taskDescription);
            }
            else
            {
                WriteTask("    " + taskName.PadRight(taskWidth));
                Write(taskDescription);
            }

            WriteLine();
        }

        return;

        void WritePrefix() => Write("    ");
        void WriteTask(string message) => Write(message, ConsoleColor.Green);
        void WriteOption(string message) => Write(message, ConsoleColor.Blue);
        void WriteArg(string message) => Write(message, ConsoleColor.DarkYellow);
        void WriteObsolete(string message) => Write(message, ConsoleColor.Gray);

        void WriteHeader(string message)
        {
            WriteLine(message, ConsoleColor.DarkCyan);
        }

        void Write(string message, ConsoleColor? color = null)
        {
            if (color != null)
                Console.ForegroundColor = color.Value;
            Console.Write(message);
            if (color != null)
                Console.ResetColor();
        }

        void WriteLine(string message = "", ConsoleColor? color = null)
        {
            Write(message, color);
            Console.WriteLine();
        }
    }

    private static HashSet<string> GetTaskNames()
    {
        return GetTasks().Select(task => task.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private static List<(string Name, string Description)> GetTasks()
    {
        return typeof(BuildContext).Assembly
            .GetTypes()
            .Where(type => type.IsSubclassOf(typeof(FrostingTask<BuildContext>)) && !type.IsAbstract)
            .Select(type => (
                Name: type.GetCustomAttribute<TaskNameAttribute>()?.Name ?? "",
                Description: type.GetCustomAttribute<TaskDescriptionAttribute>()?.Description ?? ""
            ))
            .Where(task => task.Name != "")
            .ToList();
    }


    public string[]? Parse(string[]? args)
    {
        if (args == null || args.Length == 0)
        {
            PrintHelp();
            return null;
        }

        if (args.Length == 1)
        {
            if (IsOneOf(args[0], "help"))
            {
                PrintHelp();
                return null;
            }

            if (IsOneOf(args[0], "help-cake"))
            {
                new CakeHost().UseContext<BuildContext>().Run(new[] { "--help" });
                return null;
            }
        }

        var argsToProcess = new Queue<string>(args);

        var taskName = argsToProcess.Dequeue();
        if (IsOneOf(taskName, "-t", "--target") && argsToProcess.Any())
            taskName = argsToProcess.Dequeue();

        var taskNames = GetTaskNames();
        if (!taskNames.Contains(taskName))
        {
            PrintError($"'{taskName}' is not a task");
            return null;
        }

        var cakeArgs = new List<string>
        {
            "--target",
            taskName
        };
        while (argsToProcess.Any())
        {
            var arg = argsToProcess.Dequeue();

            var matched = false;
            foreach (var option in options)
            {
                if (IsOneOf(arg, option.ShortName, option.FullName))
                {
                    matched = true;
                    cakeArgs.Add(option.CakeOption);
                    if (option.Arg != "")
                    {
                        if (!argsToProcess.Any())
                        {
                            PrintError(option.FullName + " is not specified");
                            return null;
                        }

                        cakeArgs.Add(argsToProcess.Dequeue());
                    }
                }
            }

            if (arg.StartsWith("/p:"))
            {
                matched = true;
                cakeArgs.Add("--msbuild");
                cakeArgs.Add(arg[3..]);
            }

            if (!matched)
            {
                PrintError("Unknown option: " + arg);
                return null;
            }
        }

        return cakeArgs.ToArray();
    }

    bool IsOneOf(string arg, params string[] values) =>
        values.Any(value => value.Equals(arg, StringComparison.OrdinalIgnoreCase));

    void PrintError(string text)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.Error.WriteLine("ERROR: " + text);
        Console.WriteLine();
        Console.ResetColor();
        PrintHelp(true);
    }
}