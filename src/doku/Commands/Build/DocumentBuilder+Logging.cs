// Copyright Simone Livieri. All Rights Reserved.
// Unauthorized copying of this file, via any medium is strictly prohibited.
// For terms of use, see LICENSE.txt

using System;
using Doku.Logging;

namespace Doku.Commands.Build;

internal sealed partial class DocumentBuilder
{
    private IDisposable BeginGroup(string title) => _logger.BeginGroup(title);

    private void Info(string message) => _logger.LogInfo(message);

    private void Warning(string message) => _logger.LogWarning(message);

    private void Error(string message) => _logger.LogError(message);
}
