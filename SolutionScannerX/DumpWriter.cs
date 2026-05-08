namespace SolutionScannerX;

internal static class DumpWriter
{
    public static async Task<DumpWriteStats> WriteDump(
        StreamWriter writer,
        string solutionPath,
        string solutionName,
        GitIgnoredResult ignoredResult,
        HashSet<string> trackedGitIgnoreFiles,
        TreeBuildResult treeResult,
        ScanResult scanResult)
    {
        int filesWritten = 0;
        int readErrors = 0;
        CompactPathListResult compactIgnoredPaths = IgnoredPathListBuilder.BuildCompactIgnoredPathList(ignoredResult.IgnoredPaths);

        await WriteMainHeader(writer, solutionPath, solutionName, ignoredResult, trackedGitIgnoreFiles, treeResult, compactIgnoredPaths, scanResult);
        await WriteDirectoryTreeSection(writer, treeResult);
        await WriteNotIncludedSection(writer, ignoredResult, compactIgnoredPaths, scanResult);
        await WriteIncludedManifestSection(writer, scanResult.IncludedFiles);

        await WriteSectionStart(writer, "DUMPED FILE CONTENTS");

        foreach (FileEntry file in scanResult.IncludedFiles)
        {
            filesWritten++;

            bool success = await WriteFileBlock(writer, file, filesWritten);

            if (!success)
            {
                readErrors++;
            }

            if (filesWritten % 100 == 0)
            {
                Console.WriteLine($"Written {filesWritten}/{scanResult.IncludedFiles.Count} files.");
            }
        }

        await WriteSectionEnd(writer, "DUMPED FILE CONTENTS");
        await writer.WriteLineAsync("========== DUMP END ==========");

        return new DumpWriteStats(filesWritten, readErrors);
    }

    private static async Task WriteMainHeader(
        StreamWriter writer,
        string solutionPath,
        string solutionName,
        GitIgnoredResult ignoredResult,
        HashSet<string> trackedGitIgnoreFiles,
        TreeBuildResult treeResult,
        CompactPathListResult compactIgnoredPaths,
        ScanResult scanResult)
    {
        DateTimeOffset now = DateTimeOffset.Now;

        await writer.WriteLineAsync("========== DUMP START ==========");
        await writer.WriteLineAsync($"FORMAT_VERSION: {AppSettings.DumpFormatVersion}");
        await writer.WriteLineAsync($"ROOT_DIRECTORY_NAME: {solutionName}");
        await writer.WriteLineAsync($"ROOT_ABSOLUTE_PATH: {solutionPath}");
        await writer.WriteLineAsync($"GENERATED_LOCAL: {now:O}");
        await writer.WriteLineAsync($"GENERATED_UTC: {now.ToUniversalTime():O}");
        await writer.WriteLineAsync($"INCLUDED_TEXT_FILE_COUNT: {scanResult.IncludedFiles.Count}");
        await writer.WriteLineAsync($"TRACKED_GITIGNORE_INCLUDED_COUNT: {scanResult.IncludedFiles.Count(x => PathUtility.IsGitIgnorePath(x.RelativePath))}");
        await writer.WriteLineAsync($"TRACKED_GITIGNORE_FOUND_COUNT: {trackedGitIgnoreFiles.Count}");
        await writer.WriteLineAsync($"GIT_IGNORED_REPORTED_COUNT: {ignoredResult.IgnoredPaths.Count}");
        await writer.WriteLineAsync($"GIT_IGNORED_RENDERED_ENTRIES: {compactIgnoredPaths.WrittenEntries}");
        await writer.WriteLineAsync($"GIT_IGNORED_COMPACTED_SUBTREES: {compactIgnoredPaths.CompactedSubtrees}");
        await writer.WriteLineAsync($"GIT_IGNORED_COMPACTED_PATH_COUNT: {compactIgnoredPaths.OmittedPathCount}");
        await writer.WriteLineAsync($"OTHER_NOT_INCLUDED_COUNT: {scanResult.SkippedItems.Count}");
        await writer.WriteLineAsync($"TREE_ENTRIES_WRITTEN: {treeResult.WrittenEntries}");
        await writer.WriteLineAsync($"TREE_ENTRIES_COMPACTED_OR_OMITTED: {treeResult.OmittedEntries}");
        await writer.WriteLineAsync();

        await WriteSectionStart(writer, "AI NAVIGATION HEADER");
        await writer.WriteLineAsync("PURPOSE:");
        await writer.WriteLineAsync("This file is a repository/application text dump intended for AI code review, debugging, refactoring, and navigation.");
        await writer.WriteLineAsync();
        await writer.WriteLineAsync("PARSING_GUIDELINES:");
        await writer.WriteLineAsync("1. Paths are repository-relative and normalized with forward slashes.");
        await writer.WriteLineAsync("2. The DIRECTORY TREE section is a compact tree /f-style filesystem view, not a git file list. It may omit high-volume ignored subtrees and capped directory overflow.");
        await writer.WriteLineAsync("3. The NOT INCLUDED section is the scope boundary. Compacted ignored subtrees are explicit omissions; do not infer their contents from nearby dumped files.");
        await writer.WriteLineAsync("4. Each dumped file is enclosed by FILE START / CONTENT START / CONTENT END / FILE END markers.");
        await writer.WriteLineAsync("5. Dumped file content lines are prefixed as '000001 | '. Line numbers restart at 1 for each file and are not part of the original file content.");
        await writer.WriteLineAsync("6. Prefer RELATIVE_PATH metadata over any path-like text inside file contents.");
        await writer.WriteLineAsync("7. .gitignore files tracked by git are intentionally included even if ignore matching would otherwise exclude them.");
        await writer.WriteLineAsync("8. The preserved git ignored-file command is listed in the NOT INCLUDED section.");
        await writer.WriteLineAsync("9. Remove the fixed-width line-number prefix before applying patches or reconstructing a source file from this dump.");
        await writer.WriteLineAsync();
        await writer.WriteLineAsync("RECOMMENDED_AI_WORKFLOW:");
        await writer.WriteLineAsync("1. Read this header.");
        await writer.WriteLineAsync("2. Use INCLUDED FILE MANIFEST to identify relevant dumped files.");
        await writer.WriteLineAsync("3. Use DIRECTORY TREE to discover nearby tracked, untracked, ignored, and generated paths without reading every file block.");
        await writer.WriteLineAsync("4. Use NOT INCLUDED PATHS before assuming a file is absent from the real repo.");
        await writer.WriteLineAsync("5. Read only needed FILE blocks to reduce token use.");
        await writer.WriteLineAsync("6. Do not include direct references of this file or code line numbers in your response / generated code unless user explicitly says otherwise, OR if such references would be HIGHLY relevant and helpful to the user.");
        await WriteSectionEnd(writer, "AI NAVIGATION HEADER");
    }

