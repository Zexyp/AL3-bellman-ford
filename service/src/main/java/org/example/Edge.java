package org.example;

public class Edge {
    private final int distanceDifference;
    private final int targetVertex;

    public Edge(int distanceDifference, int targetVertex) {
        this.distanceDifference = distanceDifference;
        this.targetVertex = targetVertex;
    }

    public int getDistanceDifference() {
        return distanceDifference;
    }

    public int getTargetVertex() {
        return targetVertex;
    }
}
