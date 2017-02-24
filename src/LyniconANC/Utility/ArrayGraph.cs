using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lynicon.Utility
{
    /// <summary>
    /// Simple graph object implementation which uses an 2 dimensional array indexed by node.  This
    /// implementation is therefore fast but not suited to large (i.e. > 100) numbers of nodes.
    /// </summary>
    /// <typeparam name="TNode">Type of a node</typeparam>
    /// <typeparam name="TEdge">Type of an edge</typeparam>
    public class ArrayGraph<TNode, TEdge> : IComparer<TNode>
    {
        List<TNode> nodes = null;
        Dictionary<TNode, int> nodeIndex = null;
        TEdge[,] edges = null;
        int count;

        /// <summary>
        /// Whether there can only be one edge in one direction
        /// between any two nodes
        /// </summary>
        public bool Unidirectional { get; set; }

        /// <summary>
        /// Create a graph with no edges from a list of nodes
        /// </summary>
        /// <param name="nodes">List of nodes</param>
        public ArrayGraph(IEnumerable<TNode> nodes)
        {
            this.nodes = nodes.ToList();
            this.count = this.nodes.Count;
            this.edges = new TEdge[count, count];
            this.nodeIndex = new Dictionary<TNode, int>();
            for (int i = 0; i < this.count; i++)
                nodeIndex[this.nodes[i]] = i;

            Unidirectional = false;
        }

        /// <summary>
        /// Convert a node into its array index
        /// </summary>
        /// <param name="node">A node</param>
        /// <returns>index of the node in the edge array</returns>
        public int NodeIndex(TNode node)
        {
            return nodeIndex[node];
        }

        /// <summary>
        /// Get the edge between two nodes by index (null if no edge)
        /// </summary>
        /// <param name="iFrom">Edge is from this node at this index</param>
        /// <param name="iTo">Edge goes to this node at this index</param>
        /// <returns>Edge or null if none exists</returns>
        public TEdge this[int iFrom, int iTo]
        {
            get { return this.edges[iFrom, iTo]; }
            set
            {
                if (Unidirectional && !this[iTo, iFrom].Equals(default(TEdge)))
                    throw new ArgumentException(
                        string.Format("Graph: cannot add edge from '{0}' to '{1}' as one exists in opposite direction",
                            nodes[iFrom], nodes[iTo]));
                this.edges[iFrom, iTo] = value;
            }
        }
        /// <summary>
        /// Get the edge between two nodes (null if no edge)
        /// </summary>
        /// <param name="from">Edge is from this node</param>
        /// <param name="to">Edge goes to this node</param>
        /// <returns>Edge or null if none exists</returns>
        public TEdge this[TNode from, TNode to]
        {
            get { return this.edges[nodeIndex[from], nodeIndex[to]]; }
            set
            {
                if (Unidirectional && !this[to, from].Equals(default(TEdge)))
                    throw new ArgumentException(
                        string.Format("Graph: cannot add edge from '{0}' to '{1}' as one exists in opposite direction",
                            from, to));
                this.edges[nodeIndex[from], nodeIndex[to]] = value;
            }
        }

        #region IComparer<TNode> Members

        /// <summary>
        /// This is a partial comparison, the return value is zero if no edge exists between the two nodes
        /// </summary>
        /// <param name="x">a node</param>
        /// <param name="y">another node</param>
        /// <returns>-1 if edge goes from x to y, 1 if edge goes from y to x, 0 if no edge between x and y</returns>
        public int Compare(TNode x, TNode y)
        {
            if (!this.edges[nodeIndex[x], nodeIndex[y]].Equals(default(TEdge)))
                return -1;
            else if (!this.edges[nodeIndex[y], nodeIndex[x]].Equals(default(TEdge)))
                return 1;
            else
                return 0;
        }

        #endregion
    }
}
