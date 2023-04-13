package org.example;

import java.util.HashMap;
import java.util.List;

public class BellmanFord {
    private final HashMap<Integer, Vertex> graph; // The graph is represented as Vertex objects represented by their ID
    private final int startVertex; // The starting point for distance calculations

    public BellmanFord(HashMap<Integer, Vertex> graph, int startVertex) {
        this.graph = graph;
        this.startVertex = startVertex;
        this.run(false);
//        this.run(true);
    }

    public void run(boolean detectNegativeLoops) {
        this.graph.get(startVertex).setDistanceFromStart(0); // Set the distance of the start vertex to 0

        for(int i = 0; i < this.graph.keySet().size()-1; i++) { // Iterate through all vertices n-1 times
            for (Vertex v : this.graph.values()) {
                for (Edge e : v.getEdges()) {
                    Vertex target = this.graph.get(e.getTargetVertex());
                    if (target.getDistanceFromStart() > v.getDistanceFromStart() + e.getDistanceDifference()) { // If the new path is better than the old one
                        if(detectNegativeLoops) {   // In the second iteration we can detect negative loop by checking if the distance of the target vertex is still decreasing
                            target.setInNegativeLoop();
                        }
                        target.setDistanceFromStart(v.getDistanceFromStart() + e.getDistanceDifference());
                    }
                }
            }
        }
    }

    public List<Vertex> getResults() {
        return List.copyOf(graph.values());
    }
}
