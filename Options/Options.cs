using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
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
        //public static String           _csvFile;
        //public static String           _logFile;

        public Options(String[] args)
        {
            FollowJunctions = false;
            BeQuiet = false;

            Exit = !ParseParameters(args);
        }

        public Boolean Exit
        { get; private set; }

        private Boolean ParseParameters(string[] args)
        {
            // no parameters
            if (args.Count() == 0)
            {
                DumpHelp();
                return false;
            }
            if (args.Count() < 2)
                throw new ArgumentException("Not enough arguments");

            Queue<String> parameters = new Queue<string>();
            foreach (String argument in args)
                parameters.Enqueue(argument);

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
            return true;
        }

        private static void DumpHelp()
        {
            Console.WriteLine(@"Usage:");
            Console.WriteLine(@"  SizeReporter.exe [options] <basedirectory> <maxdepth>");
            Console.WriteLine();
            Console.WriteLine(@"  The tool generates the CSV report and error log at current location");
            Console.WriteLine();
            Console.WriteLine(@"Options:");
            Console.WriteLine(@"--help:      display help and exit");
            Console.WriteLine(@"--version:   display version information and exit");
            Console.WriteLine(@"--junctions: include contents linked over junctions/reparse points");
            Console.WriteLine(@"--quiet:     do not display anything to the console");
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
