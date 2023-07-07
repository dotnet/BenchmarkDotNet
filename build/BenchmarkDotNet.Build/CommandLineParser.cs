using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Cake.Frosting;

namespace BenchmarkDotNet.Build;

public class CommandLineParser
{
    private const string ScriptName = "build.cmd";

    public static readonly CommandLineParser Instance = new();

    public string[]? Parse(string[]? args)
    {
        if (args == null || args.Length == 0 || (args.Length == 1 && Is(args[0], "help", "--help", "-h")))
        {
            PrintHelp();
            return null;
        }

        if (Is(args[0], "cake"))
            return args.Skip(1).ToArray();

        var argsToProcess = new Queue<string>(args);

        var taskName = argsToProcess.Dequeue();
        if (Is(taskName, "-t", "--target") && argsToProcess.Any())
            taskName = argsToProcess.Dequeue();

        taskName = taskName.Replace("-", "");

        var taskNames = GetTaskNames();
        if (!taskNames.Contains(taskName))
        {
            PrintError($"'{taskName}' is not a task");
            return null;
        }

        if (argsToProcess.Count == 1 && Is(argsToProcess.Peek(), "-h", "--help"))
        {
            PrintTaskHelp(taskName);
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
                if (Is(arg, option.ShortName, option.FullName))
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
            "--exclusive"),
        new("-h",
            "--help",
            "",
            "Prints help information for the target task",
            "")
    };

    private void PrintHelp()
    {
        WriteHeader("Description:");

        WritePrefix();
        WriteLine("BenchmarkDotNet build script");

        WritePrefix();
        WriteLine("Task names are case-insensitive, dashes are ignored");

        WriteLine();

        WriteHeader("Usage:");

        WritePrefix();
        Write(ScriptName + " ");
        WriteTask("<TASK> ");
        WriteOption("[OPTIONS]");
        WriteLine();

        WriteLine();

        WriteHeader("Examples:");

        WritePrefix();
        Write(ScriptName + " ");
        WriteTask("restore");
        WriteLine();

        WritePrefix();
        Write(ScriptName + " ");
        WriteTask("build ");
        WriteOption("/p:");
        WriteArg("Configuration");
        WriteOption("=");
        WriteArg("Debug");
        WriteLine();

        WritePrefix();
        Write(ScriptName + " ");
        WriteTask("pack ");
        WriteOption("/p:");
        WriteArg("Version");
        WriteOption("=");
        WriteArg("0.1.1729-preview");
        WriteLine();

        WritePrefix();
        Write(ScriptName + " ");
        WriteTask("unittests ");
        WriteOption("--exclusive --verbosity ");
        WriteArg("Diagnostic");
        WriteLine();

        WritePrefix();
        Write(ScriptName + " ");
        WriteTask("docs-update ");
        WriteOption("/p:");
        WriteArg("Depth");
        WriteOption("=");
        WriteArg("3");
        WriteLine();

        WriteLine();

        PrintCommonOptions();

        WriteLine();

        WriteHeader("Tasks:");
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
    }

    private void PrintCommonOptions()
    {
        WriteLine("Options:", ConsoleColor.DarkCyan);

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
    }

    private void PrintTaskHelp(string taskName)
    {
        var taskType = typeof(BuildContext).Assembly
            .GetTypes()
            .Where(type => type.IsSubclassOf(typeof(FrostingTask<BuildContext>)) && !type.IsAbstract)
            .First(type => Is(type.GetCustomAttribute<TaskNameAttribute>()?.Name, taskName));
        taskName = taskType.GetCustomAttribute<TaskNameAttribute>()!.Name;
        var taskDescription = taskType.GetCustomAttribute<TaskDescriptionAttribute>()?.Description ?? "";
        var taskInstance = Activator.CreateInstance(taskType);
        var helpInfo = taskInstance is IHelpProvider helpProvider ? helpProvider.GetHelp() : new HelpInfo();

        WriteHeader("Description:");

        WritePrefix();
        WriteLine($"Task '{taskName}'");
        if (!string.IsNullOrWhiteSpace(taskDescription))
        {
            WritePrefix();
            WriteLine(taskDescription);
        }

        foreach (var line in helpInfo.Description)
        {
            WritePrefix();
            WriteLine(line);
        }

        WriteLine();

        WriteHeader("Usage:");

        WritePrefix();
        Write(ScriptName + " ");
        WriteTask(taskName + " ");
        WriteOption("[OPTIONS]");
        WriteLine();

        WriteLine();

        WriteHeader("Examples:");

        WritePrefix();
        Write(ScriptName + " ");
        WriteTask(taskName);
        WriteLine();

        if (taskName.StartsWith("docs", StringComparison.OrdinalIgnoreCase))
        {
            WritePrefix();
            Write(ScriptName + " ");
            WriteTask("docs-" + taskName[4..].ToLowerInvariant());
            WriteLine();
        }
        else
        {
            WritePrefix();
            Write(ScriptName + " ");
            WriteTask(taskName.ToLowerInvariant());
            WriteLine();
        }

        WriteLine();

        PrintCommonOptions();
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

    private static bool Is(string? arg, params string[] values) =>
        values.Any(value => value.Equals(arg, StringComparison.OrdinalIgnoreCase));

    private void PrintError(string text)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.Error.WriteLine("ERROR: " + text);
        Console.WriteLine();
        Console.ResetColor();
        PrintHelp();
    }

    private void WritePrefix() => Write("    ");
    private void WriteTask(string message) => Write(message, ConsoleColor.Green);
    private void WriteOption(string message) => Write(message, ConsoleColor.Blue);
    private void WriteArg(string message) => Write(message, ConsoleColor.DarkYellow);
    private void WriteObsolete(string message) => Write(message, ConsoleColor.Gray);

    private void WriteHeader(string message)
    {
        WriteLine(message, ConsoleColor.DarkCyan);
    }

    private void Write(string message, ConsoleColor? color = null)
    {
        if (color != null)
            Console.ForegroundColor = color.Value;
        Console.Write(message);
        if (color != null)
            Console.ResetColor();
    }

    private void WriteLine(string message = "", ConsoleColor? color = null)
    {
        Write(message, color);
        Console.WriteLine();
    }
}