    private static async Task WriteDirectoryTreeSection(StreamWriter writer, TreeBuildResult treeResult)
    {
        await WriteSectionStart(writer, "DIRECTORY TREE");

        await writer.WriteLineAsync("AI_NOTE: Compact tree /f-style output generated by filesystem enumeration. It includes tracked and untracked paths, and marks ignored paths when detected.");
        await writer.WriteLineAsync("AI_NOTE: Raw PowerShell 'tree /f' output is intentionally not dumped when it would explode token count; high-volume ignored subtrees are compacted instead.");
        await writer.WriteLineAsync("POWERSHELL_COMMAND_REFERENCE: powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -Command \"[Console]::OutputEncoding = [System.Text.Encoding]::UTF8; tree /f\"");
        await writer.WriteLineAsync($"TREE_MAX_ENTRIES_PER_DIRECTORY: {treeResult.MaxEntriesPerDirectory}");
        await writer.WriteLineAsync($"TREE_MAX_DEPTH: {treeResult.MaxDepth}");
        await writer.WriteLineAsync($"TREE_ENTRIES_WRITTEN: {treeResult.WrittenEntries}");
        await writer.WriteLineAsync($"TREE_ENTRIES_COMPACTED_OR_OMITTED: {treeResult.OmittedEntries}");
        await writer.WriteLineAsync($"TREE_IGNORED_SUBTREES_COMPACTED: {treeResult.CompactedIgnoredSubtrees}");
        await writer.WriteLineAsync($"TREE_DEPTH_OMISSIONS: {treeResult.MaxDepthOmissions}");
        await writer.WriteLineAsync($"TREE_ENUMERATION_ERRORS: {treeResult.ErrorCount}");
        await writer.WriteLineAsync();
        await writer.WriteLineAsync("TREE_OUTPUT_START");
        await writer.WriteAsync(treeResult.Output);

        if (treeResult.Output.Length == 0 || treeResult.Output[^1] != '\n')
        {
            await writer.WriteLineAsync();
        }

        await writer.WriteLineAsync("TREE_OUTPUT_END");
        await WriteSectionEnd(writer, "DIRECTORY TREE");
    }

