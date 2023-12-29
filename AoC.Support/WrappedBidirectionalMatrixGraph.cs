#region license
// AoC2023 - AoC.Support - WrappedBidirectionalMatrixGraph.cs
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
using QuikGraph;

namespace AoC.Support;

using Vertex = Vertex<int>;

using BiGraph = QuikGraph.BidirectionalMatrixGraph<Edge<int>>;

public class WrappedBidirectionalMatrixGraph<TVertex> : IBidirectionalGraph<TVertex, Edge<TVertex>>, IMutableEdgeListGraph<TVertex, Edge<TVertex>> where TVertex: notnull {
    private readonly BiGraph graph;
    private readonly Func<TVertex, int> vertexToIndex;
    private readonly Func<int, TVertex> indexToVertex;
    
    public WrappedBidirectionalMatrixGraph(int vertexCount, Func<TVertex, int> vertexToIndex, Func<int, TVertex> indexToVertex) {
        graph = new BiGraph(vertexCount);
        this.vertexToIndex = vertexToIndex;
        this.indexToVertex = indexToVertex;
        graph.EdgeAdded += edge => EdgeAdded?.Invoke(new Edge<TVertex>(this.indexToVertex(edge.Source), this.indexToVertex(edge.Target)));
        graph.EdgeRemoved += edge => EdgeRemoved?.Invoke(new Edge<TVertex>(this.indexToVertex(edge.Source), this.indexToVertex(edge.Target)));
    }

    public static WrappedBidirectionalMatrixGraph<Vertex> ForVertices(int rows, int columns, bool columnMajor = true) {
        var indexToVertex = columnMajor switch {
            true => (Func<int, Vertex>) (i => Vertex.FromColumnMajorIndex(i, rows)),
            false => (Func<int, Vertex>) (i => Vertex.FromRowMajorIndex(i, columns)),
        };
        
        var vertexToIndex = columnMajor switch {
            true => (Func<Vertex, int>) (v => v.ColumnMajorIndex(rows)),
            false => (Func<Vertex, int>) (v => v.RowMajorIndex(columns)),
        };
        
        return new WrappedBidirectionalMatrixGraph<Vertex>(rows * columns, vertexToIndex, indexToVertex);
    }

    public bool IsDirected => graph.IsDirected;
    public bool AllowParallelEdges => graph.AllowParallelEdges;
    public bool ContainsVertex(TVertex vertex) => graph.ContainsVertex(vertexToIndex(vertex));

    public bool IsOutEdgesEmpty(TVertex vertex) => graph.IsOutEdgesEmpty(vertexToIndex(vertex));

    public int OutDegree(TVertex vertex) => graph.OutDegree(vertexToIndex(vertex));

    public IEnumerable<Edge<TVertex>> OutEdges(TVertex vertex) {
        var index = vertexToIndex(vertex);
        return graph.OutEdges(index).Select(e => new Edge<TVertex>(indexToVertex(e.Source), indexToVertex(e.Target)));
    }

    public bool TryGetOutEdges(TVertex vertex, [UnscopedRef] out IEnumerable<Edge<TVertex>>? edges) {
        var index = vertexToIndex(vertex);
        if (graph.TryGetOutEdges(index, out var rawEdges)) {
            edges = rawEdges.Select(e => new Edge<TVertex>(indexToVertex(e.Source), indexToVertex(e.Target)));
            return true;
        }

        edges = null;
        return false;
    }

    public Edge<TVertex> OutEdge(TVertex vertex, int index) {
        var rawEdge = graph.OutEdge(vertexToIndex(vertex), index);
        return new Edge<TVertex>(indexToVertex(rawEdge.Source), indexToVertex(rawEdge.Target));
    }

    public bool ContainsEdge(TVertex source, TVertex target) {
        return graph.ContainsEdge(vertexToIndex(source), vertexToIndex(target));
    }

