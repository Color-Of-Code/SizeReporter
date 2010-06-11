using System;
using System.Globalization;
using System.Reflection;
using System.Threading;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SizeReporter
{
    class Program
    {
        private static Options.Options  _options;
        private static DateTime         _timeStart;
        private static DateTime         _timeEnd;
        private static List<String>     _emptyFiles;

        private static UInt32 _clusterSize;
        private static int _startCharPos;

        private static Output.LogOutput _log;
        private static Output.IResultWriter _report;
        
        static void Main(string[] args)
        {
            try
            {
                _options = new Options.Options(args);
                if (_options.Exit) 
                    return;

                if (_options.ReportEmpty)
                    _emptyFiles = new List<string>();

                if (_options.Culture != null)
                    Thread.CurrentThread.CurrentCulture = _options.Culture;

                _clusterSize = DetermineClusterSize(_options.Directory.Root.FullName);

                _timeStart = DateTime.Now;
                String timestamp = _timeStart.ToString("yyyyMMdd-HHmmss");
                
                String extension = "csv";
                if (_options.Xml)
                    extension = "xml";
                String filename1 = String.Format("sizereport_result_{0}.{1}", timestamp, extension);

                String filename2 = String.Format("sizereport_errors_{0}.log", timestamp); ;
                using (TextWriter _streamResult = File.CreateText(filename1))
                {
                    using (TextWriter streamErrors = File.CreateText(filename2))
                    {
                        PerformJob(_streamResult, streamErrors, filename1, filename2);
                    }
                }

                if (_options.ReportEmpty)
                {
                    String filename = String.Format("sizereport_emptyfiles_{0}.log", timestamp);
                    using (TextWriter stream = File.CreateText(filename))
                    {
                        foreach (String file in _emptyFiles)
                            stream.WriteLine(file);
                    }
                }
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

        private static void PerformJob(TextWriter streamResult, TextWriter streamErrors,
            String filename1, String filename2)
        {
            String path = FileUtil.GetLongEscapedPathname(_options.Directory.FullName);
            _startCharPos = path.Length;

            _log = new Output.LogOutput(streamErrors, path.Length, _options.BeQuiet);
            if (_options.Xml)
                _report = new Output.XmlResultOutput(streamResult, path.Length, _options.BeQuiet);
            else
                _report = new Output.CsvResultOutput(streamResult, path.Length, _options.BeQuiet, _options.Tsv);

            _log.LogInfo("Start at {0:yyyy-MM-dd HH:mm:ss}", _timeStart);
            ConsoleWrite("Generating result into {0}", filename1);
            ConsoleWrite("Reporting errors into {0}", filename2);
            
            _report.ReportHeader();
            PathStatistics total = Process(path, 0);

            _timeEnd = DateTime.Now;
            _log.LogInfo("End at {0:yyyy-MM-dd HH:mm:ss}", _timeEnd);
            TimeSpan duration = _timeEnd - _timeStart;
            _log.LogInfo("Duration: {0}", duration);
        }

        private static PathStatistics Process(String directory, Int32 depth)
        {
            PathStatistics stats = new PathStatistics();
            RefreshLastModified(ref stats, directory);
            if (depth <= _options.MaxDepth)
            {
                OutputCurrentPosition(directory);
            }
            foreach (String directorypath in FileUtil.FindDirectoriesSorted(directory, _options.FollowJunctions))
            {
                try
                {
                    stats += Process(directorypath, depth + 1);
                    RefreshLastModified(ref stats, directorypath);
                    stats.DirectoryCount++;
                }
                catch (Exception exception)
                {
                    _log.LogError(directorypath, exception);
                }
            }

            foreach (String filepath in FileUtil.FindFilesSorted(directory))
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
                    stats.SizeOnDisk  += clusters * _clusterSize;
                    UpdateLastModified(ref stats, filepath, ref lastModified);
                    stats.FileCount++;
                }
                catch (Exception exception)
                {
                    _log.LogError(filepath, exception);
                }
            }

            if (depth <= _options.MaxDepth)
            {
                _report.OutputResultLine(directory, depth, stats);
            }
            return stats;
        }

        private static void UpdateLastModified(ref PathStatistics stats, String filepath, ref DateTime lastModified)
        {
            if (lastModified > DateTime.Now.AddDays(1))
            {
                _log.LogWarning(filepath,
                    String.Format(" -> date of last modification lies in future (ignored)! ({0})",
                    lastModified.ToString("yyyy-MM-dd HH:mm:ss")));
            }
            else
            {
                stats.RefreshLastModified(lastModified);
            }
        }

        private static void RefreshLastModified(ref PathStatistics stats, String directorypath)
        {
            DateTime lastModified;
            FileUtil.GetLastModified(directorypath, out lastModified);
            UpdateLastModified(ref stats, directorypath, ref lastModified);
        }

        private static void ClearConsoleLine()
        {
            int maxlen = Console.WindowWidth - 1;
            String value = String.Empty;
            value = value.PadRight(maxlen);
            Console.Write("\r{0}\r", value);
        }

        private static void OutputCurrentPosition(String directory)
        {
            if (!_options.BeQuiet)
            {
                int maxlen = Console.WindowWidth - 1;
                String value = String.Format("> .{0}", directory.Substring(_startCharPos));
                if (value.Length > maxlen)
                    value = value.Substring(0, maxlen);
                value = value.PadRight(maxlen);
                Console.Write("\r{0}", value);
            }
        }

        private static uint DetermineClusterSize(String rootDirectory)
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

        private static void ConsoleWrite(String format, params object[] args)
        {
            if (!_options.BeQuiet)
            {
                ClearConsoleLine();
                Console.WriteLine(format, args);
            }
        }
    }
}
