#region license

// AoC2023 - AoC2023 - RowColumnMajorAdjacencyMatrix.cs
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
using MathNet.Numerics;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Single;

namespace AoC.Support;

using Vertex = Vertex<int>;

// Adjacency matrix for a grid graph with weights of 1 for each edge
public class UniformDistanceMatrixGraph : ICloneable {
    public delegate int VertexIndexMapper(Vertex vertex);

    private readonly VertexIndexMapper indexMapper;
    private readonly bool sparseAdjacencies;
    private readonly Dictionary<Vertex, int> vertexIndices;
    private readonly Vertex[] vertices;
    private Matrix<float> adjacencies;
    private Matrix<float>? pairwiseDistances;

    static UniformDistanceMatrixGraph() {
        if (Control.TryUseNativeCUDA()) {
            Debug.WriteLine("Using CUDA");
            return;
        }

        if (Control.TryUseNativeMKL()) {
            Debug.WriteLine("Using MKL");
            return;
        }

        if (Control.TryUseNativeOpenBLAS()) {
            Debug.WriteLine("Using OpenBLAS");
            return;
        }

        Debug.WriteLine("Using managed");
    }


    public UniformDistanceMatrixGraph(int rows, int columns, IEnumerable<Vertex> verts, bool sparseAdjacencies = true,
        VertexIndexMapper? mapper = null) {
        Rows = rows;
        Columns = columns;

        this.sparseAdjacencies = sparseAdjacencies;
        indexMapper ??= v => v.ColumnMajorIndex(rows);
        vertices = verts.ToArray();
        vertexIndices = new Dictionary<Vertex, int>(vertices.Length);
        for (var i = 0; i < vertices.Length; i++) {
            var vert = vertices[i];
            if (!vert.ExistsInGrid(Columns, Rows))
                throw new ArgumentOutOfRangeException(nameof(verts), vert, "Vertex is not in grid");

            vertexIndices[vert] = i;
        }

        MakeMatrix(vertices.Length);
    }

    public UniformDistanceMatrixGraph(UniformDistanceMatrixGraph other) {
        Rows = other.Rows;
        Columns = other.Columns;
        adjacencies = other.adjacencies.Clone();
        pairwiseDistances = other.pairwiseDistances?.Clone();
        indexMapper = other.indexMapper;
        sparseAdjacencies = other.sparseAdjacencies;
        vertices = other.vertices.ToArray();
        vertexIndices = new Dictionary<Vertex, int>(other.vertexIndices);
    }

    public int Count => vertices.Length;
    public int Columns { get; }
    public int Rows { get; }

    public object Clone() {
        return new UniformDistanceMatrixGraph(this);
    }

    private int GetVertexIndex(Vertex vertex) {
        return vertexIndices[vertex];
    }

    public bool ContainsVertex(Vertex vertex) {
        return vertexIndices.ContainsKey(vertex);
    }

    [MemberNotNull(nameof(adjacencies))]
    private void MakeMatrix(int count) {
        if (sparseAdjacencies)
            adjacencies = new SparseMatrix(count, count);
        else
            adjacencies = DenseMatrix.Create(count, count, 0f);
    }

    private void AddEdge(int from, int to) {
        if (from == to) throw new ArgumentException("Cannot add edge from a vertex to itself");

        if (HasEdge(from, to)) return;

        adjacencies[from, to] = 1;
        adjacencies[to, from] = 1;
        pairwiseDistances = null;
    }

    private Vertex VertexForIndex(int index) {
        return vertices[index];
    }

    public void AddEdge(Vertex from, Vertex to) {
        if (from == to) throw new ArgumentException("Cannot add edge from a vertex to itself");

        if (!from.ExistsInGrid(Columns, Rows))
            throw new ArgumentOutOfRangeException(nameof(from), from, "Vertex is not in grid");

        if (!to.ExistsInGrid(Columns, Rows))
            throw new ArgumentOutOfRangeException(nameof(to), to, "Vertex is not in grid");

        var toIdx = GetVertexIndex(to);
        var fromIdx = GetVertexIndex(from);
        AddEdge(fromIdx, toIdx);
    }

    private void RemoveEdge(int from, int to) {
        if (!HasEdge(from, to)) return;

        if (from == to) return;

        adjacencies[from, to] = 0;
        adjacencies[to, from] = 0;
        pairwiseDistances = null;
    }

    public void RemoveEdge(Vertex from, Vertex to) {
        if (!ContainsVertex(from) || !ContainsVertex(to)) return;

        var fromIdx = GetVertexIndex(from);
        var toIdx = GetVertexIndex(to);
        RemoveEdge(fromIdx, toIdx);
    }

    public bool HasEdge(int from, int to) {
        return adjacencies[from, to] != 0;
    }

