using System;
using System.IO;
using System.Globalization;
using System.Collections.Generic;
using System.Text;
using System.Reflection;

namespace SizeReporter.Options
{
    internal class Options
    {
        public DirectoryInfo Directory { get; private set; }
        public Int32 MaxDepth { get; private set; }
        public Boolean FollowJunctions { get; private set; }
        public Boolean BeQuiet { get; private set; }
        public Boolean ReportEmpty { get; private set; }
        public Boolean Tsv { get; private set; }
        public Boolean Xml { get; private set; }
        public CultureInfo Culture { get; private set; }
        //public static String           _csvFile;
        //public static String           _logFile;

        public Options(String[] args)
        {
            FollowJunctions = false;
            BeQuiet = false;
            ReportEmpty = false;

            Exit = !ParseParameters(args);
        }

        public Boolean Exit
        { get; private set; }

        private Boolean ParseParameters(string[] args)
        {
            // no parameters
            if (args.Length == 0)
            {
                DumpHelp();
                return false;
            }

            Queue<String> parameters = new Queue<string>();
            foreach (String argument in args)
                parameters.Enqueue(argument);

            String culture = null;
            while (parameters.Peek().StartsWith("--"))
            {
                String parameter = parameters.Dequeue();
                switch (parameter)
                {
                    case "--help":
                        DumpHelp();
                        return false;
                    case "--version":
                        DumpVersion();
                        return false;
                    case "--junctions":
                        FollowJunctions = true;
                        break;
                    case "--quiet":
                        BeQuiet = true;
                        break;
                    case "--xml":
                        Xml = true;
                        break;
                    case "--empty":
                        ReportEmpty = true;
                        break;
                    case "--tsv":
                        Tsv = true;
                        break;
                    case "--culture":
                        culture = parameters.Dequeue();
                        break;
                    default:
                        throw new ArgumentException(String.Format("Unknown option {0}", parameter));
                }
            }

            if (parameters.Count < 2)
                throw new ArgumentException("Not enough arguments after parsing the options");
            if (parameters.Count > 2)
                throw new ArgumentException("Too many arguments left after parsing the options");

            String path = parameters.Dequeue();
            Directory = new DirectoryInfo(path);
            if (!Directory.Exists)
                throw new DirectoryNotFoundException(String.Format("Directory \"{0}\" not found", path));

            Int32 maxDepth;
            String depth = parameters.Dequeue();
            if (!Int32.TryParse(depth, out maxDepth))
                throw new FormatException(String.Format("The maxdepth parameter \"{0}\" is not an integer!", depth));
            MaxDepth = maxDepth;
            if (culture != null)
                Culture = new CultureInfo(culture);
            return true;
        }

        private static void DumpHelp()
        {
            Console.WriteLine(@"Usage:");
            Console.WriteLine(@"  SizeReporter.exe [options] <basedirectory> <maxdepth>");
            Console.WriteLine();
            Console.WriteLine(@"  The tool generates a TSV/CSV report and error log at current location");
            Console.WriteLine();
            Console.WriteLine(@"Options:");
            Console.WriteLine(@"--culture:   use the specified culture ""en-US"" for example");
            Console.WriteLine(@"--empty:     make a list of all empty files (size 0)");
            Console.WriteLine(@"--help:      display help and exit");
            Console.WriteLine(@"--junctions: include contents linked over junctions/reparse points");
            Console.WriteLine(@"--quiet:     do not display anything to the console");
            Console.WriteLine(@"--tsv:       generate tab separated values (TSV) instead of default MS compatible CSV");
            Console.WriteLine(@"--xml:       generate XML output");
            Console.WriteLine(@"--version:   display version information and exit");
            Console.WriteLine();
            Console.WriteLine(@"Example:");
            Console.WriteLine(@"  SizeReporter.exe ""C:\Documents and Settings"" 3");
            Console.WriteLine();
            DumpVersion();
        }

        private static void DumpVersion()
        {
            Console.WriteLine(@"Version: {0}", Assembly.GetExecutingAssembly().GetName().Version);
            Console.WriteLine(@"Author:");
            Console.WriteLine(@"  Jaap de Haan <jaap.dehaan@color-of-code.de>");
            Console.WriteLine(@"  http://www.color-of-code.de");
        }

    }
}
