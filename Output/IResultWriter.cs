using System;
using System.Collections.Generic;
using System.Text;

namespace SizeReporter.Output
{
    internal interface IResultWriter
    {
        void ReportHeader();
        void ReportFooter();
        void OutputResultLine(String directory, Int32 depth, PathStatistics stats);
    }
}
