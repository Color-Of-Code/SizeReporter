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

        public XmlResultOutput(String filename, int startPos, Boolean quiet)
        {
            Name = filename;
            _stream = new XmlTextWriter(File.CreateText(filename));
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

        public void OutputResultLine(PathStatistics stats, bool includeRemotePath)
        {
            _stream.WriteStartElement("Directory");
            _stream.WriteAttributeString("Path", stats.Path.Substring(_startCharPos));
            _stream.WriteAttributeString("Depth", stats.Depth.ToString());
            _stream.WriteElementString("Files", stats.FileCount.ToString());
            _stream.WriteElementString("Directories", stats.DirectoryCount.ToString());
            _stream.WriteElementString("VirtualSize", stats.VirtualSizeMb.ToString("0.000"));
            _stream.WriteElementString("DiskSize", stats.VirtualSizeMb.ToString("0.000"));
            _stream.WriteElementString("LastModification", stats.LastChange.ToString("yyyy-MM-dd HH:mm:ss"));
            if (includeRemotePath)
                _stream.WriteElementString("RemotePath", stats.RemotePath ?? String.Empty);
            _stream.WriteEndElement();
        }

        public string Name
        {
            get;
            private set;
        }

        public void Dispose()
        {
            _stream.Close();
        }
    }
}
