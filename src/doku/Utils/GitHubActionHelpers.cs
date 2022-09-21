// Copyright Simone Livieri. All Rights Reserved.
// Unauthorized copying of this file, via any medium is strictly prohibited.
// For terms of use, see LICENSE.txt

using System;

namespace Doku.Utils;

public static class GitHubActionHelpers
{
    public static bool IsRunningOnGitHubAction { get; } = !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("GITHUB_ACTIONS"));

    public static GitHubActionInfo? GetGitHubInfo()
    {
        // https://docs.github.com/en/actions/learn-github-actions/environment-variables#default-environment-variables
        if (!IsRunningOnGitHubAction)
        {
            return null;
        }

        string? eventName = Environment.GetEnvironmentVariable("GITHUB_EVENT_NAME");
        if (eventName is null)
        {
            return null;
        }

        string? refName = Environment.GetEnvironmentVariable("GITHUB_REF_NAME");
        if (refName is null)
        {
            return null;
        }

        string? refTypeStr = Environment.GetEnvironmentVariable("GITHUB_REF_TYPE");
        if (refTypeStr is null)
        {
            return null;
        }

        if (!Enum.TryParse(refTypeStr, true, out GitHubActionRefType refType))
        {
            return null;
        }

        string[]? ownerAndRepo = Environment.GetEnvironmentVariable("GITHUB_REPOSITORY")?.Split('/');
        if (ownerAndRepo is null || ownerAndRepo.Length != 2)
        {
            return null;
        }

        return new GitHubActionInfo(ownerAndRepo[0], ownerAndRepo[1], eventName, refName, refType);
    }
}

public record GitHubActionInfo(string Owner, string Repository, string Event, string Ref, GitHubActionRefType RefType)
{
    public override string ToString()
    {
        string refType = RefType == GitHubActionRefType.Branch ? "branch" : "tag";
        return $"user = {Owner}, repo = {Repository}, event = {Event}, ref_name = {Ref}, ref_type = {refType}";
    }
}

public enum GitHubActionRefType
{
    Branch,
    Tag
}
