using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.IO;

namespace VeryFunnyGraphs
{
    public class GraphContainer<T>
    {
        public class Edge
        {
            public T A;
            public T B;
            public int Weight;

            public Edge(T a, T b, int weight = 0)
            {
                A = a;
                B = b;
                Weight = weight;
            }

            public bool Is(T a, T b)
            {
                return (A.Equals(a) && B.Equals(b)) || (A.Equals(b) && B.Equals(a));
            }
        }

        public readonly ReadOnlyCollection<T> Vertices;
        public readonly ReadOnlyCollection<Edge> Edges;
        public T? Start
        {
            get => _start;
            set
            {
                if (value == null)
                {
                    _start = default;
                    return;
                }

                if (!_vertices.Contains(value))
                    throw new InvalidOperationException();
                _start = value;
            }
        }

        private readonly List<T> _vertices = new List<T>();
        private readonly List<Edge> _edges = new List<Edge>();
        private T? _start;

        public GraphContainer()
        {
            Vertices = _vertices.AsReadOnly();
            Edges = _edges.AsReadOnly();
        }

        public void AddVertex(T vert)
        {
            if (_vertices.Contains(vert))
                throw new InvalidOperationException();

            _vertices.Add(vert);
        }

        public void RemoveVertex(T vert)
        {
            DisconnectFrom(vert);
            _vertices.Remove(vert);
            if (_start != null && _start.Equals(vert))
                _start = default;
        }

        public void DisconnectFrom(T vert)
        {
            _edges.RemoveAll(e => e.A.Equals(vert) || e.B.Equals(vert));
        }

        public void Connect(T vertA, T vertB, int weight = 0)
        {
            if (vertA.Equals(vertB) || ContainsEdge(vertA, vertB) || !_vertices.Contains(vertA) || !_vertices.Contains(vertB))
                throw new InvalidOperationException();

            _edges.Add(new (vertA, vertB));
        }

        public void Disconnect(T vertA, T vertB)
        {
            _edges.RemoveAll(e => e.Is(vertA, vertB));
        }

        public bool ContainsEdge(T vertA, T vertB)
        {
            return _edges.Any(e => e.Is(vertA, vertB));
        }

        public string Dump()
        {
            using StringWriter writer = new StringWriter();

            // writer vertices
            List<Edge> unwrittenEdges = new List<Edge>(_edges);
            writer.Write($"{{\"start\":{_vertices.IndexOf(_start)},\"vertices\":[");
            for (int i = 0; i < _vertices.Count; i++)
            {
                writer.Write($"{{\"id\":{i},\"edges\":[{String.Join(",", unwrittenEdges.Where(e => e.A.Equals(_vertices[i])).Select(e => $"{{\"to\":{_vertices.IndexOf(e.B)},\"weight\":{e.Weight}}}"))}]}}");
                unwrittenEdges.RemoveAll(e => e.A.Equals(_vertices[i]));
                if (i != _vertices.Count - 1)
                    writer.Write(",");
            }
            writer.Write("]}");

            writer.Flush();

            return writer.ToString();
        }
    }
}
