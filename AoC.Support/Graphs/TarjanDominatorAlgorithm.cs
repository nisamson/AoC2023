#region license

// AoC2023 - AoC.Support - TarjanDominatorAlgorithm.cs
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

using System.Diagnostics;
using QuikGraph;

namespace AoC.Support.Graphs;

// https://www.cs.utexas.edu/users/misra/Lengauer+Tarjan.pdf
public class TarjanDominatorAlgorithm<TGraph, TVertex, TEdge>
    where TGraph : IBidirectionalGraph<TVertex, TEdge>
    where TVertex : notnull
    where TEdge : IEdge<TVertex> {

    private readonly TGraph graph;
    private readonly TGraph reachabilityGraph;
    private readonly BidirectionalMatrixPartialGraph<TVertex, Edge<TVertex>> dfsTree;
    private readonly TVertex root;
    private readonly IEqualityComparer<TVertex> comparer;

    // id
    private readonly Dictionary<TVertex, TVertex> immediateDominators;
    private readonly Dictionary<TVertex, TVertex> semiDominators;
    private readonly Dictionary<TVertex, int> preorderIndices;
    private readonly List<TVertex> preorderVertices;

    private bool IsDescendantOf(TVertex v, TVertex u) {
        return reachabilityGraph.ContainsEdge(v, u);
    }
    
    private bool IsAncestorOf(TVertex v, TVertex u) {
        return reachabilityGraph.ContainsEdge(u, v);
    }
    
    private bool IsProperAncestorOf(TVertex v, TVertex u) {
        return IsAncestorOf(v, u) && !comparer.Equals(v, u);
    }
    
    private bool IsProperDescendantOf(TVertex v, TVertex u) {
        return IsDescendantOf(v, u) && !comparer.Equals(v, u);
    }

    private IEnumerable<TVertex> ProperDescendantsOf(TVertex v) {
        return reachabilityGraph.OutEdges(v)
            .Select(e => e.Target)
            .Where(w => !comparer.Equals(v, w));
    }
    
    private bool IsOnProperPath(TVertex v, TVertex u, TVertex w) {
        return IsProperAncestorOf(v, u) && IsAncestorOf(u, w);
    }

    private IEnumerable<TVertex> VerticesOnProperPath(TVertex v, TVertex w) {
        return ProperDescendantsOf(v)
            .Where(u => IsOnProperPath(v, u, w));
    }

    private void ComputePreorder() {
        foreach (var v in graph.DfsPreorder(root)) {
            preorderIndices.Add(v, preorderVertices.Count);
            preorderVertices.Add(v);
        }
    }

    private void ComputeImmediateDominators() {
        var buckets = preorderVertices.ToDictionary(v => v, _ => new HashSet<TVertex>(comparer), comparer);

        foreach (var w in preorderVertices.AsEnumerable().Reverse()) {
            ComputeSemiDominator(w);
            buckets[semiDominators[w]].Add(w);
        }

        TVertex? wbar = default;
        foreach (var (k, bucket) in buckets) {
            foreach (var w in bucket) {
                Debug.Assert(comparer.Equals(semiDominators[w], k));
                var sdwbar = VerticesOnProperPath(k, w).MinBy(v => preorderIndices[v]);
                wbar = VerticesOnProperPath(k, w).First(v => comparer.Equals(semiDominators[v], sdwbar));
                Debug.Assert(comparer.Equals(semiDominators[wbar], sdwbar));
            }
        }

        ArgumentNullException.ThrowIfNull(wbar);
        foreach (var w in preorderVertices) {
            if (comparer.Equals(w, wbar)) {
                immediateDominators[w] = semiDominators[w];
            } else {
                immediateDominators[w] = immediateDominators[wbar];
            }
        }
    }

    private void ComputeSemiDominator(TVertex v) {
        throw new NotImplementedException();
    }

}
