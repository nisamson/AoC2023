#region license
// AoC2023 - AoC.Support - DominatorSearchAlgorithm.cs
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

using AoC.Support.Functional;
using QuikGraph;

namespace AoC.Support.Graphs;

public abstract class DominatorSearchAlgorithm<TGraph, TVertex, TEdge>
    where TEdge : IEdge<TVertex>
    where TVertex : notnull
    where TGraph : IBidirectionalGraph<TVertex, TEdge> {
    protected readonly BidirectionalMatrixPartialGraph<TVertex, TEdge> Graph;
    protected readonly TVertex Root;
    protected readonly IEqualityComparer<TVertex> Comparer;
    protected readonly Func<TVertex, TVertex, TEdge> EdgeFactory;
    
    protected DominatorSearchAlgorithm(TGraph graph, TVertex root, Func<TVertex, TVertex, TEdge> edgeFactory, IEqualityComparer<TVertex>? comparer = null) {
        if (graph is BidirectionalMatrixPartialGraph<TVertex, TEdge> m) {
            this.Graph = m;
        }
        this.Graph = BidirectionalMatrixPartialGraph<TVertex, TEdge>.Create(graph, edgeFactory, comparer);
        this.Root = root;
        this.EdgeFactory = edgeFactory;
        this.Comparer = comparer ?? EqualityComparer<TVertex>.Default;
    }
    
    public abstract void Compute();

    public virtual BidirectionalMatrixPartialGraph<TVertex, TEdge> DominatorTree {
        get {
            var tree = new BidirectionalMatrixPartialGraph<TVertex, TEdge>(Graph.VertexCount, EdgeFactory, Comparer);
            foreach (var v in Graph.Vertices) {
                var idom = ImmediateDominatorOf(v);
                if (idom.IsNone) {
                    continue;
                }

                tree.AddVerticesAndEdge(EdgeFactory(idom.Value, v));
            }

            return tree;
        }
    }
    public abstract bool Dominates(TVertex d, TVertex n);

    public virtual bool StrictlyDominates(TVertex d, TVertex n) {
        return Dominates(d, n) && !Comparer.Equals(d, n);
    }
    
    public virtual IEnumerable<TVertex> DominatorsOf(TVertex v) {
        return Graph.Vertices.Where(u => Dominates(u, v));
    }
    
    public virtual bool ImmediatelyDominates(TVertex u, TVertex v) {
        return StrictlyDominates(u, v) && !DominatorsOf(v).Any(w => StrictlyDominates(u, w) && StrictlyDominates(w, v));
    }
    
    public abstract IOption<TVertex> ImmediateDominatorOf(TVertex v);
}
