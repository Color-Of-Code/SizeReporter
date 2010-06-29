using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace SizeReporter
{
    internal class FileUtil
    {
        #region STRUCTURES

        internal static IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);
        //internal static int FILE_ATTRIBUTE_READONLY  = 0x00000001;
        //internal static int FILE_ATTRIBUTE_HIDDEN    = 0x00000002; 
        //internal static int FILE_ATTRIBUTE_SYSTEM    = 0x00000004; 

        //internal static int FILE_ATTRIBUTE_DIRECTORY = 0x00000010;
        //internal static int FILE_ATTRIBUTE_ARCHIVE   = 0x00000020;
        //internal static int FILE_ATTRIBUTE_DEVICE    = 0x00000040;
        //internal static int FILE_ATTRIBUTE_NORMAL    = 0x00000080;

        //internal static int FILE_ATTRIBUTE_TEMPORARY = 0x00000100; 

        internal const int MAX_PATH = 260;

        [StructLayout(LayoutKind.Sequential)]
        internal struct FILETIME
        {
            internal uint dwLowDateTime;
            internal uint dwHighDateTime;
        };


        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal struct WIN32_FIND_DATA
        {
            internal FileAttributes dwFileAttributes;
            internal FILETIME ftCreationTime;
            internal FILETIME ftLastAccessTime;
            internal FILETIME ftLastWriteTime;
            internal int nFileSizeHigh;
            internal int nFileSizeLow;
            internal int dwReserved0;
            internal int dwReserved1;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = MAX_PATH)]
            internal string cFileName;
            // not using this
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 14)]
            internal string cAlternate;
        }

        [Flags]
        internal enum EFileAccess : uint
        {
            GenericRead = 0x80000000,
            GenericWrite = 0x40000000,
            GenericExecute = 0x20000000,
            GenericAll = 0x10000000,
        }
        [Flags]
        internal enum EFileShare : uint
        {
            None = 0x00000000,
            Read = 0x00000001,
            Write = 0x00000002,
            Delete = 0x00000004,
        }

        internal enum ECreationDisposition : uint
        {
            New = 1,
            CreateAlways = 2,
            OpenExisting = 3,
            OpenAlways = 4,
            TruncateExisting = 5,
        }

        [Flags]
        internal enum EFileAttributes : uint
        {
            Readonly = 0x00000001,
            Hidden = 0x00000002,
            System = 0x00000004,
            Directory = 0x00000010,
            Archive = 0x00000020,
            Device = 0x00000040,
            Normal = 0x00000080,
            Temporary = 0x00000100,
            SparseFile = 0x00000200,
            ReparsePoint = 0x00000400,
            Compressed = 0x00000800,
            Offline = 0x00001000,
            NotContentIndexed = 0x00002000,
            Encrypted = 0x00004000,
            Write_Through = 0x80000000,
            Overlapped = 0x40000000,
            NoBuffering = 0x20000000,
            RandomAccess = 0x10000000,
            SequentialScan = 0x08000000,
            DeleteOnClose = 0x04000000,
            BackupSemantics = 0x02000000,
            PosixSemantics = 0x01000000,
            OpenReparsePoint = 0x00200000,
            OpenNoRecall = 0x00100000,
            FirstPipeInstance = 0x00080000
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct WIN32_FILE_ATTRIBUTE_DATA
        {
            public FileAttributes dwFileAttributes;
            public FILETIME ftCreationTime;
            public FILETIME ftLastAccessTime;
            public FILETIME ftLastWriteTime;
            public uint nFileSizeHigh;
            public uint nFileSizeLow;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct SECURITY_ATTRIBUTES
        {
            public int nLength;
            public IntPtr lpSecurityDescriptor;
            public int bInheritHandle;
        }

        internal enum GET_FILEEX_INFO_LEVELS
        {
            GetFileExInfoStandard,
            GetFileExMaxInfoLevel
        }

        #endregion

        #region DllImports

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode, EntryPoint = "GetFileAttributesEx")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool _GetFileAttributesEx(string lpFileName, GET_FILEEX_INFO_LEVELS fInfoLevelId, out WIN32_FILE_ATTRIBUTE_DATA fileData);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, EntryPoint = "FindFirstFile")]
        internal static extern IntPtr _FindFirstFile(string lpFileName, out WIN32_FIND_DATA lpFindFileData);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, EntryPoint = "FindNextFile")]
        internal static extern bool _FindNextFile(IntPtr hFindFile, out WIN32_FIND_DATA lpFindFileData);

        [DllImport("kernel32.dll", SetLastError = true, EntryPoint = "FindClose")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool _FindClose(IntPtr hFindFile);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto, EntryPoint = "GetDiskFreeSpace")]
        internal static extern bool _GetDiskFreeSpace(string lpRootPathName,
           out uint lpSectorsPerCluster,
           out uint lpBytesPerSector,
           out uint lpNumberOfFreeClusters,
           out uint lpTotalNumberOfClusters);

        [DllImport("kernel32.dll", SetLastError = true, EntryPoint = "GetCompressedFileSize")]
        internal static extern uint _GetCompressedFileSize(string lpFileName, out uint lpFileSizeHigh);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true, EntryPoint = "GetShortPathName")]
        static extern uint _GetShortPathName(
            [MarshalAs(UnmanagedType.LPTStr)]string lpszLongPath,
            [MarshalAs(UnmanagedType.LPTStr)]StringBuilder lpszShortPath,
            uint cchBuffer);

        #endregion

        public static uint GetClusterSize(String dir)
        {
            uint SectorsPerCluster;
            uint BytesPerSector;
            uint NumberOfFreeClusters;
            uint TotalNumberOfClusters;

            _GetDiskFreeSpace(dir, out SectorsPerCluster, out BytesPerSector,
               out NumberOfFreeClusters, out TotalNumberOfClusters);
            return BytesPerSector * SectorsPerCluster;
        }

        public static string ToShortPathName(string longName)
        {
            uint bufferSize = 256;
            // don´t allocate stringbuilder here but outside of the function for fast access
            StringBuilder shortNameBuffer = new StringBuilder((int)bufferSize);
            uint result = _GetShortPathName(longName, shortNameBuffer, bufferSize);
            return shortNameBuffer.ToString();
        }

        // Assume dirName passed in is already prefixed with \\?\
        public static List<string> FindFilesAndDirs(string dirName)
        {
            List<string> results = new List<string>();
            WIN32_FIND_DATA findData;
            IntPtr findHandle = _FindFirstFile(dirName + @"\*", out findData);

            if (findHandle != INVALID_HANDLE_VALUE)
            {
                bool found;
                do
                {
                    string currentFileName = findData.cFileName;

                    // if this is a directory, find its contents
                    if ((findData.dwFileAttributes & FileAttributes.Directory) != 0)
                    {
                        if (currentFileName != "." && currentFileName != "..")
                        {
                            List<string> childResults = FindFilesAndDirs(Path.Combine(dirName, currentFileName));
                            // add children and self to results
                            results.AddRange(childResults);
                            results.Add(Path.Combine(dirName, currentFileName));
                        }
                    }

                    // it's a file; add it to the results
                    else
                    {
                        results.Add(Path.Combine(dirName, currentFileName));
                    }

                    // find next
                    found = _FindNextFile(findHandle, out findData);
                }
                while (found);
            }

            // close the find handle
            _FindClose(findHandle);
            return results;
        }

        // Assume dirName passed in is already prefixed with \\?\
        public static List<string> FindFiles(string dirName)
        {
            List<string> results = new List<string>();
            WIN32_FIND_DATA findData;
            IntPtr findHandle = _FindFirstFile(dirName + @"\*", out findData);

            if (findHandle != INVALID_HANDLE_VALUE)
            {
                bool found;
                do
                {
                    string currentFileName = findData.cFileName;

                    // if this is a file
                    if ((findData.dwFileAttributes & FileAttributes.Directory) == 0)
                    {
                        results.Add(Path.Combine(dirName, currentFileName));
                    }

                    // find next
                    found = _FindNextFile(findHandle, out findData);
                }
                while (found);
            }

            // close the find handle
            _FindClose(findHandle);
            return results;
        }

        public static List<string> FindFilesSorted(string dirName)
        {
            List<string> results = FindFiles(dirName);
            results.Sort();
            return results;
        }

        // Assume dirName passed in is already prefixed with \\?\
        public static List<string> FindDirectories(string dirName, bool followLinks)
        {
            List<string> results = new List<string>();
            WIN32_FIND_DATA findData;
            IntPtr findHandle = _FindFirstFile(dirName + @"\*", out findData);

            if (findHandle != INVALID_HANDLE_VALUE)
            {
                bool found;
                do
                {
                    string currentFileName = findData.cFileName;

                    if ((findData.dwFileAttributes & FileAttributes.Directory) != 0)
                    {
                        if (followLinks || (findData.dwFileAttributes & FileAttributes.ReparsePoint) == 0)
                        {
                            if (currentFileName != "." && currentFileName != "..")
                            {
                                results.Add(Path.Combine(dirName, currentFileName));
                            }
                        }
                    }
                    // find next
                    found = _FindNextFile(findHandle, out findData);
                }
                while (found);
            }

            // close the find handle
            _FindClose(findHandle);
            return results;
        }

        public static List<string> FindDirectoriesSorted(string dirName, bool followLinks)
        {
            List<string> results = FindDirectories(dirName, followLinks);
            results.Sort();
            return results;
        }

        public static List<string> FindDirectoriesSorted(string dirName)
        {
            return FindDirectoriesSorted(dirName, true);
        }

        // Assume filename passed in is already prefixed with \\?\
        public static UInt64 GetFileSize(string filename)
        {
            GET_FILEEX_INFO_LEVELS levels = GET_FILEEX_INFO_LEVELS.GetFileExInfoStandard;
            WIN32_FILE_ATTRIBUTE_DATA result;
            if (_GetFileAttributesEx(filename, levels, out result))
                return ((ulong)result.nFileSizeHigh << 32) + result.nFileSizeLow;
            throw new Exception("Could not get file size");
        }

        public static void GetFileSizeAndLastModified(string filename, out UInt64 size, out DateTime lastModified)
        {
            GET_FILEEX_INFO_LEVELS levels = GET_FILEEX_INFO_LEVELS.GetFileExInfoStandard;
            WIN32_FILE_ATTRIBUTE_DATA result;
            if (_GetFileAttributesEx(filename, levels, out result))
            {
                size = ((ulong)result.nFileSizeHigh << 32) + result.nFileSizeLow;
                Int64 filetime = ((long)result.ftLastWriteTime.dwHighDateTime << 32) + result.ftLastWriteTime.dwLowDateTime;
                lastModified = DateTime.FromFileTime(filetime);
                return;
            }
            throw new Exception("Could not get file attributes");
        }

        public static void GetLastModified(string filename, out DateTime lastModified)
        {
            GET_FILEEX_INFO_LEVELS levels = GET_FILEEX_INFO_LEVELS.GetFileExInfoStandard;
            WIN32_FILE_ATTRIBUTE_DATA result;
            if (_GetFileAttributesEx(filename, levels, out result))
            {
                Int64 filetime = ((long)result.ftLastWriteTime.dwHighDateTime << 32) + result.ftLastWriteTime.dwLowDateTime;
                lastModified = DateTime.FromFileTime(filetime);
                return;
            }
            throw new Exception("Could not get file attributes");
        }

        // Assume filename passed in is already prefixed with \\?\
        public static UInt64 GetCompressedFileSize(string filename)
        {
            uint high;
            uint low;
            low = _GetCompressedFileSize(filename, out high);
            int error = Marshal.GetLastWin32Error();
            if (high == 0 && low == 0xFFFFFFFF && error != 0)
            {
                throw new System.ComponentModel.Win32Exception(error);
            }
            else
            {
                return ((ulong)high << 32) + low;
            }
        }


        public static String GetLongEscapedPathname(String path)
        {
            if (path.StartsWith(@"\\?\"))
                return path;
            if (path.StartsWith(@"\\"))
            {
                //_startCharPos = 7;
                path = @"\\?\UNC" + path.Substring(1);
            }
            else
            {
                //_startCharPos = 4;
                path = @"\\?\" + path;
            }
            return path;
        }

    }

}
