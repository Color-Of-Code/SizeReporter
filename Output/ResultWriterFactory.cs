using System;
using System.IO;
using System.Collections.Generic;
using System.Text;

namespace SizeReporter.Output
{
    internal class ResultWriterFactory
    {
        public static IResultWriter Build(Options.Options options) 
        {
            String filename = options.ReportFile;
            if (String.IsNullOrEmpty(filename))
            {
                String extension = "csv";
                if (options.Xml)
                    extension = "xml";
                filename = String.Format("sizereport_result_{0}.{1}", options.Timestamp, extension);
            }
            if (options.Xml)
                return new Output.XmlResultOutput(filename, options.StartCharPos, options.BeQuiet);
            else
                return new Output.CsvResultOutput(filename, options.StartCharPos, options.BeQuiet, options.Tsv);

        }
    }
}
