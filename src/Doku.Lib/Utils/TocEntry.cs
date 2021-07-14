// Copyright 2021 Simone Livieri. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Dwenegar.Doku.Utils
{
    internal sealed class TocEntry
    {
        private static int s_nextId;

        private readonly List<TocEntry> _entries = new();

        private readonly int _id = s_nextId++;

        public TocEntry(TocEntry? parent, string? title)
        {
            Title = title;
            Parent = parent;
        }

        public TocEntry? Parent { get; }

        public string? Title { get; }
        public string? Href { get; set; }

        public IEnumerable<TocEntry> Entries => _entries;

        public TocEntry AddEntry(string title, string? href)
        {
            string? parentName = Path.GetDirectoryName(href);
            TocEntry parent = string.IsNullOrEmpty(parentName) ? this : GetParentEntry(parentName);

            TocEntry? entry = parent.Entries.FirstOrDefault(x => x.Title == title);
            if (entry == null)
            {
                parent._entries.Add(entry = new TocEntry(parent, title));
            }

            entry.Href = href?.Replace('\\', '/');
            return entry;
        }

        private TocEntry GetParentEntry(string path)
        {
            string entryName = Path.GetFileName(path);
            string title = TocHelper.ToTitleCase(entryName);

            foreach (TocEntry tocEntry in Entries)
            {
                if (string.Equals(tocEntry.Title, title))
                {
                    return tocEntry;
                }
            }

            return AddEntry(entryName, path);
        }

        public override string ToString() => $"ID={_id} Href={Href} Title={Title} Parent={Parent?._id ?? -1}";
    }
}
