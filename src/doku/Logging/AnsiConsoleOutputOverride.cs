// Copyright Simone Livieri. All Rights Reserved.
// Unauthorized copying of this file, via any medium is strictly prohibited.
// For terms of use, see LICENSE.txt

using System.IO;
using System.Text;
using Spectre.Console;

namespace Doku.Logging;

public class AnsiConsoleOutputOverride : IAnsiConsoleOutput
{
    private readonly IAnsiConsoleOutput _delegate;

    public AnsiConsoleOutputOverride(IAnsiConsoleOutput @delegate)
    {
        _delegate = @delegate;
        Width = 80;
        Height = 80;
    }

    public TextWriter Writer => _delegate.Writer;

    public bool IsTerminal => _delegate.IsTerminal;

    public int Width { get; set; }

    public int Height { get; set; }

    public void SetEncoding(Encoding encoding)
        => _delegate.SetEncoding(encoding);
}
