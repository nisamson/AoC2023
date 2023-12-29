#region license
// AoC2023 - AoC.Support - NaiveDominatorSearchAlgorithm.cs
// Copyright (C) 2023 Nicholas
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <https://www.gnu.org/licenses/>.
#endregion

using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Diagnostics;
using QuikGraph;

namespace AoC.Support.Graphs;

public class NaiveDominatorSearchAlgorithm<TGraph, TVertex, TEdge> where TEdge : IEdge<TVertex> where TVertex : notnull where TGraph : IBidirectionalGraph<TVertex, TEdge> {
    private readonly BidirectionalMatrixPartialGraph<TVertex, TEdge> graph;
    private readonly TVertex root;
    private readonly IEqualityComparer<TVertex> comparer;
    private readonly Func<TVertex, TVertex, TEdge> edgeFactory;
    private readonly ImmutableHashSet<TVertex> rootReachableVertices;
    // vertex -> immediate dominator up the tree
    private readonly Dictionary<TVertex, TVertex> immediateDominators = new();
    // vertex -> set of vertices that are dominated by vertex
    private readonly Dictionary<TVertex, ImmutableHashSet<TVertex>> dominators = new();
    
    public NaiveDominatorSearchAlgorithm(TGraph graph, TVertex root, Func<TVertex, TVertex, TEdge> edgeFactory, IEqualityComparer<TVertex>? comparer = null) {
        this.graph = BidirectionalMatrixPartialGraph<TVertex, TEdge>.Create(graph, edgeFactory, comparer);
        this.root = root;
        this.comparer = comparer ?? EqualityComparer<TVertex>.Default;
        this.edgeFactory = edgeFactory;
        rootReachableVertices = graph.DfsPreorder(root).ToImmutableHashSet(this.comparer);
        dominators[root] = rootReachableVertices;
    }

    private bool StrictlyDominates(TVertex d, TVertex n) {
        return Dominates(d, n) && !Dominates(n, d);
    }
    
    private bool Dominates(TVertex d, TVertex n) {
        return dominators[n].Contains(d);
    }

    private void ComputeDominators() {
        foreach (var vertex in graph.Vertices.Where(v => !comparer.Equals(v, root))) {
            dominators[vertex] = ComputeDominators(vertex);
        }
    }

    private ImmutableHashSet<TVertex> ComputeDominators(TVertex vertex) {
        Debug.Assert(!comparer.Equals(vertex, root));
        return rootReachableVertices.Except(graph.GetReachableFromWithout(root, vertex)).Add(vertex);
    }
}
