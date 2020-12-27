// Copyright 2021 Simone Livieri. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace Dwenegar.Doku.Resources
{
    internal sealed class ResourceReader : IDisposable
    {
        private readonly IReadOnlyList<string> _resourceNames;
        private readonly ZipArchive _zipArchive;

        private bool _disposed;

        public ResourceReader(Stream stream)
        {
            _zipArchive = new ZipArchive(stream);
            _resourceNames = _zipArchive.Entries.Where(s => !string.IsNullOrEmpty(s.Name))
                                        .Select(s => s.FullName)
                                        .ToArray();
        }

        public IEnumerable<(string, Stream)> GetResourceStreams()
        {
            ThrowIfDisposed();
            foreach (string name in _resourceNames)
            {
                Stream? stream = GetResourceStream(name);
                if (stream != null)
                {
                    yield return (name, stream);
                }
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private Stream? GetResourceStream(string name)
        {
            ThrowIfDisposed();
            return _zipArchive.GetEntry(name.Trim())?.Open();
        }

        private void Dispose(bool disposing)
        {
            if (disposing && !_disposed)
            {
                _zipArchive.Dispose();
                _disposed = true;
            }
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(ResourceReader));
            }
        }

        ~ResourceReader()
        {
            Dispose(false);
        }
    }
}
