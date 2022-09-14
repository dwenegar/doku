// Copyright Simone Livieri. All Rights Reserved.
// Unauthorized copying of this file, via any medium is strictly prohibited.
// For terms of use, see LICENSE.txt

using System;

namespace Doku.Utils;

public static class GitHubActionHelpers
{
    public static readonly bool IsRunningOnGitHubAction = !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("GITHUB_ACTIONS"));
}
