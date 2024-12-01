#region license

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

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using AoC.Support.Collections;
using AoC.Support.Graphs;
using QuikGraph;
using QuikGraph.Graphviz;
using QuikGraph.Graphviz.Dot;

namespace AoC.Support;

public class LongestSimplePathAlgorithm<TVertex, TEdge, TGraph>
    where TEdge : IEdge<TVertex>
    where TGraph : IBidirectionalGraph<TVertex, TEdge>
    where TVertex : notnull {
    private readonly IEqualityComparer<TVertex> comparer;
    private readonly Func<IEdge<TVertex>, double> cost;
    private readonly Func<TVertex, TVertex, TEdge> edgeFactory;
    private readonly KnownPopulationSet<TVertex>.Generator vertexSetGenerator;
    private BidirectionalMatrixPartialGraph<TVertex, TEdge>? dominatorTree;
    private IDictionary<TVertex, KnownPopulationSet<TVertex>> reachableFrom;

    private BidirectionalMatrixPartialGraph<TVertex, TEdge>? transitiveClosure;

    public LongestSimplePathAlgorithm(TGraph visitedGraph,
        TVertex root,
        TVertex dest,
        Func<TVertex, TVertex, TEdge> edgeFactory,
        IEqualityComparer<TVertex>? comparer = null,
        Func<IEdge<TVertex>, double>? cost = null) {
        this.comparer = comparer ?? EqualityComparer<TVertex>.Default;
        Graph = BidirectionalMatrixPartialGraph<TVertex, TEdge>.Create(visitedGraph, edgeFactory, this.comparer);
        Root = root;
        Dest = dest;
        this.edgeFactory = edgeFactory;
        vertexSetGenerator = KnownPopulationSet.CreateGenerator(Graph.Vertices, this.comparer);
        this.cost = cost ?? (_ => 1);
    }

    public TVertex Dest { get; }

    public TVertex Root { get; }

    private BidirectionalMatrixPartialGraph<TVertex, TEdge> Graph { get; }

    private BidirectionalMatrixPartialGraph<TVertex, TEdge> DominatorTree {
        [MemberNotNull(nameof(dominatorTree))]
        get {
            if (dominatorTree != null) return dominatorTree;

            // var alg = NaiveDominatorSearchAlgorithm.Create(Graph,  Root, edgeFactory, comparer);
            var alg = DataFlowDominatorSearchAlgorithm.Create(Graph, Root, edgeFactory, comparer);
            alg.Compute();
            dominatorTree = alg.DominatorTree;
            var renderer = new GraphvizAlgorithm<TVertex, TEdge>(dominatorTree);
            renderer.FormatVertex += (sender, args) => {
                if (Root.Equals(args.Vertex)) args.VertexFormat.FillColor = GraphvizColor.Lavender;

                if (Dest.Equals(args.Vertex)) args.VertexFormat.FillColor = GraphvizColor.LightPink;

                args.VertexFormat.Label = args.Vertex.ToString();
            };
            File.WriteAllText($"{Graph.VertexCount}-{DateTime.Now.ToFileTimeUtc()}.dot", renderer.Generate());
            return dominatorTree;
        }
    }

    private BidirectionalMatrixPartialGraph<TVertex, TEdge> TransitiveClosure {
        get {
            if (transitiveClosure == null) CalculateReachableFrom();

            return transitiveClosure;
        }
    }

    public IEnumerable<TVertex> Compute() {
        // Console.WriteLine("Calculating dominators.");
        // CalculateDominators();
        Console.WriteLine("Calculating reachable from.");
        CalculateReachableFrom();
        Console.WriteLine("Calculating longest path.");
        return LongestPathBetween(Root, Dest);
    }

    private List<TVertex> LongestPathBetween(TVertex source, TVertex dest) {
        Console.WriteLine($"Calculating longest path between {source} and {dest}");
        if (!TransitiveClosure.ContainsEdge(source, dest))
            throw new ArgumentException("No path between source and dest");

        // var dag = DominatorTree.ShortestPathsDag(_ => 1, source);
        // if (!dag.Invoke(dest, out var path)) {
        //     throw new Exception("No path found");
        // }

        var visitedNodes = new HashSet<TVertex>(comparer);
        visitedNodes.Add(source);
        var longestPath = new List<TVertex> { source };
        // var dominatorPath = new List<TVertex>(path.Select(e => e.Target));
        // var pairs = source.Once().Concat(dominatorPath).Zip(dominatorPath);
        var pairs = new[] { (Root, Dest) };
        foreach (var (cur, next) in pairs) {
            var nextStep =
                LongestPathBetweenDominators(longestPath.Take(longestPath.Count - 1), visitedNodes, cur, next);
            longestPath.AddRange(nextStep.Skip(longestPath.Count));
            visitedNodes.UnionWith(longestPath);
        }

        return longestPath;
    }


    // precondition: source is the immediate dominator of dest
    private IEnumerable<TVertex> LongestPathBetweenDominators(IEnumerable<TVertex> currentPath,
        ISet<TVertex> alreadyVisited,
        TVertex source,
        TVertex dest) {
        Console.WriteLine($"Calculating longest path between {source} and {dest}");
        // if (!DominatorTree.ContainsEdge(source, dest)) {
        //     throw new ArgumentException($"${source} is not the immediate dominator of ${dest}");
        // }

        var prevPath = alreadyVisited;

        var frontier = new PriorityQueue<PathSegment, int>(Comparer<int>.Create((a, b) => b.CompareTo(a)));
        // var frontier = new Stack<PathSegment>();
        var dominant = new Dictionary<TVertex, PathSegment>(comparer);
        var curPathQueue = ImmutableQueue.CreateRange(currentPath);
        var origin = new PathSegment(source, vertexSetGenerator.CreateSet(curPathQueue), curPathQueue,
            reachableFrom[source], comparer);
        frontier.Enqueue(origin, origin.Reachable.Count);
        // frontier.Push(origin);
        var cnt = 0;
        var probed = 0;
        while (frontier.Count > 0) {
            cnt++;
            if (cnt % 5000 == 0) {
                var curCnt = frontier.Count;
                Console.WriteLine(
                    $"Frontier size at {cnt}: {frontier.Count}, probed: {probed}, rejected: {probed - cnt}");
                if (dominant.TryGetValue(dest, out var ps)) {
                    Console.WriteLine($"Best path so far of length {ps.Length}");
                }
                else {
                    Console.WriteLine("No path candidate found yet");
                    var best = frontier.Peek();
                    Console.WriteLine($"Best path being investigated ends at {best.Vertex} of length {best.Length}");
                }
                // prune dominated paths
                // var frontierList = frontier.UnorderedItems.Where(
                //     e => {
                //         if (!dominant.TryGetValue(e.Element.Vertex, out var dom)) {
                //             return true;
                //         }
                //
                //         return !dom.Dominates(e.Element);
                //     }
                // );
                // frontier = new PriorityQueue<PathSegment, int>(
                //     frontierList,
                //     Comparer<int>.Create((a, b) => b.CompareTo(a))
                // );
                // Console.WriteLine($"Pruned {curCnt - frontier.Count} dominated paths");
            }

            var state = frontier.Dequeue();
            // var state = frontier.Pop();

            if (!dominant.TryAdd(state.Vertex, state)) {
                if (dominant[state.Vertex].Dominates(state)) continue;

                dominant[state.Vertex] = state;
            }

            // We've already visited the destination, no point in continuing.
            if (state.Visits(dest)) continue;

            foreach (var e in Graph.OutEdges(state.Vertex)) {
                probed++;
                if (prevPath.Contains(e.Target)) continue;

                if (state.Visits(e.Target)) continue;

                if (!CanReachDestFrom(e.Target)) continue;

                if (!comparer.Equals(e.Target, dest) && !TransitiveClosure.ContainsEdge(e.Target, dest)) continue;

                var newVisited = state.Visited.Clone();
                newVisited.Add(state.Vertex);
                var newState = new PathSegment(
                    e.Target,
                    newVisited,
                    state.Path.Enqueue(state.Vertex),
                    reachableFrom[e.Target],
                    comparer
                );

                if (dominant.TryGetValue(newState.Vertex, out var curDom))
                    if (curDom.Dominates(newState))
                        continue;

                frontier.Enqueue(newState, newState.Reachable.Count);
                // frontier.Push(newState);
            }
        }

        if (!dominant.TryGetValue(dest, out var res)) throw new InvalidOperationException("No path found");

        return res.EnumeratePath();
    }

    [MemberNotNull(nameof(transitiveClosure), nameof(reachableFrom))]
    private void CalculateReachableFrom() {
        if (transitiveClosure != null && reachableFrom != null) return;

        var subGraph = BidirectionalMatrixPartialGraph<TVertex, TEdge>.Create(Graph, edgeFactory, comparer);
        var tc = subGraph.ComputeTransitiveClosure();
        transitiveClosure = tc;

        reachableFrom = new Dictionary<TVertex, KnownPopulationSet<TVertex>>(comparer);
        foreach (var v in Graph.Vertices) {
            var reach = vertexSetGenerator.CreateSet();
            if (subGraph.ContainsVertex(v)) reach.UnionWith(TransitiveClosure.OutEdges(v).Select(e => e.Target));

            reachableFrom[v] = reach;
        }
    }

    [MemberNotNull(nameof(dominatorTree))]
    private void CalculateDominators() {
        _ = DominatorTree;
    }

    private bool CanReachDestFrom(TVertex v) {
        return TransitiveClosure.ContainsEdge(v, Dest);
    }

    private class PathSegment : IEquatable<PathSegment> {
        public PathSegment(TVertex vertex,
            KnownPopulationSet<TVertex> visited,
            ImmutableQueue<TVertex> path,
            KnownPopulationSet<TVertex> reachableFromVertex,
            IEqualityComparer<TVertex> comparer) {
            Vertex = vertex;
            Visited = visited;
            Path = path;
            var localKnown = reachableFromVertex.Clone();
            localKnown.ExceptWith(Visited);
            localKnown.Remove(vertex);
            Reachable = localKnown;
            Comparer = comparer;
        }

        public TVertex Vertex { get; }
        public KnownPopulationSet<TVertex> Visited { get; }
        public ImmutableQueue<TVertex> Path { get; }
        public KnownPopulationSet<TVertex> Reachable { get; }

        public IEqualityComparer<TVertex> Comparer { get; }

        public int Length => Visited.Count + 1;

        public bool Equals(PathSegment? other) {
            if (ReferenceEquals(null, other)) return false;

            if (ReferenceEquals(this, other)) return true;

            return Symmetric(other);
        }

        public bool Visits(TVertex v) {
            return Visited.Contains(v) || Comparer.Equals(Vertex, v);
        }

        public bool Symmetric(PathSegment other) {
            return Comparer.Equals(other.Vertex, Vertex) && Visited.SetEquals(other.Visited);
        }

        public override bool Equals(object? obj) {
            return obj is PathSegment other && Equals(other);
        }

        public bool Dominates(PathSegment other, bool proper = false) {
            if (!Comparer.Equals(Vertex, other.Vertex)) return false;

            if (Length < other.Length) return false;

            if (proper) return Reachable.IsProperSupersetOf(other.Reachable);

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
            return Path.Append(Vertex);
        }
    }

    private class PathState {
        public PathState(TVertex current, ImmutableHashSet<TVertex> reachable) {
            Current = current;
            Reachable = reachable;
        }

        public TVertex Current { get; }
        public ImmutableHashSet<TVertex> Reachable { get; }
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
        var alg = new LongestSimplePathAlgorithm<TVertex, TEdge, TGraph>(graph, root, dest, edgeFactory, comparer,
            cost);
        return alg.Compute();
    }
}