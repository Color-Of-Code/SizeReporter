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

        public CsvResultOutput(TextWriter tw, int startPos, Boolean quiet, Boolean tabSeparated)
        {
            if (tabSeparated)
                _separator = "\t";
            else
                _separator = ";";
            //_verbose = true;
            _stream = tw;
            _quiet = quiet;
            _startCharPos = startPos;
        }

        public void ReportHeader()
        {
            _stream.WriteLine(
                "Depth{0}Files{0}Dirs{0}Virtual size (MB){0}Size on disk (MB){0}Last modification{0}Relative Path",
                _separator);
        }

        public void OutputResultLine(String directory, Int32 depth, PathStatistics stats)
        {
            String resultLine = String.Format("{1}{0}{2}{0}{3}{0}{4:0.000}{0}{5:0.000}{0}{6:yyyy-MM-dd HH:mm:ss}{0}\".{7}\"",
                _separator, depth, stats.FileCount, stats.DirectoryCount,
                stats.VirtualSizeMb, stats.SizeOnDiskMb,
                stats.LastChange, directory.Substring(_startCharPos));
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
    }
}
