using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace SizeReporter.Output
{
    internal class XmlResultOutput : IResultWriter
    {
        private Boolean _quiet = false;
        private TextWriter _stream;
        private int _startCharPos;

        public XmlResultOutput(TextWriter tw, int startPos, Boolean quiet)
        {
            _stream = tw;
            _quiet = quiet;
            _startCharPos = startPos;
        }

        public void ReportHeader()
        {

        }

        public void ReportFooter()
        {

        }

        public void OutputResultLine(string directory, int depth, PathStatistics stats)
        {

        }
    }
}
