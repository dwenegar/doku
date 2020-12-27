// Copyright 2021 Simone Livieri. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Dwenegar.Doku.Utils
{
    internal sealed class TocEntry
    {
        private readonly List<TocEntry> _entries = new();

        public TocEntry(string? title)
        {
            Title = title;
        }

        public string? Title { get; }
        public string? Href { get; set; }

        public IEnumerable<TocEntry> Entries => _entries;

        public void AddEntry(string title, string href)
        {
            string? parentName = Path.GetDirectoryName(href);
            TocEntry parent = GetParentEntry(parentName);
            TocEntry? entry = parent.Entries.FirstOrDefault(x => x.Title == title);
            if (entry == null)
            {
                parent._entries.Add(entry = new TocEntry(title));
            }

            Debug.Assert(entry.Href == null, "entry.Href == null");
            entry.Href = href.Replace('\\', '/');
        }

        private TocEntry GetParentEntry(string? path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return this;
            }

            string entryName = Path.GetFileName(path);
            string title = TocHelper.ToTitleCase(entryName);

            foreach (TocEntry tocEntry in Entries)
            {
                if (tocEntry.Title == title)
                {
                    return tocEntry;
                }
            }

            string? parentName = Path.GetDirectoryName(path);
            TocEntry parent = GetParentEntry(parentName);

            var entry = new TocEntry(title);
            parent._entries.Add(entry);
            return entry;
        }
    }
}
