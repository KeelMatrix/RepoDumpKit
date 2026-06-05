# RepoDumpKit

![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)
![.NET](https://img.shields.io/badge/.NET-10.0-blue.svg)

RepoDumpKit is a small .NET console tool that creates AI-friendly text dumps of C#/.NET solution or repository folders.

It scans a repository, includes readable text files, excludes binary/build/generated content, records Git-ignored paths, and writes a structured `.txt` dump designed for code review, debugging, refactoring, and AI-assisted navigation.

## Features

- Generates a single structured text dump for a solution or repository.
- Includes a compact directory tree overview.
- Preserves tracked `.gitignore` files.
- Uses Git ignore rules to skip ignored files and folders.
- Excludes known binary formats and binary-looking files.
- Records skipped files and reasons.
- Adds metadata such as file size, last write time, encoding, SHA-256 hash, and line count.
- Saves the last used repository path for repeated runs.
- Writes output files to the current user's Desktop.

## Output format

Each generated dump contains:

- Repository metadata
- AI navigation instructions
- Compact directory tree
- Git-ignored paths
- Other skipped paths
- Included file manifest
- Numbered file contents

Output files are named like this:

```text
RepositoryName-yyyyMMddHHmmss.txt
```

Example:

```text
RepoDumpKit-20260508190701.txt
```

## Requirements

- .NET 10 SDK
- Git
- Windows

The current implementation invokes `cmd.exe` for Git ignore discovery, so Windows is required unless that process invocation is changed.

## Build

```bash
dotnet build RepoDumpKit.slnx
```

## Run

Run with an explicit repository path:

```bash
dotnet run --project RepoDumpKit -- "C:\Path\To\Repository"
```

Or run without arguments and enter the repository path when prompted:

```bash
dotnet run --project RepoDumpKit
```

The selected path is stored in:

```text
solution_path_config.txt
```

When the app is run again, it offers to reuse the saved path.

## Publish

```bash
dotnet publish RepoDumpKit/RepoDumpKit.csproj --configuration Release
```

The included publish profile writes to:

```text
RepoDumpKit/bin/Release/net10.0/publish/
```

## How it works

1. Resolves the target repository path from the first command-line argument, a saved config file, or interactive input.
2. Uses Git to determine ignored files and folders.
3. Builds a compact tree-style repository overview.
4. Scans readable files while skipping ignored, binary, generated, and unsafe paths.
5. Writes a structured dump file to the Desktop.

## Notes

- Git metadata is never dumped.
- Build output directories such as `bin/` and `obj/` are ignored.
- Binary files are skipped by extension and by sample inspection.
- Line numbers in generated dumps are for navigation only and are not part of the original source files.
- The dump format version is currently `3`.

## License

This project is licensed under the MIT License. See [LICENSE](LICENSE).