    public bool HasEdge(Vertex from, Vertex to) {
        var fromIdx = GetVertexIndex(from);
        var toIdx = GetVertexIndex(to);
        return HasEdge(fromIdx, toIdx);
    }

    private int GetDegree(int vertex) {
        return int.CreateChecked(adjacencies.Row(vertex).Sum());
    }

    public int GetDegree(Vertex vertex) {
        var idx = GetVertexIndex(vertex);
        return GetDegree(idx);
    }

    private IEnumerable<int> GetNeighbors(int vertex) {
        return adjacencies.Row(vertex)
            .EnumerateIndexed(Zeros.AllowSkip)
            .Select(t => t.Item2 != 0 ? t.Item1 : -1)
            .Where(i => i != -1);
    }

    public IEnumerable<Vertex> GetNeighbors(Vertex vertex) {
        return GetNeighbors(GetVertexIndex(vertex)).Select(VertexForIndex);
    }

    private static bool SeidelDone(Matrix<float> mg) {
        return mg.EnumerateIndexed()
            .Where(t => t.Item1 != t.Item2)
            .All(t => t.Item3 == 1);
    }

    private static Matrix<float> CreateDg(Matrix<float> mg) {
        return mg.MapIndexed(
            (i, j, v) => {
                if (i == j) return 0f;

                if (v == 1) return 1f;

                return 2;
            },
            Zeros.Include
        );
    }

    private static Matrix<float> CreateMg2(Matrix<float> mg, Matrix<float> mgs) {
        var mg2 = mg.MapIndexed(
            (i, j, mgij) => {
                var mgsij = mgs[i, j];
                if (i != j && (mgij == 1 || mgsij > 0)) return 1f;

                return 0f;
            }, Zeros.Include);
        return mg2;
    }

    // Based on https://www.wisdom.weizmann.ac.il/~/oded/MC/apd.pdf
    private static Matrix<float> SeidelDistances(Matrix<float> a) {
        Matrix<float> b;
        {
            var z = a * a;
            b = z.MapIndexed(
                (i, j, zij) => {
                    if (i == j) return 0f;

                    if (zij > 0) return 1f;

                    return a[i, j];
                }
            );
        }

        if (SeidelDone(b)) return 2 * b - a;

        var t = SeidelDistances(b);
        var x = t * a;
        var neighborCounts = a.RowSums();
        var d = t.MapIndexed((i, j, v) => {
            if (i == j) return 0f;

            if (x[i, j] >= neighborCounts[j] * v) return 2 * v;

            return 2 * v - 1;
        });

        return d;
    }

    private bool IsConnected() {
        return ConnectedTo(0).Count == Count;
    }

    private HashSet<int> ConnectedTo(int vertex) {
        var visited = new HashSet<int>();
        var toVisit = new Queue<int>();
        toVisit.Enqueue(vertex);
        while (toVisit.Count > 0) {
            var next = toVisit.Dequeue();
            if (!visited.Add(next)) continue;

            foreach (var neighbor in GetNeighbors(next)) toVisit.Enqueue(neighbor);
        }

        return visited;
    }

    public IEnumerable<Vertex> ConnectedTo(Vertex vertex) {
        var idx = GetVertexIndex(vertex);
        var connected = ConnectedTo(idx);
        return connected.Select(VertexForIndex);
    }

    // Returns the strongly connected component starting at the given vertex
    public UniformDistanceMatrixGraph SccStartingAt(Vertex vertex) {
        var connected = ConnectedTo(vertex).ToHashSet();
        var g = new UniformDistanceMatrixGraph(Rows, Columns, connected, sparseAdjacencies, indexMapper);
        foreach (var v in connected) {
            foreach (var n in GetNeighbors(v))
                if (connected.Contains(n))
                    g.AddEdge(v, n);
        }

        return g;
    }

    [MemberNotNull(nameof(pairwiseDistances))]
    public void UpdatePairwiseDistances() {
        var connected = ConnectedTo(0);
        if (connected.Count != Count) {
            var unconnected = Enumerable.Range(0, Count).Except(connected);
            var examples = string.Join(", ", unconnected.Take(5).Select(VertexForIndex).Select(v => v.ToString()));
            throw new InvalidOperationException(
                "Cannot compute pairwise distances on a disconnected graph, examples: {" + examples + "}");
        }

        var inp = DenseMatrix.OfMatrix(adjacencies);
        var dg = SeidelDistances(inp);
        pairwiseDistances = dg;
    }

    public int GetDistance(int from, int to) {
        if (pairwiseDistances == null) UpdatePairwiseDistances();

        return int.CreateChecked(pairwiseDistances[from, to]);
    }

    public int GetDistance(Vertex from, Vertex to) {
        return GetDistance(GetVertexIndex(from), GetVertexIndex(to));
    }
}