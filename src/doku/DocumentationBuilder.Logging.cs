// Copyright (c) Simone Livieri. For terms of use, see LICENSE.txt

using Doku.Logging;

namespace Doku;

internal sealed partial class DocumentationBuilder
{
    private void Verbose(string message) => _logger.LogTrace(message);

    private void Info(string message) => _logger.LogInfo(message);

    private void Warning(string message) => _logger.LogWarning(message);

    private void Error(string message) => _logger.LogError(message);
}
