// Copyright (c) Simone Livieri. For terms of use, see LICENSE.txt

using System.IO;
using System.Text;

namespace Doku.Logging.Handlers
{
    internal sealed class FileLogHandler : LogHandlerBase
    {
        private readonly TextWriter _writer;

        public FileLogHandler(string path)
            => _writer = CreateWriter(path);

        public override void Close()
            => _writer.Close();

        public override void Handle(ref LogRecord logRecord)
            => _writer.WriteLine(FormatRecord(ref logRecord));

        private static TextWriter CreateWriter(string path)
        {
            string fullPath = Path.GetFullPath(path);
            string? directory = Path.GetDirectoryName(fullPath);
            if (string.IsNullOrEmpty(directory))
            {
                throw new IOException($"Invalid path: {path}");
            }

            Directory.CreateDirectory(directory);
            var stream = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
            var writer = new StreamWriter(stream, Encoding.UTF8);
            return TextWriter.Synchronized(writer);
        }
    }
}
