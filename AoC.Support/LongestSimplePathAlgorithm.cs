﻿#region license

// AoC2023 - AoC.Support - LongestSimplePathAlgorithm.cs
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

using System.Collections;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using QuikGraph;
using QuikGraph.Algorithms;
using QuikGraph.Algorithms.Services;

namespace AoC.Support;

public class LongestSimplePathAlgorithm<TVertex, TEdge, TGraph>
    where TEdge : IEdge<TVertex>
    where TGraph : IBidirectionalGraph<TVertex, TEdge>
    where TVertex : notnull {
    private readonly IEqualityComparer<TVertex> comparer;
    private readonly Func<IEdge<TVertex>, double> cost;
    private readonly Func<TVertex, TVertex, TEdge> edgeFactory;

    private BidirectionalMatrixPartialGraph<TVertex, TEdge>? transitiveClosure;
    private IDictionary<TVertex, ImmutableHashSet<TVertex>> reachableFrom;

    private class PathSegment : IEquatable<PathSegment> {
        
        public PathSegment(TVertex vertex, ImmutableOrderedHashSet<TVertex> visited, ImmutableHashSet<TVertex> reachableFromVertex) {
            Vertex = vertex;
            Visited = visited;
            Reachable = reachableFromVertex.Except(Visited).Remove(vertex);
        }

        public TVertex Vertex { get; }
        public ImmutableOrderedHashSet<TVertex> Visited { get; }
        public ImmutableHashSet<TVertex> Reachable { get; }
        public bool Visits(TVertex v) => Visited.Contains(v) || Visited.EqualityComparer.Equals(Vertex, v);
        public int Length => Visited.Count + 1;

        public bool Symmetric(PathSegment other) {
            return ((IEqualityComparer) Visited.EqualityComparer).Equals(other.Vertex, Vertex) && Visited.SetEquals(other.Visited);
        }

        public override bool Equals(object? obj) {
            return obj is PathSegment other && Equals(other);
        }

        public bool Equals(PathSegment? other) {
            if (ReferenceEquals(null, other)) {
                return false;
            }

            if (ReferenceEquals(this, other)) {
                return true;
            }

            return Symmetric(other);
        }

        public bool Dominates(PathSegment other) {
            if (!Visited.EqualityComparer.Equals(Vertex, other.Vertex)) {
                return false;
            }

            if (Length < other.Length) {
                return false;
            }

            return Reachable.IsSupersetOf(other.Reachable);
        }

        public override int GetHashCode() {
            return HashCode.Combine(Vertex, Visited);
        }

        public static bool operator ==(PathSegment? left, PathSegment? right) {
            return Equals(left, right);
        }

        public static bool operator !=(PathSegment? left, PathSegment? right) {
            return !Equals(left, right);
        }

        public IEnumerable<TVertex> EnumeratePath() {
            return Visited.Append(Vertex);
        }
    }

    private class PathState {
        public TVertex Current { get; }
        public ImmutableHashSet<TVertex> Reachable { get; }
        
        public PathState(TVertex current, ImmutableHashSet<TVertex> reachable) {
            Current = current;
            Reachable = reachable;
        }
    }

    public LongestSimplePathAlgorithm(TGraph visitedGraph,
        TVertex root,
        TVertex dest,
        Func<TVertex, TVertex, TEdge> edgeFactory,
        IEqualityComparer<TVertex>? comparer = null,
        Func<IEdge<TVertex>, double>? cost = null) {
        Graph = visitedGraph;
        Root = root;
        Dest = dest;
        this.edgeFactory = edgeFactory;
        this.comparer = comparer ?? EqualityComparer<TVertex>.Default;
        this.cost = cost ?? (_ => 1);
    }

    public TVertex Dest { get; }

    public TVertex Root { get; }

    public TGraph Graph { get; }

    private ImmutableHashSet<TVertex> ReachableFromState(PathSegment state) {
        var res = ImmutableHashSet<TVertex>.Empty.WithComparer(comparer);
        res = res.Add(state.Vertex);
        res = res.Union(state.Visited);

        return reachableFrom[state.Vertex].Except(res);
    }

    private bool VertexEquals(TVertex a, TVertex b) {
        return ((IEqualityComparer) comparer).Equals(a, b);
    }

    private bool Dominates(PathSegment a, PathSegment b) {
        if (!VertexEquals(a.Vertex, b.Vertex)) {
            return false;
        }

        if (a.Length < b.Length) {
            return false;
        }

        var reachableFromA = ReachableFromState(a);
        var reachableFromB = ReachableFromState(b);
        return reachableFromA.IsSupersetOf(reachableFromB);
    }

    public IEnumerable<TVertex> Compute() {
        CalculateReachableFrom();
        var frontier = new PriorityQueue<PathSegment, PathSegment>(
            Comparer<PathSegment>.Create((a, b) => b.Reachable.Count.CompareTo(a.Reachable.Count))
            );
        var visited = new HashSet<PathSegment>();
        var origin = new PathSegment(Root, ImmutableOrderedHashSet<TVertex>.Empty.WithComparer(comparer), reachableFrom[Root]);
        frontier.Enqueue(origin, origin);
        var cnt = 0;
        while (frontier.Count > 0) {
            cnt++;
            if (cnt % 1000 == 0) {
                Console.WriteLine($"Frontier and visited size at {cnt}: {frontier.Count}, {visited.Count}");
            }
            var state = frontier.Dequeue();

            if (!visited.Add(state)) {
                continue;
            }

            foreach (var e in Graph.OutEdges(state.Vertex)) {
                if (state.Visits(e.Target)) {
                    continue;
                }

                if (!CanReachDestFrom(e.Target)) {
                    continue;
                }

                var newState = new PathSegment(e.Target, state.Visited.Add(state.Vertex), reachableFrom[e.Target]);

                frontier.Enqueue(newState, newState);
            }
        }

        return visited.Where(v => VertexEquals(v.Vertex, Dest)).MaxBy(p => p.Length)?.EnumeratePath() ??
               throw new InvalidOperationException("No path found");
    }

    [MemberNotNull(nameof(transitiveClosure), nameof(reachableFrom))]
    private void CalculateReachableFrom() {
        var subGraph = BidirectionalMatrixPartialGraph<TVertex, TEdge>.Create(Graph, edgeFactory, comparer);
        var tc = subGraph.ComputeTransitiveClosure();
        transitiveClosure = tc;
         
         reachableFrom = new Dictionary<TVertex, ImmutableHashSet<TVertex>>(comparer);
         foreach (var v in Graph.Vertices) {
             var reach = ImmutableHashSet<TVertex>.Empty.WithComparer(comparer);
             if (subGraph.ContainsVertex(v)) {
                 reach = reach.Union(TransitiveClosure.OutEdges(v).Select(e => e.Target));
             }
             reachableFrom[v] = reach;
         }
    }

    private bool CanReachDestFrom(TVertex v) {
        return TransitiveClosure.ContainsEdge(v, Dest);
    }

    private BidirectionalMatrixPartialGraph<TVertex, TEdge> TransitiveClosure {
        get {
            if (transitiveClosure == null) {
                CalculateReachableFrom();
            }

            return transitiveClosure;
        }
    }
}

public static class LongestSimplePathAlgorithmExtensions {
    public static IEnumerable<TVertex> LongestSimplePath<TVertex, TEdge, TGraph>(
        this TGraph graph,
        TVertex root,
        TVertex dest,
        Func<TVertex, TVertex, TEdge> edgeFactory,
        IEqualityComparer<TVertex>? comparer = null,
        Func<IEdge<TVertex>, double>? cost = null
    )
        where TEdge : IEdge<TVertex>
        where TGraph : IBidirectionalGraph<TVertex, TEdge>
        where TVertex : notnull {
        var alg = new LongestSimplePathAlgorithm<TVertex, TEdge, TGraph>(graph, root, dest, edgeFactory, comparer, cost);
        return alg.Compute();
    }
}
