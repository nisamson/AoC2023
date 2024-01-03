#region license

// AoC2023 - AoC.Support - DataFlowDominatorSearchAlgorithm.cs
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
using System.Data;
using System.Diagnostics;
using AoC.Support.Collections;
using AoC.Support.Functional;
using QuikGraph;

namespace AoC.Support.Graphs;

public class DataFlowDominatorSearchAlgorithm<TGraph, TVertex, TEdge> : DominatorSearchAlgorithm<TGraph, TVertex, TEdge>
    where TEdge : IEdge<TVertex> where TVertex : notnull where TGraph : IBidirectionalGraph<TVertex, TEdge> {
    private readonly Dictionary<TVertex, BitArray> dominators = new();
    private readonly Dictionary<TVertex, TVertex> immediateDominators = new();
    private readonly ImmutableHashSet<TVertex> vertices;
    private readonly FrozenDictionary<TVertex, int> vertIndices;
    private readonly ImmutableArray<TVertex> postorder;
    private readonly ImmutableArray<TVertex> preorder;
    private readonly Dictionary<TVertex, BitArray> strictlyDominated = new();
    private readonly Dictionary<TVertex, BitArray> strictDominators = new();

    public DataFlowDominatorSearchAlgorithm(TGraph graph,
        TVertex root,
        Func<TVertex, TVertex, TEdge> edgeFactory,
        IEqualityComparer<TVertex>? comparer = null)
        : base(graph, root, edgeFactory, comparer) {
        
        preorder = graph.DfsPreorder(root).ToImmutableArray();
        vertices = preorder.ToImmutableHashSet(comparer);
        vertIndices = preorder.Select((v, i) => (v, i)).ToFrozenDictionary(x => x.v, x => x.i, Comparer);
        // postorder = graph.DfsPreorder()
        Debug.Assert(graph.ContainsVertex(root));
    }

    public override void Compute() {
        foreach (var vertex in preorder) {
            dominators[vertex] = new BitArray(preorder.Length, true);
        }

        dominators[Root].Clear();
        dominators[Root][vertIndices[Root]] = true;

        var changed = true;
        var i = 0;
        var iter = 0;
        while (changed) {
            iter++;
            changed = false;
            foreach (var v in preorder) {
                if (Comparer.Equals(v, Root)) {
                    continue;
                }

                var preds = new BitArray(preorder.Length, true);
                foreach (var pred in Graph.InEdges(v)) {
                    preds.And(dominators[pred.Source]);
                }

                preds[vertIndices[v]] = true;
                if (preds.Equals(dominators[v])) {
                    i++;
                    continue;
                }

                dominators[v] = preds;
                changed = true;
            }
        }
        
        Console.WriteLine("Iterations: " + iter);

        foreach (var v in vertices) {
            var varr = new BitArray(preorder.Length);
            strictlyDominated[v] = varr;
            foreach (var u in vertices) {
                if (Comparer.Equals(u, v)) {
                    continue;
                }

                if (dominators[u][vertIndices[v]]) {
                    varr[vertIndices[u]] = true;
                }
            }
        }

        foreach (var v in vertices) {
            strictDominators[v] = dominators[v].Clone();
            strictDominators[v][vertIndices[v]] = false;
        }
        
        //Compute immediate dominators
        foreach (var v in vertices) {
            foreach (var u in DominatorsOf(v)) {
                if (!ImmediatelyDominates(u, v)) {
                    continue;
                }
                
                immediateDominators[v] = u;
                break;
            }
        }
    }

    public override bool StrictlyDominates(TVertex d, TVertex n) {
        return strictlyDominated[d][vertIndices[n]];
    }

    public override bool ImmediatelyDominates(TVertex u, TVertex v) {
        //     public virtual bool ImmediatelyDominates(TVertex u, TVertex v) {
        // return StrictlyDominates(u, v) && !DominatorsOf(v).Any(w => StrictlyDominates(u, w) && StrictlyDominates(w, v));
        if (!StrictlyDominates(u, v)) {
            return false;
        }

        var vdoms = strictDominators[v];
        var udommed = strictlyDominated[u];
        return vdoms.IntersectionIsEmpty(udommed);
    }

    public override bool Dominates(TVertex d, TVertex n) {
        return dominators[n][vertIndices[d]];
    }

    public override IEnumerable<TVertex> DominatorsOf(TVertex v) {
        return dominators[v].EnumerateSetBits().Select(i => preorder[i]);
    }
    
    public override IOption<TVertex> ImmediateDominatorOf(TVertex v) {
        if (!immediateDominators.TryGetValue(v, out var value)) {
            return Option.None<TVertex>();
        }
        return value.Some();
    }
}

public static class DataFlowDominatorSearchAlgorithm {
    public static DataFlowDominatorSearchAlgorithm<TGraph, TVertex, TEdge> Create<TGraph, TVertex, TEdge>(TGraph graph,
        TVertex root,
        Func<TVertex, TVertex, TEdge> edgeFactory,
        IEqualityComparer<TVertex>? comparer = null)
        where TEdge : IEdge<TVertex> where TVertex : notnull where TGraph : IBidirectionalGraph<TVertex, TEdge> {
        return new(graph, root, edgeFactory, comparer);
    }
}
