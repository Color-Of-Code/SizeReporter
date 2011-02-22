using System;
using System.Globalization;
using System.Reflection;
using System.Threading;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SizeReporter
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                Program sizeReporter = new Program(args);
                sizeReporter.Run();
            }
            catch (Exception ex)
            {
                // FATAL exception, don't be quiet in this case
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Error.WriteLine(ex.Message);
                Console.Error.WriteLine(ex.StackTrace);
                Console.ResetColor();
            }
        }

        private Options.Options _options;
        private DateTime _timeStart;
        private DateTime _timeEnd;
        private List<Junction> _junctions;
        private List<String> _emptyFiles;

        private UInt32 _clusterSize;

        private Program(string[] args)
        {
            _options = new Options.Options(args);
        }

        private void Run()
        {
            if (_options.Exit)
                return;

            if (_options.ReportEmpty)
                _emptyFiles = new List<string>();

            if (_options.ListJunctions)
                _junctions = new List<Junction>();

            if (_options.Culture != null)
                Thread.CurrentThread.CurrentCulture = _options.Culture;

            _clusterSize = DetermineClusterSize(_options.Directory.Root.FullName);

            _timeStart = DateTime.Now;

            using (Output.IResultWriter result = Output.ResultWriterFactory.Build(_options))
            {
                using (Output.LogOutput log = new Output.LogOutput(_options))
                {
                    PerformJob(result, log);
                }
            }

            ReportEmptyFiles();
            ReportJunctions();
        }

        private void ReportEmptyFiles()
        {
            if (_options.ReportEmpty)
            {
                using (TextWriter stream = File.CreateText(_options.EmptyFilesFile))
                {
                    foreach (String file in _emptyFiles)
                        stream.WriteLine(file);
                }
            }
        }

        private void ReportJunctions()
        {
            if (_options.ListJunctions)
            {
                using (TextWriter stream = File.CreateText(_options.JunctionsFile))
                {
                    _junctions.Sort();
                    stream.WriteLine("Source;Target");
                    foreach (Junction junction in _junctions)
                        stream.WriteLine("\"{0}\";\"{1}\"",
                            junction.Source.Substring(_options.StartCharPos), junction.Target);
                }
            }
        }

        private void PerformJob(Output.IResultWriter writer, Output.LogOutput logOutput)
        {
            String path = FileUtil.GetLongEscapedPathname(_options.Directory.FullName);

            logOutput.LogInfo("Start at {0:yyyy-MM-dd HH:mm:ss}", _timeStart);
            ConsoleWrite("Generating result into {0}", writer.Name);
            ConsoleWrite("Reporting errors into {0}", logOutput.Name);

            writer.ReportHeader();
            PathStatistics total = Process(writer, logOutput, path, 0);

            _timeEnd = DateTime.Now;
            logOutput.LogInfo("End at {0:yyyy-MM-dd HH:mm:ss}", _timeEnd);
            TimeSpan duration = _timeEnd - _timeStart;
            logOutput.LogInfo("Duration: {0}", duration);
        }

        private PathStatistics Process(Output.IResultWriter writer, Output.LogOutput _log,
            String directory, Int32 depth)
        {
            PathStatistics stats = new PathStatistics();
            stats.Path = directory;
            stats.Depth = depth;

            RefreshLastModified(_log, ref stats, directory);
            if (depth <= _options.MaxDepth)
            {
                OutputCurrentPosition(directory);
            }

            if (_options.ListJunctions)
            {
                IList<Junction> junctions = FileUtil.FindJunctions(directory);
                _junctions.AddRange(junctions);
            }

            IList<String> directories = FileUtil.FindDirectoriesSorted(directory, _options.FollowJunctions);
            foreach (String directorypath in directories)
            {
                try
                {
                    stats += Process(writer, _log, directorypath, depth + 1);
                    RefreshLastModified(_log, ref stats, directorypath);
                    stats.DirectoryCount++;
                }
                catch (Exception exception)
                {
                    _log.LogError(directorypath, exception);
                }
            }

            IList<String> filepaths = FileUtil.FindFilesSorted(directory);
            foreach (String filepath in filepaths)
            {
                try
                {
                    string shortfilename = FileUtil.ToShortPathName(filepath);
                    UInt64 vsize;
                    DateTime lastModified;
                    FileUtil.GetFileSizeAndLastModified(shortfilename, out vsize, out lastModified);
                    UInt64 csize = FileUtil.GetCompressedFileSize(shortfilename);
                    if (csize > vsize)
                    {
                        _log.LogWarning(filepath,
                            String.Format(" -> compressed size > real size! ({0}>{1})",
                            csize, vsize));
                    }
                    if (_emptyFiles != null && vsize == 0)
                        _emptyFiles.Add(filepath);

                    UInt64 clusters = (csize + _clusterSize - 1) / _clusterSize;
                    stats.VirtualSize += vsize;
                    stats.SizeOnDisk += clusters * _clusterSize;
                    UpdateLastModified(_log, ref stats, filepath, ref lastModified);
                    stats.FileCount++;
                }
                catch (Exception exception)
                {
                    _log.LogError(filepath, exception);
                }
            }

            if (depth <= _options.MaxDepth)
            {
                if (_options.RemotePath)
                {
                    if (_junctions.Count > 0)
                    {
                        String sourcePath = stats.Path;
                        List<Junction> juncties = _junctions.FindAll(x => sourcePath.StartsWith(x.Source));
                        juncties.Sort();
                        if (juncties.Count > 0)
                        {
                            Junction shortest = juncties[0];
                            stats.RemotePath = stats.Path.Replace(shortest.Source, shortest.Target);
                        }
                    }
                    writer.OutputResultLine(stats, true);
                }
                else
                {
                    writer.OutputResultLine(stats, false);
                }
            }
            return stats;
        }

        private void UpdateLastModified(Output.LogOutput log, ref PathStatistics stats, String filepath, ref DateTime lastModified)
        {
            if (lastModified > DateTime.Now.AddDays(1))
            {
                log.LogWarning(filepath,
                    String.Format(" -> date of last modification lies in future (ignored)! ({0})",
                    lastModified.ToString("yyyy-MM-dd HH:mm:ss")));
            }
            else
            {
                stats.RefreshLastModified(lastModified);
            }
        }

        private void RefreshLastModified(Output.LogOutput log, ref PathStatistics stats, String directorypath)
        {
            DateTime lastModified;
            FileUtil.GetLastModified(directorypath, out lastModified);
            UpdateLastModified(log, ref stats, directorypath, ref lastModified);
        }

        private static void ClearConsoleLine()
        {
            int maxlen = Console.WindowWidth - 1;
            String value = String.Empty;
            value = value.PadRight(maxlen);
            Console.Write("\r{0}\r", value);
        }

        private void OutputCurrentPosition(String directory)
        {
            if (!_options.BeQuiet)
            {
                int maxlen = Console.WindowWidth - 1;
                String value = String.Format("> .{0}", directory.Substring(_options.StartCharPos));
                if (value.Length > maxlen)
                    value = value.Substring(0, maxlen);
                value = value.PadRight(maxlen);
                Console.Write("\r{0}", value);
            }
        }

        private uint DetermineClusterSize(String rootDirectory)
        {
            uint clustersize = 0;

            DriveInfo[] allDrives = DriveInfo.GetDrives();
            foreach (DriveInfo d in allDrives)
            {
                if (d.RootDirectory.FullName == _options.Directory.Root.FullName)
                {
                    clustersize = FileUtil.GetClusterSize(rootDirectory);
                }
            }

            if (clustersize == 0)
            {
                clustersize = 4096;
                ConsoleWrite("Cluster size could not be detected, defaulting to {0}", clustersize);
            }
            else
            {
                ConsoleWrite("Cluster size: {0}", clustersize);
            }

            return clustersize;
        }

        private void ConsoleWrite(String format, params object[] args)
        {
            if (!_options.BeQuiet)
            {
                ClearConsoleLine();
                Console.WriteLine(format, args);
            }
        }
    }
}
