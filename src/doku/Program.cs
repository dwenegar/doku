// Copyright 2021 Simone Livieri. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the LICENSE file in the project root for more information.

using System;
using CommandLine;
using CommandLine.Text;
using Dwenegar.Doku.Logging;

namespace Dwenegar.Doku
{
    internal static class Program
    {
        private static int Main(string[] args)
        {
            var parser = new Parser(settings => settings.HelpWriter = null);
            ParserResult<Options> parserResult = parser.ParseArguments<Options>(args);
            parserResult.WithParsed(Run).WithNotParsed(_ => DisplayHelp(parserResult));
            return 0;
        }

        private static void Run(Options options)
        {
            Logger.Initialize(options.LogLevel, options.LogFilePath);

            DocumentationBuilder builder = new(options.PackagePath, options.OutputPath, options.BuildPath)
            {
                DocFxPath = options.DocFxPath,
                TemplatePath = options.TemplatePath,
                StyleSheetPath = options.StyleSheetPath,
                KeepBuildFolder = options.KeepBuildFolder
            };

            builder.Build();
            Logger.Shutdown();
        }

        private static void DisplayHelp(ParserResult<Options> parserResult)
        {
            var helpText = HelpText.AutoBuild(parserResult, h => OnError(h, parserResult), e => e);
            helpText.AddPreOptionsLine("    Usage: doku <package path> [options]");
            Console.WriteLine(helpText.ToString());
        }

        private static HelpText OnError(HelpText h, ParserResult<Options> parserResult)
        {
            h.AddNewLineBetweenHelpSections = true;
            h.AdditionalNewLineAfterOption = false;
            h.AddDashesToOption = true;
            return HelpText.DefaultParsingErrorsHandler(parserResult, h);
        }
    }
}
