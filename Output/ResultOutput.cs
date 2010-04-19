using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SizeReporter.Output
{
    internal class ResultOutput
    {
        private Boolean _quiet = false;
        private TextWriter _stream;
        private int _startCharPos;

        public ResultOutput(TextWriter tw, int startPos, Boolean quiet)
        {
            _stream = tw;
            _quiet = quiet;
            _startCharPos = startPos;
        }

        public void ReportHeader()
        {
            //_stream.WriteLine("Depth\tFiles\tDirs\tVirtual size\tSize on disk\tPath");
            _stream.WriteLine("Depth\tFiles\tDirs\tVirtual size (MB)\tSize on disk (MB)\tLast modification\tRelative Path");
        }

        public void OutputResultLine(String directory, Int32 depth, PathStatistics stats)
        {
            String resultLine = String.Format("{0}\t{1}\t{2}\t{3:0.000}\t{4:0.000}\t{5:yyyy-MM-dd HH:mm:ss}\t.{6}",
                depth,
                stats.FileCount, stats.DirectoryCount,
                stats.VirtualSizeMb, stats.SizeOnDiskMb,
                stats.LastChange,
                directory.Substring(_startCharPos));
            if (!_quiet)
            {
                ClearConsoleLine();
                Console.WriteLine(resultLine);
            }
            _stream.WriteLine(resultLine);
        }

        private static void ClearConsoleLine()
        {
            int maxlen = Console.WindowWidth - 1;
            String value = String.Empty;
            value = value.PadRight(maxlen);
            Console.Write("\r{0}\r", value);
        }
    }
}
