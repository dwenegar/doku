// Copyright 2021 Simone Livieri. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Dwenegar.Doku.Releaser
{
    internal sealed class ReleaseAssetInfo
    {
        private string? _name;
        private string? _sha256;

        public ReleaseAssetInfo(ReleaseAssetKind kind, string path, string mime)
        {
            Kind = kind;
            Path = path;
            Mime = mime;
        }

        public ReleaseAssetKind Kind { get; }

        public string Path { get; }

        public string Sha256 => _sha256 ??= ComputeSha256();
        public string Name => _name ??= System.IO.Path.GetFileName(Path);
        public string Mime { get; }

        private string ComputeSha256()
        {
            byte[] bytes = File.ReadAllBytes(Path);
            byte[] hash = SHA256.HashData(bytes);

            StringBuilder sb = new(hash.Length * 2);
            foreach (byte b in hash)
            {
                sb.Append(b.ToString("x2"));
            }

            return sb.ToString();
        }
    }
}
