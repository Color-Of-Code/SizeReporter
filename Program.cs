using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SizeReporter
{
    class Program
    {
        private static DirectoryInfo _directory;
        private static Int32 _maxDepth;
        private static UInt32 _clusterSize;
        private static TextWriter _streamResult;
        private static TextWriter _streamErrors;
        private static int _startCharPos;
        private static Boolean _followJunctions = false;

        static void Main(string[] args)
        {
            if (args.Count() != 2)
            {
                Console.WriteLine("SizeReporter.exe <basedirectory> <maxdepth>");
                Console.WriteLine(@"example: SizeReporter.exe ""C:\Documents and Settings"" 3");
                Console.WriteLine(@"   generates the report at current location");
                return;
            }

            try
            {
                _directory = new DirectoryInfo(args[0]);
                _maxDepth = Int32.Parse(args[1]);

                _clusterSize = DetermineClusterSize(_directory.Root.FullName);

                String timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
                String filename1 = String.Format("sizereport_result_{0}.csv", timestamp);
                String filename2 = String.Format("sizereport_errors_{0}.log", timestamp);
                using (_streamResult = File.CreateText(filename1))
                {
                    Console.WriteLine("Generating result into {0}", filename1);
                    using (_streamErrors = File.CreateText(filename2))
                    {
                        Console.WriteLine("Reporting errors into {0}", filename2);
                        //_stream.WriteLine("Depth\tFiles\tDirs\tVirtual size\tSize on disk\tPath");
                        _streamResult.WriteLine("Depth\tFiles\tDirs\tVirtual size (MB)\tSize on disk (MB)\tLast modification\tRelative Path");
                        String path = FileUtil.GetLongEscapedPathname(_directory.FullName);
                        _startCharPos = path.Length;
                        PathStatistics total = Process(path, 0);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Error.WriteLine(ex.Message);
                Console.Error.WriteLine(ex.StackTrace);
                Console.ResetColor();
            }
        }

        private static PathStatistics Process(String directory, Int32 depth)
        {
            PathStatistics stats = new PathStatistics();
            if (depth <= _maxDepth)
            {
                OutputCurrentPosition(directory);
            }
            foreach (String directorypath in FileUtil.FindDirectoriesSorted(directory, _followJunctions))
            {
                try
                {
                    stats += Process(directorypath, depth + 1);
                    stats.DirectoryCount++;
                }
                catch (Exception exception)
                {
                    DumpError(directorypath, exception);
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
                        DumpWarning(filepath, 
                            String.Format(" -> compressed size > real size! ({0}>{1})",
                            csize, vsize));
                    }

                    UInt64 clusters = (csize + _clusterSize - 1) / _clusterSize;
                    stats.VirtualSize += vsize;
                    stats.SizeOnDisk  += clusters * _clusterSize;
                    if (lastModified > DateTime.Now.AddDays(1))
                    {
                        DumpWarning(filepath, 
                            String.Format(" -> date of last modification lies in future (ignored)! ({0})",
                            lastModified.ToString("yyyy-MM-dd HH:mm:ss")));
                    }
                    else
                    {
                        stats.RefreshLastModified(lastModified);
                    }
                    stats.FileCount++;
                }
                catch (Exception exception)
                {
                    DumpError(filepath, exception);
                }
            }

            if (depth <= _maxDepth)
            {
                OutputResultLine(directory, depth, stats);
            }
            return stats;
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
            int maxlen = Console.WindowWidth - 1;
            String value = String.Format("> .{0}", directory.Substring(_startCharPos));
            if (value.Length > maxlen)
                value = value.Substring(0, maxlen);
            value = value.PadRight(maxlen);
            Console.Write("\r{0}", value);
        }

        private static void OutputResultLine(String directory, Int32 depth, PathStatistics stats)
        {
            String resultLine = String.Format("{0}\t{1}\t{2}\t{3:0.000}\t{4:0.000}\t{5:yyyy-MM-dd HH:mm:ss}\t.{6}",
                depth,
                stats.FileCount, stats.DirectoryCount,
                stats.VirtualSizeMb, stats.SizeOnDiskMb,
                stats.LastChange,
                directory.Substring(_startCharPos));
            ClearConsoleLine();
            Console.WriteLine(resultLine);
            _streamResult.WriteLine(resultLine);
        }

        private static uint DetermineClusterSize(String rootDirectory)
        {
            uint clustersize = 0;

            DriveInfo[] allDrives = DriveInfo.GetDrives();
            foreach (DriveInfo d in allDrives)
            {
                if (d.RootDirectory.FullName == _directory.Root.FullName)
                {
                    clustersize = FileUtil.GetClusterSize(rootDirectory);
                }
            }

            if (clustersize == 0)
            {
                clustersize = 4096;
                Console.WriteLine("Cluster size could not be detected, defaulting to {0}", clustersize);
            }
            else
            {
                Console.WriteLine("Cluster size: {0}", clustersize);
            }

            return clustersize;
        }

        private static void DumpError(String filepath, Exception exception)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.WriteLine();
            _streamErrors.WriteLine("skipping .{0}:", filepath.Substring(_startCharPos));
            Console.Error.WriteLine("skipping .{0}:", filepath.Substring(_startCharPos));
            _streamErrors.WriteLine(" -> {0}", exception.Message);
            Console.Error.WriteLine(" -> {0}", exception.Message);
            Console.ResetColor();
        }

        private static void DumpWarning(String filepath, String text)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Error.WriteLine();
            _streamErrors.WriteLine("warning .{0}:", filepath.Substring(_startCharPos));
            Console.Error.WriteLine("warning .{0}:", filepath.Substring(_startCharPos));
            _streamErrors.WriteLine(text);
            Console.Error.WriteLine(text);
            Console.ResetColor();
        }
    }
}
