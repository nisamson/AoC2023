#region license

// AoC2023 - AoC.Support - BidirectionalMatrixGraph.cs
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

using System.Diagnostics.CodeAnalysis;
using ILGPU;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Single;
using QuikGraph;

namespace AoC.Support;

using Vertex = Vertex<int>;

public class BidirectionalMatrixGraph<TVertex, TEdge> : IBidirectionalGraph<TVertex, TEdge>,
    IMutableBidirectionalGraph<TVertex, TEdge> where TEdge : IEdge<TVertex> {
    private readonly Func<TVertex, TVertex, TEdge> edgeFactory;
    private readonly Func<int, TVertex> indexToVertex;
    private readonly Matrix<float> matrix;
    private readonly Func<TVertex, int> vertexToIndex;

    public BidirectionalMatrixGraph(int vertexCount,
        Func<TVertex, int> vertexToIndex,
        Func<int, TVertex> indexToVertex,
        Func<TVertex, TVertex, TEdge> edgeFactory,
        bool sparse = true) {
        if (sparse)
            matrix = SparseMatrix.Create(vertexCount, vertexCount, 0);
        else
            matrix = DenseMatrix.Create(vertexCount, vertexCount, 0);

        this.vertexToIndex = vertexToIndex;
        this.indexToVertex = indexToVertex;
        this.edgeFactory = edgeFactory;
    }

    private BidirectionalMatrixGraph(Matrix<float> matrix,
        Func<TVertex, int> vertexToIndex,
        Func<int, TVertex> indexToVertex,
        Func<TVertex, TVertex, TEdge> edgeFactory) {
        this.matrix = matrix;
        this.vertexToIndex = vertexToIndex;
        this.indexToVertex = indexToVertex;
        this.edgeFactory = edgeFactory;
    }

    public bool IsDirected => true;
    public bool AllowParallelEdges => false;

    public bool ContainsVertex(TVertex vertex) {
        return ContainsIndex(vertexToIndex(vertex));
    }

    public bool IsOutEdgesEmpty(TVertex vertex) {
        var idx = vertexToIndex(vertex);
        return matrix.Row(idx).Sum() == 0;
    }

    public int OutDegree(TVertex vertex) {
        var idx = vertexToIndex(vertex);
        return int.CreateChecked(matrix.Row(idx).Sum());
    }

    public IEnumerable<TEdge> OutEdges(TVertex vertex) {
        var idx = vertexToIndex(vertex);
        return matrix.Row(idx)
            .EnumerateIndexed()
            .Where(t => t.Item2 != 0)
            .Select(t => edgeFactory(vertex, indexToVertex(t.Item1)));
    }

    public bool TryGetOutEdges(TVertex vertex, [UnscopedRef] out IEnumerable<TEdge> edges) {
        var idx = vertexToIndex(vertex);
        if (!ContainsIndex(idx)) {
            edges = null;
            return false;
        }

        edges = OutEdges(vertex);
        return true;
    }

    public TEdge OutEdge(TVertex vertex, int index) {
        return OutEdges(vertex).ElementAt(index);
    }

    public bool ContainsEdge(TVertex source, TVertex target) {
        var fromIndex = vertexToIndex(source);
        var toIndex = vertexToIndex(target);
        return ContainsIndex(fromIndex) && ContainsIndex(toIndex) && matrix[fromIndex, toIndex] != 0;
    }

    public bool TryGetEdge(TVertex source, TVertex target, [UnscopedRef] out TEdge edge) {
        if (!ContainsEdge(source, target)) {
            edge = default;
            return false;
        }

        edge = edgeFactory(source, target);
        return true;
    }

    public bool TryGetEdges(TVertex source, TVertex target, [UnscopedRef] out IEnumerable<TEdge> edges) {
        if (!ContainsEdge(source, target)) {
            edges = null;
            return false;
        }

        edges = new[] { edgeFactory(source, target) };
        return true;
    }

    public bool IsVerticesEmpty => false;
    public int VertexCount => matrix.RowCount;
    public IEnumerable<TVertex> Vertices => Enumerable.Range(0, matrix.RowCount).Select(indexToVertex);

    public bool ContainsEdge(TEdge edge) {
        var fromIndex = vertexToIndex(edge.Source);
        var toIndex = vertexToIndex(edge.Target);
        return ContainsIndex(fromIndex) && ContainsIndex(toIndex) && matrix[fromIndex, toIndex] != 0;
    }

    public bool IsEdgesEmpty => matrix.Exists(f => f != 0);
    public int EdgeCount => int.CreateChecked(matrix.RowSums().Sum());

    public IEnumerable<TEdge> Edges => matrix.EnumerateIndexed(Zeros.Include)
        .Where(t => t.Item3 != 0)
        .Select(t => edgeFactory(indexToVertex(t.Item1), indexToVertex(t.Item2)));

    public bool IsInEdgesEmpty(TVertex vertex) {
        var idx = vertexToIndex(vertex);
        return matrix.Column(idx).Sum() == 0;
    }

    public int InDegree(TVertex vertex) {
        var idx = vertexToIndex(vertex);
        return int.CreateChecked(matrix.Column(idx).Sum());
    }

    public IEnumerable<TEdge> InEdges(TVertex vertex) {
        var idx = vertexToIndex(vertex);
        return matrix.Column(idx)
            .EnumerateIndexed(Zeros.Include)
            .Where(t => t.Item2 != 0)
            .Select(t => edgeFactory(indexToVertex(t.Item1), vertex));
    }

    public bool TryGetInEdges(TVertex vertex, [UnscopedRef] out IEnumerable<TEdge>? edges) {
        var idx = vertexToIndex(vertex);
        if (!ContainsIndex(idx)) {
            edges = null;
            return false;
        }

        edges = InEdges(vertex);
        return true;
    }

    public TEdge InEdge(TVertex vertex, int index) {
        return InEdges(vertex).ElementAt(index);
    }

    public int Degree(TVertex vertex) {
        var idx = vertexToIndex(vertex);
        return int.CreateChecked(matrix.Row(idx).Sum())
               + int.CreateChecked(matrix.Column(idx).Sum());
    }

    public void Clear() {
        matrix.MapInplace(_ => 0);
    }

    public int RemoveOutEdgeIf(TVertex vertex, EdgePredicate<TVertex, TEdge> predicate) {
        var edges = OutEdges(vertex).Where(e => predicate(e)).ToList();
        var cnt = edges.Count;
        foreach (var edge in edges) RemoveEdge(edge);

        return cnt;
    }

    public void ClearOutEdges(TVertex vertex) {
        var idx = vertexToIndex(vertex);
        matrix.SetRow(idx, SparseVector.Create(matrix.RowCount, 0));
    }

    public void TrimEdgeExcess() { }

    public bool AddVertex(TVertex vertex) {
        if (!ContainsVertex(vertex)) throw new ArgumentOutOfRangeException(nameof(vertex));

        return false;
    }

    public int AddVertexRange(IEnumerable<TVertex> vertices) {
        return vertices.Count(AddVertex);
    }

    public bool RemoveVertex(TVertex vertex) {
        if (!ContainsVertex(vertex)) return false;

        var idx = vertexToIndex(vertex);
        matrix.SetRow(idx, SparseVector.Create(matrix.RowCount, 0));
        matrix.SetColumn(idx, SparseVector.Create(matrix.ColumnCount, 0));
        return true;
    }

    public int RemoveVertexIf(VertexPredicate<TVertex> predicate) {
        var vertices = Vertices.Where(v => predicate(v)).ToList();
        var cnt = vertices.Count;
        foreach (var vertex in vertices) RemoveVertex(vertex);

        return cnt;
    }

    public event VertexAction<TVertex>? VertexAdded;
    public event VertexAction<TVertex>? VertexRemoved;

    public bool AddEdge(TEdge edge) {
        var fromIndex = vertexToIndex(edge.Source);
        var toIndex = vertexToIndex(edge.Target);
        if (!ContainsIndex(fromIndex) || !ContainsIndex(toIndex)) throw new ArgumentOutOfRangeException(nameof(edge));

        if (matrix[fromIndex, toIndex] != 0) return false;
        matrix[fromIndex, toIndex] = 1;
        EdgeAdded?.Invoke(edge);
        return true;
    }

    public int AddEdgeRange(IEnumerable<TEdge> edges) {
        return edges.Count(AddEdge);
    }

    public bool RemoveEdge(TEdge edge) {
        var fromIndex = vertexToIndex(edge.Source);
        var toIndex = vertexToIndex(edge.Target);
        if (!ContainsIndex(fromIndex) || !ContainsIndex(toIndex)) return false;

        if (matrix[fromIndex, toIndex] == 0) return false;
        matrix[fromIndex, toIndex] = 0;
        EdgeRemoved?.Invoke(edge);
        return true;
    }

    public int RemoveEdgeIf(EdgePredicate<TVertex, TEdge> predicate) {
        var edges = Edges.Where(e => predicate(e)).ToList();
        var cnt = edges.Count;
        foreach (var edge in edges) RemoveEdge(edge);

        return cnt;
    }

    public event EdgeAction<TVertex, TEdge>? EdgeAdded;
    public event EdgeAction<TVertex, TEdge>? EdgeRemoved;

    public bool AddVerticesAndEdge(TEdge edge) {
        return AddEdge(edge);
    }

    public int AddVerticesAndEdgeRange(IEnumerable<TEdge> edges) {
        return AddEdgeRange(edges);
    }

    public int RemoveInEdgeIf(TVertex vertex, EdgePredicate<TVertex, TEdge> predicate) {
        var edges = InEdges(vertex).Where(e => predicate(e)).ToList();
        var cnt = edges.Count;
        foreach (var edge in edges) RemoveEdge(edge);

        return cnt;
    }

    public void ClearInEdges(TVertex vertex) {
        var idx = vertexToIndex(vertex);
        matrix.SetColumn(idx, SparseVector.Create(matrix.ColumnCount, 0));
    }

    public void ClearEdges(TVertex vertex) {
        ClearInEdges(vertex);
        ClearOutEdges(vertex);
    }

    public static BidirectionalMatrixGraph<Vertex, Edge<Vertex>> ForVertices(int rows,
        int columns,
        bool columnMajor = true,
        bool sparse = true) {
        var indexToVertex = columnMajor switch {
            true => i => Vertex.FromColumnMajorIndex(i, rows),
            false => (Func<int, Vertex>)(i => Vertex.FromRowMajorIndex(i, columns))
        };

        var vertexToIndex = columnMajor switch {
            true => v => v.ColumnMajorIndex(rows),
            false => (Func<Vertex, int>)(v => v.RowMajorIndex(columns))
        };

        return new BidirectionalMatrixGraph<Vertex, Edge<Vertex>>(rows * columns, vertexToIndex, indexToVertex,
            (s, t) => new Edge<Vertex>(s, t), sparse);
    }

    public static BidirectionalMatrixGraph<TVertex, TEdge> FromGraph(IBidirectionalGraph<TVertex, TEdge> g,
        Func<TVertex, int> vertexToIndex,
        Func<int, TVertex> indexToVertex,
        Func<TVertex, TVertex, TEdge> edgeFactory,
        bool sparse = true) {
        var res = new BidirectionalMatrixGraph<TVertex, TEdge>(g.VertexCount, vertexToIndex, indexToVertex, edgeFactory,
            sparse);
        res.AddEdgeRange(g.Edges);
        return res;
    }

    private bool ContainsIndex(int index) {
        return index >= 0 && index < matrix.RowCount;
    }

    public BidirectionalMatrixGraph<TVertex, TEdge> ComputeTransitiveClosure() {
        Matrix<float> oldM = DenseMatrix.OfMatrix(matrix);
        using var acc = new MathAccelerator(MathMode.Fast);
        Matrix<float>? m = null;
        do {
            if (m != null) (oldM, m) = (m, oldM);
            acc.MultiplyMatrixRowMajorTiling(oldM, oldM, ref m);
            m.ToBooleanInplace();
        } while (!oldM.Equals(m));

        return new BidirectionalMatrixGraph<TVertex, TEdge>(m, vertexToIndex, indexToVertex, edgeFactory);
    }

    public bool SameNeighborhood(TVertex v1, TVertex v2) {
        var idx1 = vertexToIndex(v1);
        var idx2 = vertexToIndex(v2);

        var row1 = matrix.Row(idx1);
        var row2 = matrix.Row(idx2);
        if ((row1 - row2).Sum() != 0) return false;

        var col1 = matrix.Column(idx1);
        var col2 = matrix.Column(idx2);
        return (col1 - col2).Sum() == 0;
    }

    public bool AreMutualNeighbors(TVertex a, TVertex b) {
        return ContainsEdge(a, b) && ContainsEdge(b, a);
    }

    public bool IsAncestor(TVertex a, TVertex b) {
        return ContainsEdge(a, b) && !ContainsEdge(b, a);
    }
}