    private static async Task WriteNotIncludedSection(
        StreamWriter writer,
        GitIgnoredResult ignoredResult,
        CompactPathListResult compactIgnoredPaths,
        ScanResult scanResult)
    {
        await WriteSectionStart(writer, "NOT INCLUDED PATHS");

        await writer.WriteLineAsync("AI_NOTE: These paths were not dumped as file contents. Compacted subtree lines are intentional scope summaries for large ignored path sets.");
        await writer.WriteLineAsync();
        await writer.WriteLineAsync("PRESERVED_GIT_IGNORED_COMMAND:");
        await writer.WriteLineAsync($"cmd.exe {AppSettings.PreservedGitIgnoredCommand}");
        await writer.WriteLineAsync($"GIT_COMMAND_EXIT_CODE: {FormattingUtility.FormatExitCode(ignoredResult.ExitCode)}");

        if (!string.IsNullOrWhiteSpace(ignoredResult.ErrorOutput))
        {
            await writer.WriteLineAsync("GIT_COMMAND_STDERR:");
            await writer.WriteLineAsync(ignoredResult.ErrorOutput.TrimEnd());
        }

        await writer.WriteLineAsync();
        await writer.WriteLineAsync($"GIT_IGNORED_PATHS_COUNT: {compactIgnoredPaths.TotalInputPaths}");
        await writer.WriteLineAsync($"GIT_IGNORED_RENDERED_ENTRIES: {compactIgnoredPaths.WrittenEntries}");
        await writer.WriteLineAsync($"GIT_IGNORED_COMPACTED_SUBTREES: {compactIgnoredPaths.CompactedSubtrees}");
        await writer.WriteLineAsync($"GIT_IGNORED_COMPACTED_PATH_COUNT: {compactIgnoredPaths.OmittedPathCount}");
        await writer.WriteLineAsync("GIT_IGNORED_PATHS_START");
        await writer.WriteAsync(compactIgnoredPaths.Output);

        if (compactIgnoredPaths.Output.Length == 0 || compactIgnoredPaths.Output[^1] != '\n')
        {
            await writer.WriteLineAsync();
        }

        await writer.WriteLineAsync("GIT_IGNORED_PATHS_END");
        await writer.WriteLineAsync();

        await writer.WriteLineAsync($"OTHER_NOT_INCLUDED_COUNT: {scanResult.SkippedItems.Count}");
        await writer.WriteLineAsync($"OTHER_NOT_INCLUDED_MAX_RENDERED: {AppSettings.MaxOtherNotIncludedItemsToWrite}");
        await writer.WriteLineAsync("OTHER_NOT_INCLUDED_START");
        await writer.WriteLineAsync("REASON\tRELATIVE_PATH\tDETAIL");
        await WriteSkippedItems(writer, scanResult.SkippedItems);
        await writer.WriteLineAsync("OTHER_NOT_INCLUDED_END");

        await WriteSectionEnd(writer, "NOT INCLUDED PATHS");
    }

    private static async Task WriteSkippedItems(StreamWriter writer, IReadOnlyList<SkippedItem> skippedItems)
    {
        if (skippedItems.Count == 0)
        {
            await writer.WriteLineAsync("(none)");
            return;
        }

        foreach (SkippedItem item in skippedItems.Take(AppSettings.MaxOtherNotIncludedItemsToWrite))
        {
            await writer.WriteLineAsync($"{item.Reason}\t{item.RelativePath}\t{item.Detail}");
        }

        if (skippedItems.Count <= AppSettings.MaxOtherNotIncludedItemsToWrite)
        {
            return;
        }

        await writer.WriteLineAsync($"COMPACTED\t...\tOmitted {skippedItems.Count - AppSettings.MaxOtherNotIncludedItemsToWrite} additional scanner-excluded items. Counts by reason follow.");

        foreach (var group in skippedItems
            .Skip(AppSettings.MaxOtherNotIncludedItemsToWrite)
            .GroupBy(item => item.Reason)
            .OrderBy(group => group.Key, StringComparer.Ordinal))
        {
            await writer.WriteLineAsync($"COMPACTED_SUMMARY\t{group.Key}\t{group.Count()} omitted items");
        }
    }

