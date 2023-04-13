package org.example;

import java.util.ArrayList;

public class Vertex {
    private final int id;
    private int distanceFromStart = Integer.MAX_VALUE;
    private final ArrayList<Edge> edges;

    boolean isInNegativeLoop = false;

    public Vertex(int id, ArrayList<Edge> edges) {
        this.id = id;
        this.edges = edges;
    }

    public int getId() {
        return id;
    }

    public int getDistanceFromStart() {
        return distanceFromStart;
    }

    public void setDistanceFromStart(int distanceFromStart) {
        this.distanceFromStart = distanceFromStart;
    }

    public ArrayList<Edge> getEdges() {
        return edges;
    }

    public void addEdge(Edge edge) {
        this.edges.add(edge);
    }

    public boolean isInNegativeLoop() {
        return isInNegativeLoop;
    }

    public void setInNegativeLoop() {
        isInNegativeLoop = true;
    }
}
