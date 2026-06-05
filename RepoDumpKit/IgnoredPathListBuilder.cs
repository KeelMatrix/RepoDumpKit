using System.Text;

namespace RepoDumpKit;

internal static class IgnoredPathListBuilder
{
    public static CompactPathListResult BuildCompactIgnoredPathList(HashSet<string> ignoredPaths)
    {
        if (ignoredPaths.Count == 0)
        {
            return new CompactPathListResult("(none)" + Environment.NewLine, 1, 0, 0, 0);
        }

        var root = new PathTrieNode();

        foreach (string ignoredPath in ignoredPaths.OrderBy(path => path, AppSettings.PathComparer))
        {
            string normalized = PathUtility.NormalizeGitPath(ignoredPath).TrimEnd('/');

            if (string.IsNullOrWhiteSpace(normalized) || normalized == ".")
            {
                continue;
            }

            string[] parts = normalized.Split('/', StringSplitOptions.RemoveEmptyEntries);
            PathTrieNode current = root;
            current.LeafCount++;

            foreach (string part in parts)
            {
                current = current.GetOrAdd(part);
                current.LeafCount++;
            }

            current.IsTerminal = true;
        }

        var output = new StringBuilder();
        var stats = new CompactPathListStats();

        foreach (KeyValuePair<string, PathTrieNode> child in root.Children.OrderBy(x => x.Key, AppSettings.PathComparer))
        {
            RenderCompactIgnoredPathNode(child.Value, child.Key, depth: 1, output, stats);
        }

        return new CompactPathListResult(
            output.ToString(),
            stats.WrittenEntries,
            stats.CompactedSubtrees,
            stats.OmittedPathCount,
            ignoredPaths.Count);
    }

    private static void RenderCompactIgnoredPathNode(
        PathTrieNode node,
        string path,
        int depth,
        StringBuilder output,
        CompactPathListStats stats)
    {
        bool hasChildren = node.Children.Count > 0;
        string displayPath = hasChildren ? path + "/" : path;

        if (hasChildren && depth >= 1 && node.LeafCount > AppSettings.MaxIgnoredPathsPerRenderedSubtree)
        {
            output.AppendLine($"{displayPath} [compacted ignored subtree: {node.LeafCount} ignored paths]");
            stats.WrittenEntries++;
            stats.CompactedSubtrees++;
            stats.OmittedPathCount += node.LeafCount;
            return;
        }

        if (node.IsTerminal)
        {
            output.AppendLine(path);
            stats.WrittenEntries++;
        }

        if (!hasChildren)
        {
            return;
        }

        if (depth >= AppSettings.MaxIgnoredPathRenderDepth)
        {
            output.AppendLine($"{path}/... [compacted by depth: {node.LeafCount} ignored paths under this prefix]");
            stats.WrittenEntries++;
            stats.CompactedSubtrees++;
            stats.OmittedPathCount += node.LeafCount;
            return;
        }

        int childIndex = 0;
        int omittedChildCount = 0;
        int omittedPathCount = 0;

        foreach (KeyValuePair<string, PathTrieNode> child in node.Children.OrderBy(x => x.Key, AppSettings.PathComparer))
        {
            if (childIndex >= AppSettings.MaxIgnoredPathChildrenPerDirectory)
            {
                omittedChildCount++;
                omittedPathCount += child.Value.LeafCount;
                continue;
            }

            string childPath = path + "/" + child.Key;
            RenderCompactIgnoredPathNode(child.Value, childPath, depth + 1, output, stats);
            childIndex++;
        }

        if (omittedChildCount > 0)
        {
            output.AppendLine($"{path}/... [compacted {omittedChildCount} sibling subtrees containing {omittedPathCount} ignored paths]");
            stats.WrittenEntries++;
            stats.CompactedSubtrees += omittedChildCount;
            stats.OmittedPathCount += omittedPathCount;
        }
    }
}