    public bool TryGetEdge(TVertex source, TVertex target, [UnscopedRef] out Edge<TVertex> edge) {
        if (graph.TryGetEdge(vertexToIndex(source), vertexToIndex(target), out var rawEdge)) {
            edge = new Edge<TVertex>(indexToVertex(rawEdge.Source), indexToVertex(rawEdge.Target));
            return true;
        }

        edge = default;
        return false;
    }

    public bool TryGetEdges(TVertex source, TVertex target, [UnscopedRef] out IEnumerable<Edge<TVertex>>? edges) {
        if (graph.TryGetEdges(vertexToIndex(source), vertexToIndex(target), out var rawEdges)) {
            edges = rawEdges.Select(e => new Edge<TVertex>(indexToVertex(e.Source), indexToVertex(e.Target)));
            return true;
        }

        edges = null;
        return false;
    }

    public bool IsVerticesEmpty => graph.IsVerticesEmpty;
    public int VertexCount => graph.VertexCount;
    public IEnumerable<TVertex> Vertices => graph.Vertices.Select(indexToVertex);
    public bool ContainsEdge(Edge<TVertex> edge) {
        return graph.ContainsEdge(new Edge<int>(vertexToIndex(edge.Source), vertexToIndex(edge.Target)));
    }

    public bool IsEdgesEmpty => graph.IsEdgesEmpty;
    public int EdgeCount => graph.EdgeCount;
    public IEnumerable<Edge<TVertex>> Edges => graph.Edges.Select(e => new Edge<TVertex>(indexToVertex(e.Source), indexToVertex(e.Target)));
    public bool IsInEdgesEmpty(TVertex vertex) => graph.IsInEdgesEmpty(vertexToIndex(vertex));

    public int InDegree(TVertex vertex) => graph.InDegree(vertexToIndex(vertex));

    public IEnumerable<Edge<TVertex>> InEdges(TVertex vertex) => graph.InEdges(vertexToIndex(vertex)).Select(e => new Edge<TVertex>(indexToVertex(e.Source), indexToVertex(e.Target)));

    public bool TryGetInEdges(TVertex vertex, [UnscopedRef] out IEnumerable<Edge<TVertex>> edges) {
        if (graph.TryGetInEdges(vertexToIndex(vertex), out var rawEdges)) {
            edges = rawEdges.Select(e => new Edge<TVertex>(indexToVertex(e.Source), indexToVertex(e.Target)));
            return true;
        }

        edges = null;
        return false;
    }

    public Edge<TVertex> InEdge(TVertex vertex, int index) {
        var rawEdge = graph.InEdge(vertexToIndex(vertex), index);
        return new Edge<TVertex>(indexToVertex(rawEdge.Source), indexToVertex(rawEdge.Target));
    }

    public int Degree(TVertex vertex) {
        return graph.Degree(vertexToIndex(vertex));
    }

    public void Clear() {
        graph.Clear();
    }

    public bool AddEdge(Edge<TVertex> edge) {
        return graph.AddEdge(new Edge<int>(vertexToIndex(edge.Source), vertexToIndex(edge.Target)));
    }

    public int AddEdgeRange(IEnumerable<Edge<TVertex>> edges) {
        return graph.AddEdgeRange(edges.Select(e => new Edge<int>(vertexToIndex(e.Source), vertexToIndex(e.Target))));
    }

    public bool RemoveEdge(Edge<TVertex> edge) {
        return graph.RemoveEdge(new Edge<int>(vertexToIndex(edge.Source), vertexToIndex(edge.Target)));
    }

    public int RemoveEdgeIf(EdgePredicate<TVertex, Edge<TVertex>> predicate) {
        return graph.RemoveEdgeIf(e => predicate(new Edge<TVertex>(indexToVertex(e.Source), indexToVertex(e.Target))));
    }

    public event EdgeAction<TVertex, Edge<TVertex>>? EdgeAdded;
    public event EdgeAction<TVertex, Edge<TVertex>>? EdgeRemoved;
}
