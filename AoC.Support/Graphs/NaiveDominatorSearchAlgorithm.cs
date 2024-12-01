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

using System.Collections.Immutable;
using AoC.Support.Functional;
using QuikGraph;

namespace AoC.Support.Graphs;

public class NaiveDominatorSearchAlgorithm<TGraph, TVertex, TEdge> : DominatorSearchAlgorithm<TGraph, TVertex, TEdge>
    where TEdge : IEdge<TVertex> where TVertex : notnull where TGraph : IBidirectionalGraph<TVertex, TEdge> {
    private readonly Dictionary<TVertex, ImmutableHashSet<TVertex>> dominated = new();

    // vertex -> set of vertices that are dominated by vertex
    private readonly Dictionary<TVertex, ImmutableHashSet<TVertex>> dominators = new();

    // vertex -> immediate dominator up the tree
    private readonly Dictionary<TVertex, TVertex> immediateDominators = new();
    private readonly TVertex[] preorder;

    private readonly IDictionary<TVertex, int> preorderIndex = new Dictionary<TVertex, int>();
    private readonly ImmutableHashSet<TVertex> rootReachableVertices;

    public NaiveDominatorSearchAlgorithm(TGraph graph,
        TVertex root,
        Func<TVertex, TVertex, TEdge> edgeFactory,
        IEqualityComparer<TVertex>? comparer = null) : base(graph, root, edgeFactory, comparer) {
        preorder = graph.DfsPreorder(root).ToArray();
        for (var i = 0; i < preorder.Length; i++) preorderIndex[preorder[i]] = i;
        rootReachableVertices = preorder.ToImmutableHashSet(comparer);
        dominators[root] = ImmutableHashSet<TVertex>.Empty.WithComparer(comparer).Add(root);
    }

    public override BidirectionalMatrixPartialGraph<TVertex, TEdge> DominatorTree => GetDominanceTree();

    public override void Compute() {
        ComputeDominators();
    }

    public override bool Dominates(TVertex d, TVertex n) {
        return dominators[n].Contains(d);
    }

    private ReadOnlySpan<TVertex> Predecessors(TVertex v) {
        return preorder.AsSpan()[..preorderIndex[v]];
    }

    private ReadOnlySpan<TVertex> Successors(TVertex v) {
        var idx = preorderIndex[v] + 1;
        if (idx >= preorder.Length) return ReadOnlySpan<TVertex>.Empty;

        return preorder.AsSpan()[idx..];
    }

    private void ComputeDominators() {
        foreach (var n in rootReachableVertices)
            // Console.WriteLine($"Processing dominated for {n}");
            dominated[n] = ComputeDominated(n);

        foreach (var n in rootReachableVertices)
            // Console.WriteLine($"Processing dominators for {n}");
            dominators[n] = dominated
                .Where(kv => kv.Value.Contains(n))
                .Select(kv => kv.Key).ToImmutableHashSet(Comparer);
    }

    private ImmutableHashSet<TVertex> PredecessorDominators(TVertex v) {
        var preds = Predecessors(v);
        if (preds.Length == 0) return ImmutableHashSet<TVertex>.Empty.WithComparer(Comparer);

        var doms = dominators[preds[0]];
        foreach (var p in preds) doms = doms.Intersect(dominators[p]);

        return doms;
    }

    // A node w immediately dominates v if w dominates v, but all other nodes that dominate v also dominate w.
    // The root node has no immediate dominator.
    private IOption<TVertex> ImmediateDominator(TVertex v) {
        if (Comparer.Equals(v, Root)) return Option.None<TVertex>();

        if (immediateDominators.TryGetValue(v, out var idom)) return idom.Some();

        var doms = dominators[v];

        foreach (var w in doms) {
            if (!ImmediatelyDominates(w, v)) continue;

            idom = w;
            break;
        }

        // Every node has an immediate dominator except the root.
        immediateDominators[v] = idom ?? throw new Exception("Every node has an immediate dominator except the root.");
        return idom.Some();
    }

    public override bool ImmediatelyDominates(TVertex u, TVertex v) {
        if (immediateDominators.TryGetValue(v, out var idom)) return Comparer.Equals(idom, u);

        return StrictlyDominates(u, v) && !dominators[v].Any(w => StrictlyDominates(u, w) && StrictlyDominates(w, v));
    }

    public override IOption<TVertex> ImmediateDominatorOf(TVertex v) {
        if (Comparer.Equals(v, Root)) return Option.None<TVertex>();
        return immediateDominators[v].Some();
    }

    private ImmutableHashSet<TVertex> ComputeDominated(TVertex vertex) {
        var reachableWithout = Graph.GetReachableFromWithout(Root, vertex);
        var unreached = rootReachableVertices.Except(reachableWithout).Add(vertex);
        return unreached;
    }

    public BidirectionalMatrixPartialGraph<TVertex, TEdge> GetDominanceTree() {
        var tree = new BidirectionalMatrixPartialGraph<TVertex, TEdge>(rootReachableVertices.Count, EdgeFactory,
            Comparer);
        foreach (var v in rootReachableVertices) {
            var idom = ImmediateDominator(v);
            if (idom.IsNone) continue;

            tree.AddVerticesAndEdge(EdgeFactory(idom.Value, v));
        }

        return tree;
    }
}

public static class NaiveDominatorSearchAlgorithm {
    public static NaiveDominatorSearchAlgorithm<TGraph, TVertex, TEdge> Create<TGraph, TVertex, TEdge>(TGraph graph,
        TVertex root,
        Func<TVertex, TVertex, TEdge> edgeFactory,
        IEqualityComparer<TVertex>? comparer = null)
        where TEdge : IEdge<TVertex> where TVertex : notnull where TGraph : IBidirectionalGraph<TVertex, TEdge> {
        return new NaiveDominatorSearchAlgorithm<TGraph, TVertex, TEdge>(graph, root, edgeFactory, comparer);
    }
}