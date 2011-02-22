using System;
using System.IO;
using System.Collections.Generic;
using System.Text;

namespace SizeReporter.Output
{
    internal class CsvResultOutput : IResultWriter
    {
        private Boolean _quiet = false;
        //private Boolean _verbose;
        private TextWriter _stream;
        private int _startCharPos;
        private String _separator;

        public CsvResultOutput(String filename, int startPos, Boolean quiet, Boolean tabSeparated)
        {
            Name = filename;
            _stream = File.CreateText(filename);
            if (tabSeparated)
            {
                _separator = "\t";
            }
            else
            {
                if (System.Globalization.CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator == ",")
                    _separator = ";";
                else
                    _separator = ",";
            }
            //_verbose = true;
            _quiet = quiet;
            _startCharPos = startPos;
        }

        public void ReportHeader()
        {
            _stream.WriteLine(
                "Depth{0}Files{0}Dirs{0}Virtual size (MB){0}Size on disk (MB){0}Last modification{0}Relative Path",
                _separator);
        }

        public void OutputResultLine(PathStatistics stats, bool includeRemotePath)
        {
            String resultLine = null;
            if (includeRemotePath)
            {
                String remotePath = stats.RemotePath ?? String.Empty;
                resultLine = String.Format(
                    "{1}{0}{2}{0}{3}{0}{4:0.000}{0}{5:0.000}{0}{6:yyyy-MM-dd HH:mm:ss}{0}\"{7}\"{0}\"{8}\"",
                    _separator, stats.Depth, stats.FileCount, stats.DirectoryCount,
                    stats.VirtualSizeMb, stats.SizeOnDiskMb,
                    stats.LastChange, stats.Path.Substring(_startCharPos), remotePath);
            }
            else
            {
                resultLine = String.Format("{1}{0}{2}{0}{3}{0}{4:0.000}{0}{5:0.000}{0}{6:yyyy-MM-dd HH:mm:ss}{0}\".{7}\"",
                    _separator, stats.Depth, stats.FileCount, stats.DirectoryCount,
                    stats.VirtualSizeMb, stats.SizeOnDiskMb,
                    stats.LastChange, stats.Path.Substring(_startCharPos));
            }
            //if (!_quiet && _verbose)
            //{
            //    ClearConsoleLine();
            //    Console.WriteLine(resultLine);
            //}
            _stream.WriteLine(resultLine);
        }

        //private static void ClearConsoleLine()
        //{
        //    int maxlen = Console.WindowWidth - 1;
        //    String value = String.Empty;
        //    value = value.PadRight(maxlen);
        //    Console.Write("\r{0}\r", value);
        //}

        public void ReportFooter()
        {
        }

        public string Name
        {
            get;
            private set;
        }

        public void Dispose()
        {
            _stream.Dispose();
        }
    }
}
