// Copyright (c) Simone Livieri. For terms of use, see LICENSE.txt

using System;
using System.Threading.Tasks;
using CommandLine;
using CommandLine.Text;
using Dwenegar.Doku.Logging;

namespace Dwenegar.Doku.Releaser
{
    internal static class Program
    {
        private static int s_exitCode;

        private static async Task<int> Main(string[] args)
        {
            var parser = new Parser(settings => settings.HelpWriter = null);
            ParserResult<Options> parserResult = parser.ParseArguments<Options>(args);
            await parserResult.WithNotParsed(_ => DisplayHelp(parserResult)).WithParsedAsync(Run);
            return s_exitCode;
        }

        private static async Task Run(Options options)
        {
            Logger.Initialize(options.LogLevel, options.LogFilePath);
            try
            {
                if (options.BuildOnly)
                {
                    RunBuilder(options);
                }
                else
                {
                    await RunBuilderAndReleaser(options);
                }
            }
            catch (Exception e)
            {
                Logger.LogError(e.Message);
                Logger.LogVerbose(e);
                s_exitCode = 1;
            }
            finally
            {
                Logger.Shutdown();
            }
        }

        private static void RunBuilder(Options options)
        {
            Builder builder = new(options.RootPath, options.OutputPath);
            builder.Build();
        }

        private static async Task RunBuilderAndReleaser(Options options)
        {
            ValidateGitHubCredentials(options);

            Builder builder = new(options.RootPath, options.OutputPath);
            BuildResult buildResult = builder.Build();

            var uploader = new Releaser(options.GitHubToken, options.GitHubRepository);
            await uploader.MakeRelease(buildResult);
        }

        private static void ValidateGitHubCredentials(Options options)
        {
            if (string.IsNullOrEmpty(options.GitHubToken))
            {
                throw new Exception("Missing GitHub API token.");
            }

            if (string.IsNullOrEmpty(options.GitHubRepository))
            {
                throw new Exception("Missing GitHub repository.");
            }

            string[] split = options.GitHubRepository.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (split.Length != 2)
            {
                throw new Exception("Invalid GitHub repository.");
            }
        }

        private static void DisplayHelp(ParserResult<Options> parserResult)
        {
            var helpText = HelpText.AutoBuild(parserResult, h => OnError(h, parserResult), e => e);
            helpText.AddPreOptionsLine("")
                    .AddPreOptionsLine("    Usage: Doku.Releaser [options]")
                    .AddPreOptionsLine("");
            Console.WriteLine(helpText);
        }

        private static HelpText OnError(HelpText h, ParserResult<Options> parserResult)
        {
            s_exitCode = 1;
            h.AdditionalNewLineAfterOption = false;
            h.AddDashesToOption = true;
            return HelpText.DefaultParsingErrorsHandler(parserResult, h);
        }
    }
}
