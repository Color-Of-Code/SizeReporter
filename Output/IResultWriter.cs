using System;
using System.Collections.Generic;
using System.Text;

namespace SizeReporter.Output
{
    internal interface IResultWriter : IDisposable
    {
        String Name { get; }
        void ReportHeader();
        void ReportFooter();
        void OutputResultLine(PathStatistics stats, bool includeRemotePath);
    }
}
