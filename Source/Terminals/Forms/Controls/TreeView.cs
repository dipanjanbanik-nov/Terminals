using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace Terminals.Forms.Controls
{
    /// <summary>
    /// Treeview control derived from winforms tree view and allowes load or save expanded state.
    /// </summary>
    internal partial class TreeView : System.Windows.Forms.TreeView
    {
        /// <summary>Separates individual path entries in the persisted string.</summary>
        private const string JOIN_SEPARATOR = "%%";

        /// <summary>Separates path segments within a single node path (e.g. "Parent|Child|GrandChild").</summary>
        private const string PATH_SEPARATOR = "|";

        protected TreeView()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Gets or sets the expansion state of every group node in the tree as a single string.
        /// Full nested paths are stored, e.g. "Root%%Root|Child%%Root|Child|GrandChild".
        /// Compatible with the old flat format so existing settings are read correctly.
        /// </summary>
        internal string ExpandedNodes
        {
            get { return this.GetExpandedFavoriteNodes(); }
            set { this.ExpandTreeView(value); }
        }

        private string GetExpandedFavoriteNodes()
        {
            var expandedPaths = new List<string>();
            CollectExpandedPaths(this.Nodes, string.Empty, expandedPaths);
            return string.Join(JOIN_SEPARATOR, expandedPaths.ToArray());
        }

        /// <summary>
        /// Recursively walks the tree and records the full path of every expanded GroupTreeNode.
        /// </summary>
        private static void CollectExpandedPaths(TreeNodeCollection nodes, string parentPath, List<string> result)
        {
            foreach (TreeNode treeNode in nodes)
            {
                if (!(treeNode is GroupTreeNode) || !treeNode.IsExpanded)
                    continue;

                string nodePath = string.IsNullOrEmpty(parentPath)
                    ? treeNode.Text
                    : parentPath + PATH_SEPARATOR + treeNode.Text;

                result.Add(nodePath);
                CollectExpandedPaths(treeNode.Nodes, nodePath, result);
            }
        }

        private void ExpandTreeView(string savedNodesToExpand)
        {
            if (string.IsNullOrEmpty(savedNodesToExpand))
                return;

            var paths = new List<string>(Regex.Split(savedNodesToExpand, JOIN_SEPARATOR));

            // Expand shallower paths first so parents are lazy-loaded before children are sought.
            paths.Sort((a, b) => CountChar(a, '|') - CountChar(b, '|'));

            foreach (string path in paths)
            {
                if (string.IsNullOrEmpty(path))
                    continue;

                ExpandNodePath(this.Nodes, path.Split(new char[] { '|' }), 0);
            }
        }

        /// <summary>
        /// Walks <paramref name="segments"/> depth-first, expanding each matching node.
        /// Calling Expand() on a GroupTreeNode that is not yet loaded fires AfterExpand,
        /// which triggers lazy loading so child nodes are available for the next segment.
        /// </summary>
        private static void ExpandNodePath(TreeNodeCollection nodes, string[] segments, int depth)
        {
            if (depth >= segments.Length)
                return;

            foreach (TreeNode treeNode in nodes)
            {
                if (treeNode.Text != segments[depth])
                    continue;

                if (!treeNode.IsExpanded)
                    treeNode.Expand();

                ExpandNodePath(treeNode.Nodes, segments, depth + 1);
                return;
            }
        }

        private static int CountChar(string text, char c)
        {
            int count = 0;
            foreach (char ch in text)
                if (ch == c) count++;
            return count;
        }
    }
}