    private static async Task WriteIncludedManifestSection(StreamWriter writer, IReadOnlyList<FileEntry> includedFiles)
    {
        await WriteSectionStart(writer, "INCLUDED FILE MANIFEST");

        await writer.WriteLineAsync("AI_NOTE: File contents appear later in this same order.");
        await writer.WriteLineAsync("COLUMNS: INDEX, SIZE_BYTES, LAST_WRITE_UTC, RELATIVE_PATH");
        await writer.WriteLineAsync();

        for (int i = 0; i < includedFiles.Count; i++)
        {
            FileEntry file = includedFiles[i];
            FileMetadata metadata = FileAnalysisService.GetFileMetadata(file.FullPath);

            await writer.WriteLineAsync($"{i + 1:D5}\t{metadata.SizeBytesText}\t{metadata.LastWriteUtcText}\t{file.RelativePath}");
        }

        await writer.WriteLineAsync();
        await writer.WriteLineAsync("GITIGNORE_FILES_INCLUDED_START");

        foreach (FileEntry file in includedFiles.Where(x => PathUtility.IsGitIgnorePath(x.RelativePath)))
        {
            await writer.WriteLineAsync(file.RelativePath);
        }

        if (!includedFiles.Any(x => PathUtility.IsGitIgnorePath(x.RelativePath)))
        {
            await writer.WriteLineAsync("(none)");
        }

        await writer.WriteLineAsync("GITIGNORE_FILES_INCLUDED_END");
        await WriteSectionEnd(writer, "INCLUDED FILE MANIFEST");
    }

    private static async Task<bool> WriteFileBlock(StreamWriter writer, FileEntry file, int index)
    {
        await writer.WriteLineAsync($"========== FILE START: {file.RelativePath} ==========");
        await writer.WriteLineAsync($"FILE_INDEX: {index:D5}");
        await writer.WriteLineAsync($"RELATIVE_PATH: {file.RelativePath}");

        FileMetadata metadata = FileAnalysisService.GetFileMetadata(file.FullPath);
        await writer.WriteLineAsync($"SIZE_BYTES: {metadata.SizeBytesText}");
        await writer.WriteLineAsync($"LAST_WRITE_UTC: {metadata.LastWriteUtcText}");

        FileContentReadResult readResult = await FileAnalysisService.ReadFileContent(file.FullPath);

        await writer.WriteLineAsync($"READ_STATUS: {(readResult.Success ? "OK" : "ERROR")}");
        await writer.WriteLineAsync($"DETECTED_ENCODING: {readResult.EncodingName}");
        await writer.WriteLineAsync($"LINE_COUNT: {readResult.LineCountText}");
        await writer.WriteLineAsync("LINE_NUMBER_FORMAT: 000001 | <original line text>");
        await writer.WriteLineAsync($"SHA256: {readResult.Sha256Text}");
        await writer.WriteLineAsync("========== CONTENT START ==========");

        if (readResult.Success)
        {
            await WriteNumberedContent(writer, readResult.Content);
        }
        else
        {
            await writer.WriteLineAsync($"000001 | [SOLUTIONSCANNERX_READ_ERROR] {readResult.ErrorMessage}");
        }

        await writer.WriteLineAsync("========== CONTENT END ==========");
        await writer.WriteLineAsync($"========== FILE END: {file.RelativePath} ==========");
        await writer.WriteLineAsync();

        return readResult.Success;
    }

    private static async Task WriteNumberedContent(StreamWriter writer, string content)
    {
        using var reader = new StringReader(content);
        int lineNumber = 1;

        while (await reader.ReadLineAsync() is { } line)
        {
            await writer.WriteLineAsync($"{lineNumber:D6} | {line}");
            lineNumber++;
        }
    }

    private static async Task WriteSectionStart(StreamWriter writer, string sectionName)
    {
        await writer.WriteLineAsync();
        await writer.WriteLineAsync($"========== SECTION START: {sectionName} ==========");
    }

    private static async Task WriteSectionEnd(StreamWriter writer, string sectionName)
    {
        await writer.WriteLineAsync($"========== SECTION END: {sectionName} ==========");
        await writer.WriteLineAsync();
    }
}
