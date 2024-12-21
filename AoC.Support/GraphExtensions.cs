#region license

// AoC2023 - AoC.Support - GraphExtensions.cs
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

using QuikGraph;

namespace AoC.Support;

public static class GraphExtensions {
    public static IEnumerable<TVertex> DfsPreorder<TVertex, TEdge>(this IVertexListGraph<TVertex, TEdge> graph,
        TVertex root,
        IEqualityComparer<TVertex>? comparer = null)
        where TEdge : IEdge<TVertex> {
        comparer ??= EqualityComparer<TVertex>.Default;
        var stack = new Stack<TVertex>();
        var visited = new HashSet<TVertex>(comparer);
        stack.Push(root);
        while (stack.Count > 0) {
            var v = stack.Pop();
            if (!visited.Add(v)) continue;

            yield return v;
            foreach (var edge in graph.OutEdges(v)) stack.Push(edge.Target);
        }
    }


    // public static IDictionary<TVertex, int> ShortestPathLengthsFrom<TVertex, TEdge>(this IVertexListGraph<TVertex, TEdge> graph,
    //     TVertex root,
    //     Func<TVertex, TVertex, int> edgeCost,
    //     IEqualityComparer<TVertex>? comparer = null)
    //     where TEdge : IEdge<TVertex> {
    //     
    //     comparer ??= EqualityComparer<TVertex>.Default;
    //     var queue = new PriorityQueue<TVertex, int>();
    //     
    // }

    public static IEnumerable<Edge<TVertex>> DfsPreorderEdges<TVertex, TEdge>(
        this IVertexListGraph<TVertex, TEdge> graph,
        TVertex root,
        IEqualityComparer<TVertex>? comparer = null) where TEdge : IEdge<TVertex> {
        comparer ??= EqualityComparer<TVertex>.Default;
        var stack = new Stack<TVertex>();
        var visited = new HashSet<TVertex>(comparer);
        stack.Push(root);
        while (stack.Count > 0) {
            var v = stack.Pop();
            if (!visited.Add(v)) continue;

            foreach (var edge in graph.OutEdges(v)) {
                if (visited.Contains(edge.Target)) continue;

                stack.Push(edge.Target);
                yield return new Edge<TVertex>(v, edge.Target);
            }
        }
    }
}

public readonly record struct ValueEdge<TVertex>(TVertex Source, TVertex Target)
    : IEdge<TVertex> where TVertex : notnull {
    
    public override string ToString() => $"{Source} -> {Target}";
    public ValueEdge<TVertex> Reversed() => new(Target, Source);
    public static ValueEdge<TVertex> Create(TVertex source, TVertex target) => new(source, target);
    
}