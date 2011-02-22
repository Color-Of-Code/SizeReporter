using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace SizeReporter.Output
{
    internal class XmlResultOutput : IResultWriter
    {
        private Boolean _quiet = false;
        private XmlTextWriter _stream;
        private int _startCharPos;

        public XmlResultOutput(TextWriter tw, int startPos, Boolean quiet)
        {
            _stream = new XmlTextWriter(tw);
            _stream.Formatting = Formatting.Indented;
            _quiet = quiet;
            _startCharPos = startPos;
        }

        public void ReportHeader()
        {
            _stream.WriteStartElement("Sizereport");
        }

        public void ReportFooter()
        {
            _stream.WriteEndElement();
        }

        public void OutputResultLine(string directory, int depth, PathStatistics stats)
        {
            _stream.WriteStartElement("Directory");
            _stream.WriteAttributeString("Path", directory.Substring(_startCharPos));
            _stream.WriteAttributeString("Depth", depth.ToString());
            _stream.WriteElementString("Files", stats.FileCount.ToString());
            _stream.WriteElementString("Directories", stats.DirectoryCount.ToString());
            _stream.WriteElementString("VirtualSize", stats.VirtualSizeMb.ToString("0.000"));
            _stream.WriteElementString("DiskSize", stats.VirtualSizeMb.ToString("0.000"));
            _stream.WriteElementString("LastModification", stats.LastChange.ToString("yyyy-MM-dd HH:mm:ss"));
            _stream.WriteEndElement();
        }
    }
}
