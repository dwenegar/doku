// Copyright (c) Simone Livieri. For terms of use, see LICENSE.txt

using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Doku.Utils
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

            if (href != null && href.EndsWith(".md"))
            {
                entry.Href = href.Replace('\\', '/');
            }

            return entry;
        }

        public override string ToString() => $"ID={_id} Href={Href} Title={Title} Parent={Parent?._id ?? -1}";

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
    }
}
