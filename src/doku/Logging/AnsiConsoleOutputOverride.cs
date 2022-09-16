// Copyright Simone Livieri. All Rights Reserved.
// Unauthorized copying of this file, via any medium is strictly prohibited.
// For terms of use, see LICENSE.txt

using System.IO;
using System.Text;
using Spectre.Console;

namespace Doku.Logging;

internal sealed class AnsiConsoleOutputOverride : IAnsiConsoleOutput
{
    private readonly IAnsiConsoleOutput _consoleOutput;

    public AnsiConsoleOutputOverride(IAnsiConsoleOutput consoleOutput)
    {
        _consoleOutput = consoleOutput;
        Width = 80;
        Height = 80;
    }

    public TextWriter Writer => _consoleOutput.Writer;

    public bool IsTerminal => _consoleOutput.IsTerminal;

    public int Width { get; init; }

    public int Height { get; init; }

    public void SetEncoding(Encoding encoding)
        => _consoleOutput.SetEncoding(encoding);
}
