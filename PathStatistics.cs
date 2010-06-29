using System;
using System.Collections.Generic;
using System.Text;

namespace SizeReporter
{
    internal struct PathStatistics
    {
        public static PathStatistics operator +(PathStatistics s1, PathStatistics s2)
        {
            PathStatistics result = new PathStatistics();

            result.VirtualSize = s1.VirtualSize + s2.VirtualSize;
            result.SizeOnDisk = s1.SizeOnDisk + s2.SizeOnDisk;

            result.FileCount = s1.FileCount + s2.FileCount;
            result.DirectoryCount = s1.DirectoryCount + s2.DirectoryCount;
            result.LastChange = s1.LastChange;
            result.RefreshLastModified(s2.LastChange);
            return result;
        }

        public Double VirtualSizeMb
        {
            get
            {
                return (Double)VirtualSize / 1024.0 / 1024.0;
            }
        }

        public Double SizeOnDiskMb
        {
            get
            {
                return (Double)SizeOnDisk / 1024.0 / 1024.0;
            }
        }

        public void RefreshLastModified(DateTime lastModified)
        {
            if (lastModified > LastChange)
                LastChange = lastModified;
        }

        public DateTime LastChange;
        public UInt64 VirtualSize;
        public UInt64 SizeOnDisk;
        public UInt32 FileCount;
        public UInt32 DirectoryCount;
    }
}
