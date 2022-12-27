﻿using System;
using System.Linq;
using Statiq.Common;
using Statiq.Core;

namespace Statiq.Web.Pipelines
{
    public class Redirects : Pipeline
    {
        public Redirects()
        {
            Dependencies.Add(nameof(Content));

            ProcessModules = new ModuleList
            {
                new ReplaceDocuments(nameof(Content)),
                new ExecuteConfig(Config.FromContext(async ctx =>
                {
                    GenerateRedirects generateRedirects = new GenerateRedirects()
                        .WithMetaRefreshPages(ctx.Settings.GetBool(WebKeys.MetaRefreshRedirects, true));
                    if (ctx.Settings.GetBool(WebKeys.NetlifyRedirects, false))
                    {
                        // Make sure we keep any existing manual redirect content
                        IFile existingFile = ctx.FileSystem.GetInputFile("_redirects");
                        string existingContent = string.Empty;
                        if (existingFile.Exists)
                        {
                            generateRedirects = generateRedirects.AlwaysCreateAdditionalOutput();
                            existingContent = await existingFile.ReadAllTextAsync();
                        }

                        // Produce the additional redirect content
                        generateRedirects = generateRedirects.WithAdditionalOutput(
                            "_redirects",
                            redirects =>
                            {
                                string newContent = redirects.Count > 0
                                    ? (existingContent.IsNullOrEmpty() ? string.Empty : Environment.NewLine + Environment.NewLine)
                                        + "# Automatic redirects generated by Statiq"
                                        + Environment.NewLine
                                        + string.Join(Environment.NewLine, redirects.Select(r => $"/{r.Key} {r.Value}"))
                                    : string.Empty;
                                return existingContent + newContent;
                            });
                    }
                    return generateRedirects;
                }))
            };

            OutputModules = new ModuleList
            {
                new WriteFiles()
            };
        }
    }
}