// Copyright 2021 Simone Livieri. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Dwenegar.Doku.Logging;
using Octokit;

namespace Dwenegar.Doku.Releaser
{
    internal class Releaser
    {
        private const int UploadRetryCount = 3;

        private readonly IGitHubClient _gh;
        private readonly string _repository;
        private readonly string _user;

        private IReadOnlyList<ReleaseAsset>? _assets;

        public Releaser(string token, string repository)
        {
            var productHeaderValue = new ProductHeaderValue("Doku.Releaser");
            _gh = new GitHubClient(productHeaderValue)
            {
                Credentials = new Credentials(token)
            };

            string[] split = repository.Split('/', StringSplitOptions.RemoveEmptyEntries);
            _user = split[0];
            _repository = split[1];
        }

        public async Task MakeRelease(BuildResult buildResult)
        {
            (string version, string changelog, IEnumerable<ReleaseAssetInfo> assets) = buildResult;
            Release release = await UpdateOrCreateRelease(version, changelog);
            await UploadReleaseAssets(release, assets);
        }

        private async Task<Release> UpdateOrCreateRelease(string version, string changelog)
        {
            using Logger.Scope scope = new("UpdateOrCreateRelease");

            IReadOnlyList<Release>? releases = await _gh.Repository.Release.GetAll(_user, _repository);
            Release? release = releases?.FirstOrDefault(x => x.TagName == version);
            if (release == null)
            {
                Logger.LogVerbose($"Creating release {version}.");

                var newRelease = new NewRelease(version);
                release = await _gh.Repository.Release.Create(_user, _repository, newRelease);
            }

            ReleaseUpdate? releaseUpdate = null;
            if (release.Body != changelog)
            {
                releaseUpdate = release.ToUpdate();
                releaseUpdate.Body = changelog;
            }

            if (releaseUpdate != null)
            {
                Logger.LogVerbose($"Updating release {version}.");

                release = await _gh.Repository.Release.Edit(_user,
                                                            _repository,
                                                            release.Id,
                                                            releaseUpdate);
            }

            return release;
        }

        private async Task UploadReleaseAssets(Release release, IEnumerable<ReleaseAssetInfo> entries)
        {
            using Logger.Scope scope = new("UploadReleaseAssets");

            _assets = await _gh.Repository.Release.GetAllAssets(_user,
                                                                _repository,
                                                                release.Id,
                                                                ApiOptions.None);

            foreach (ReleaseAssetInfo entry in entries)
            {
                await UploadReleaseAsset(release, entry);
            }
        }

        private async Task UploadReleaseAsset(Release release, ReleaseAssetInfo entry)
        {
            using Logger.Scope scope = new("UploadReleaseAsset");

            if (_assets!.Any(x => x.Name == entry.Name))
            {
                return;
            }

            for (var i = 0; i < UploadRetryCount; i++)
            {
                Logger.LogVerbose($"Uploading {entry.Path}, try {i} of {UploadRetryCount}.");

                if (i > 0)
                {
                    await Task.Delay(100);
                }

                bool uploaded = await TryUploadAsset(release, entry);
                if (uploaded)
                {
                    return;
                }

                bool deleted = await TryDeleteAsset(entry);
                if (deleted)
                {
                    _assets = await _gh.Repository.Release.GetAllAssets(_user,
                                                                        _repository,
                                                                        release.Id,
                                                                        ApiOptions.None);
                }
            }

            throw new Exception($"Failed to upload `{entry.Path}`.");
        }

        private async Task<bool> TryUploadAsset(Release release, ReleaseAssetInfo entry)
        {
            try
            {
                await using FileStream stream = File.OpenRead(entry.Path);
                var assetUpload = new ReleaseAssetUpload(Path.GetFileName(entry.Path),
                                                         entry.Mime,
                                                         stream,
                                                         TimeSpan.FromSeconds(5));

                await _gh.Repository.Release.UploadAsset(release, assetUpload);
                return true;
            }
            catch (Exception e)
            {
                Logger.LogVerbose(e);
                return false;
            }
        }

        private async Task<bool> TryDeleteAsset(ReleaseAssetInfo entry)
        {
            try
            {
                ReleaseAsset? assetToDelete = _assets!.FirstOrDefault(x => x.Name == entry.Name);
                if (assetToDelete != null)
                {
                    await _gh.Repository.Release.DeleteAsset(_user, _repository, assetToDelete.Id);
                    return true;
                }
            }
            catch (Exception e)
            {
                Logger.LogVerbose(e);
            }

            return false;
        }
    }
}
