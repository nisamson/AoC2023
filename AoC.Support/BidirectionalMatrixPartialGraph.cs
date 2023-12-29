#region license

// AoC2023 - AoC.Support - BidirectionalMatrixSubsetGraph.cs
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
using System.Diagnostics.CodeAnalysis;
using ILGPU;
using ILGPU.Runtime;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Single;
using QuikGraph;

namespace AoC.Support;

// Represents a subset of a bidirectional matrix graph.
public class BidirectionalMatrixPartialGraph<TVertex, TEdge> : IMutableBidirectionalGraph<TVertex, TEdge>,
                                                               ICloneable where TEdge : IEdge<TVertex> where TVertex : notnull {
    private readonly Func<TVertex, TVertex, TEdge> edgeFactory;
    private readonly TVertex?[] vertices;
    private readonly Dictionary<TVertex, int> vertexIndices;
    private readonly IEqualityComparer<TVertex> vertexComparer;
    private readonly Matrix<float> matrix;
    private readonly Queue<int> emptyIndices = new();

    public BidirectionalMatrixPartialGraph(int vertexCount,
        Func<TVertex, TVertex, TEdge> edgeFactory,
        IEqualityComparer<TVertex>? vertexComparer = null) {
        this.edgeFactory = edgeFactory;
        vertices = new TVertex[vertexCount];
        vertexIndices = new Dictionary<TVertex, int>(vertexCount);
        this.vertexComparer = vertexComparer ?? EqualityComparer<TVertex>.Default;
        matrix = DenseMatrix.Create(vertexCount, vertexCount, 0f);
        emptyIndices.EnsureCapacity(vertexCount);
        for (var i = 0; i < vertexCount; i++) {
            emptyIndices.Enqueue(i);
        }
    }

    private BidirectionalMatrixPartialGraph(BidirectionalMatrixPartialGraph<TVertex, TEdge> other, Matrix<float>? matrix = null) {
        edgeFactory = other.edgeFactory;
        vertices = other.vertices.ToArray();
        vertexIndices = other.vertexIndices.ToDictionary(kvp => kvp.Key, kvp => kvp.Value, vertexComparer);
        vertexComparer = other.vertexComparer;
        this.matrix = matrix ?? other.matrix.Clone();
    }


    public bool IsDirected => true;
    public bool AllowParallelEdges => false;

    public bool ContainsVertex(TVertex vertex) {
        return vertexIndices.ContainsKey(vertex);
    }

    private int GetVertexIndex(TVertex vertex) {
        if (!vertexIndices.TryGetValue(vertex, out var index)) {
            throw new ArgumentException($"Vertex {vertex} is not in the graph");
        }

        return index;
    }

    private TVertex? GetVertex(int index) {
        return vertices[index];
    }

    private int? GetNextEmptyIndex() {
        if (emptyIndices.TryDequeue(out var index)) {
            return index;
        }

        return null;
    }

    public bool IsOutEdgesEmpty(TVertex vertex) {
        return OutEdges(vertex).FirstOrDefault() == null;
    }

    public int OutDegree(TVertex vertex) {
        return OutEdgeIndices(GetVertexIndex(vertex)).Count();
    }

    private IEnumerable<int> OutEdgeIndices(int vertexIndex) {
        for (var i = 0; i < vertices.Length; i++) {
            if (matrix[vertexIndex, i] != 0) {
                yield return i;
            }
        }
    }

    public IEnumerable<TEdge> OutEdges(TVertex vertex) {
        var row = GetVertexIndex(vertex);
        return OutEdgeIndices(row).Select(i => edgeFactory(vertex, GetVertex(i)!));
    }

    public bool TryGetOutEdges(TVertex vertex, [UnscopedRef] out IEnumerable<TEdge>? edges) {
        if (vertexIndices.TryGetValue(vertex, out _)) {
            edges = OutEdges(vertex);
            return true;
        }

        edges = null;
        return false;
    }

    public TEdge OutEdge(TVertex vertex, int index) {
        var row = GetVertexIndex(vertex);
        var col = OutEdgeIndices(row).ElementAt(index);
        return edgeFactory(vertex, GetVertex(col)!);
    }

    public bool ContainsEdge(TVertex source, TVertex target) {
        return matrix[GetVertexIndex(source), GetVertexIndex(target)] != 0;
    }

    public bool TryGetEdge(TVertex source, TVertex target, [UnscopedRef] out TEdge edge) {
        if (matrix[GetVertexIndex(source), GetVertexIndex(target)] != 0) {
            edge = edgeFactory(source, target);
            return true;
        }

        edge = default(TEdge)!;
        return false;
    }

    public bool TryGetEdges(TVertex source, TVertex target, [UnscopedRef] out IEnumerable<TEdge>? edges) {
        if (matrix[GetVertexIndex(source), GetVertexIndex(target)] != 0) {
            edges = new[] { edgeFactory(source, target) };
            return true;
        }

        edges = null;
        return false;
    }

    public bool IsVerticesEmpty => vertexIndices.Count == 0;
    public int VertexCount => vertexIndices.Count;
    public IEnumerable<TVertex> Vertices => vertexIndices.Keys;

    public bool ContainsEdge(TEdge edge) {
        return matrix[GetVertexIndex(edge.Source), GetVertexIndex(edge.Target)] != 0;
    }

    public bool IsEdgesEmpty => EdgeCount == 0;
    public int EdgeCount => int.CreateChecked(matrix.ColumnSums().Sum());

    public IEnumerable<TEdge> Edges => matrix.EnumerateIndexed(Zeros.Include)
        .Where(t => t.Item3 != 0)
        .Select(t => edgeFactory(GetVertex(t.Item1)!, GetVertex(t.Item2)!));

    public bool IsInEdgesEmpty(TVertex vertex) {
        return InEdges(vertex).FirstOrDefault() == null;
    }

    public int InDegree(TVertex vertex) {
        return InEdges(vertex).Count();
    }

    public IEnumerable<TEdge> InEdges(TVertex vertex) {
        return matrix.Row(GetVertexIndex(vertex))
            .Select((b, i) => (b, i))
            .Where(t => t.b != 0)
            .Select(t => edgeFactory(GetVertex(t.i)!, vertex));
    }

    public bool TryGetInEdges(TVertex vertex, [UnscopedRef] out IEnumerable<TEdge> edges) {
        if (vertexIndices.TryGetValue(vertex, out _)) {
            edges = InEdges(vertex);
            return true;
        }

        edges = null;
        return false;
    }

    public TEdge InEdge(TVertex vertex, int index) {
        var col = InEdges(vertex).ElementAt(index);
        return col;
    }

    public int Degree(TVertex vertex) {
        return InDegree(vertex) + OutDegree(vertex);
    }

    public object Clone() {
        return new BidirectionalMatrixPartialGraph<TVertex, TEdge>(this);
    }

    public void Clear() {
        vertices.Initialize();
        vertexIndices.Clear();
        matrix.Clear();
    }

    public int RemoveOutEdgeIf(TVertex vertex, EdgePredicate<TVertex, TEdge> predicate) {
        var delEdges = OutEdges(vertex).Where(predicate.Invoke).ToList();
        foreach (var edge in delEdges) {
            RemoveEdge(edge);
        }

        return delEdges.Count;
    }

    public void ClearOutEdges(TVertex vertex) {
        foreach (var edge in OutEdges(vertex)) {
            RemoveEdge(edge);
        }
    }

    public void TrimEdgeExcess() { }

    public bool AddVertex(TVertex vertex) {
        if (vertexIndices.ContainsKey(vertex)) {
            return false;
        }

        var index = GetNextEmptyIndex();
        if (index is not { } i) {
            throw new InvalidOperationException("Graph is full");
        }

        vertices[i] = vertex;
        vertexIndices[vertex] = i;
        VertexAdded?.Invoke(vertex);
        return true;
    }

    public int AddVertexRange(IEnumerable<TVertex> vertices) {
        return vertices.Count(AddVertex);
    }

    public bool RemoveVertex(TVertex vertex) {
        if (!vertexIndices.TryGetValue(vertex, out var index)) {
            return false;
        }

        vertices[index] = default(TVertex?);
        vertexIndices.Remove(vertex);
        emptyIndices.Enqueue(index);
        VertexRemoved?.Invoke(vertex);
        return true;
    }

    public int RemoveVertexIf(VertexPredicate<TVertex> predicate) {
        var delVertices = Vertices.Where(predicate.Invoke).ToList();
        foreach (var vertex in delVertices) {
            RemoveVertex(vertex);
        }

        return delVertices.Count;
    }

    public event VertexAction<TVertex>? VertexAdded;
    public event VertexAction<TVertex>? VertexRemoved;

    public bool AddEdge(TEdge edge) {
        var sourceIndex = GetVertexIndex(edge.Source);
        var targetIndex = GetVertexIndex(edge.Target);
        if (matrix[sourceIndex, targetIndex] != 0) {
            return false;
        }

        matrix[sourceIndex, targetIndex] = 1;
        EdgeAdded?.Invoke(edge);
        return true;
    }

    public int AddEdgeRange(IEnumerable<TEdge> edges) {
        return edges.Count(AddEdge);
    }

    public bool RemoveEdge(TEdge edge) {
        var sourceIndex = GetVertexIndex(edge.Source);
        var targetIndex = GetVertexIndex(edge.Target);
        if (matrix[sourceIndex, targetIndex] == 0) {
            return false;
        }

        matrix[sourceIndex, targetIndex] = 0;
        EdgeRemoved?.Invoke(edge);
        return true;
    }

    public int RemoveEdgeIf(EdgePredicate<TVertex, TEdge> predicate) {
        var delEdges = Edges.Where(predicate.Invoke).ToList();
        foreach (var edge in delEdges) {
            RemoveEdge(edge);
        }

        return delEdges.Count;
    }

    public event EdgeAction<TVertex, TEdge>? EdgeAdded;
    public event EdgeAction<TVertex, TEdge>? EdgeRemoved;

    public bool AddVerticesAndEdge(TEdge edge) {
        AddVertex(edge.Source);
        AddVertex(edge.Target);

        if (matrix[GetVertexIndex(edge.Source), GetVertexIndex(edge.Target)] != 0) {
            return false;
        }

        matrix[GetVertexIndex(edge.Source), GetVertexIndex(edge.Target)] = 1;

        EdgeAdded?.Invoke(edge);
        return true;
    }

    public int AddVerticesAndEdgeRange(IEnumerable<TEdge> edges) {
        return edges.Count(AddVerticesAndEdge);
    }

    public int RemoveInEdgeIf(TVertex vertex, EdgePredicate<TVertex, TEdge> predicate) {
        var delEdges = InEdges(vertex).Where(predicate.Invoke).ToList();
        foreach (var edge in delEdges) {
            RemoveEdge(edge);
        }

        return delEdges.Count;
    }

    public void ClearInEdges(TVertex vertex) {
        foreach (var edge in InEdges(vertex)) {
            RemoveEdge(edge);
        }
    }

    public void ClearEdges(TVertex vertex) {
        ClearInEdges(vertex);
        ClearOutEdges(vertex);
    }
    
    public IEnumerable<TVertex> GetReachableFromWithout(TVertex start, TVertex without) {
        var withoutIndex = GetVertexIndex(without);
        var row = matrix.Row(withoutIndex);
        var col = matrix.Column(withoutIndex);
        matrix.ClearRow(withoutIndex);
        matrix.ClearColumn(withoutIndex);

        var res = this.DfsPreorder(start, vertexComparer).ToList();
        matrix.SetRow(withoutIndex, row);
        matrix.SetColumn(withoutIndex, col);
        return res;
    }

    // public BidirectionalMatrixSubsetGraph<TVertex, TEdge> ComputeTransitiveClosure() {
    //     Matrix<float> oldM;
    //     var m = matrix;
    //     using var acc = new MathAccelerator();
    //     do {
    //         oldM = m;
    //         m = acc.MultipleMatrixRowMajorTiling(m, m);
    //         m.ToBooleanInplace();
    //     } while (!m.Equals(oldM));
    //
    //     var res = new BidirectionalMatrixSubsetGraph<TVertex, TEdge>(this);
    //     m.CopyTo(res.matrix);
    //     return res;
    // }

    private const int UseGpuThreshold = 250;
    public BidirectionalMatrixPartialGraph<TVertex, TEdge> ComputeTransitiveClosure() {
        
        using var acc = matrix.ColumnCount switch {
            > UseGpuThreshold => new MathAccelerator(MathMode.Fast),
            _ => new MathAccelerator(MathMode.Fast, true),
        };
        var start = Stopwatch.StartNew();
        var arr = acc.WarshallReachabilityMatrixGrouping(matrix.ToBytes(), true);
        // var arr = matrix.ToBytes().CalculateWarshallAlgorithmRowParallel();
        var end = start.Elapsed;
        Console.WriteLine($"Warshall took {end.TotalMicroseconds} \u03BCs");
        var res = new BidirectionalMatrixPartialGraph<TVertex, TEdge>(this, arr.ToMatrix<float>());
        return res;
    }

    public static BidirectionalMatrixPartialGraph<TVertex, TEdge> Create(IBidirectionalGraph<TVertex, TEdge> edgeSet,
        Func<TVertex, TVertex, TEdge> edgeFactory,
        IEqualityComparer<TVertex>? comparer = null) {
        var edges = edgeSet.Edges;
        var vertCount = edgeSet.Vertices.Count(v => edgeSet.OutDegree(v) + edgeSet.InDegree(v) > 0);
        var res = new BidirectionalMatrixPartialGraph<TVertex, TEdge>(vertCount, edgeFactory, comparer);
        res.AddVerticesAndEdgeRange(edges);
        return res;
    }
}
