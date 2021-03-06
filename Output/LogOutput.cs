﻿using System;
using System.IO;
using System.Collections.Generic;
using System.Text;

namespace SizeReporter.Output
{
    internal class LogOutput : IDisposable
    {
        private Boolean _quiet = false;
        private TextWriter _stream;
        private int _startCharPos;

        public LogOutput(Options.Options options)
        {
            _name = options.ErrorFile;
            _stream = File.CreateText(_name);
            _quiet = options.BeQuiet;
            _startCharPos = options.StartCharPos;
        }

        private String _name;
        public String Name
        {
            get
            {
                return _name;
            }
        }

        public void LogError(String filepath, Exception exception)
        {
            if (!_quiet)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Error.WriteLine();
                Console.Error.WriteLine("skipping .{0}:", filepath.Substring(_startCharPos));
                Console.Error.WriteLine(" -> {0}", exception.Message);
                Console.ResetColor();
            }
            if (_stream != null)
            {
                _stream.WriteLine("skipping .{0}:", filepath.Substring(_startCharPos));
                _stream.WriteLine(" -> {0}", exception.Message);
            }
        }

        public void LogWarning(String filepath, String text)
        {
            if (!_quiet)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Error.WriteLine();
                Console.Error.WriteLine("warning .{0}:", filepath.Substring(_startCharPos));
                Console.Error.WriteLine(text);
                Console.ResetColor();
            }
            if (_stream != null)
            {
                _stream.WriteLine("warning .{0}:", filepath.Substring(_startCharPos));
                _stream.WriteLine(text);
            }
        }

        public void LogInfo(String format, params object[] args)
        {
            if (!_quiet)
            {
                Console.ForegroundColor = ConsoleColor.Blue;
                ClearConsoleLine();
                Console.WriteLine(format, args);
                Console.ResetColor();
            }
            if (_stream != null)
            {
                _stream.WriteLine(format, args);
            }
        }

        private static void ClearConsoleLine()
        {
            int maxlen = Console.WindowWidth - 1;
            String value = String.Empty;
            value = value.PadRight(maxlen);
            Console.Write("\r{0}\r", value);
        }

        public void Dispose()
        {
            _stream.Dispose();
            _stream = null;
        }
    }
}
