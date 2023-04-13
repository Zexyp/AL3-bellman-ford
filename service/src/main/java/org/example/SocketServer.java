package org.example;

import org.json.JSONArray;
import org.json.JSONObject;

import java.io.BufferedReader;
import java.io.IOException;
import java.io.InputStreamReader;
import java.io.PrintWriter;
import java.net.ServerSocket;
import java.net.Socket;
import java.util.ArrayList;
import java.util.HashMap;
import java.util.List;

public class SocketServer {
    private ServerSocket serverSocket;
    private Socket clientSocket;
    private PrintWriter out;
    private BufferedReader in;

    public void start(int port) {
        try {
            while (true) {
                serverSocket = new ServerSocket(port);
                System.out.println("Server started");
                clientSocket = serverSocket.accept();
                out = new PrintWriter(clientSocket.getOutputStream(), true);
                in = new BufferedReader(new InputStreamReader(clientSocket.getInputStream()));
                String inputJson = in.readLine();
//                String inputJson = "{\"start\": 0, \"vertices\":[{\"id\":1,\"edges\":[{\"to\":2,\"weight\":6},{\"to\":3,\"weight\":7}]},{\"id\":2,\"edges\":[]},{\"id\":3,\"edges\":[{\"to\":2,\"weight\":-11}]}]}";

                System.out.println("INPUT DETECTED");
                System.out.println(inputJson);

                try {
                    JSONObject json = new JSONObject(inputJson);
                    JSONArray vertices = json.getJSONArray("vertices");

                    HashMap<Integer, Vertex> graph = new HashMap<>();

                    for (int i = 0; i < vertices.length(); i++) {
                        JSONObject vertex = vertices.getJSONObject(i);
                        JSONArray edges = vertex.getJSONArray("edges");

                        ArrayList<Edge> edgesList = new ArrayList<>();
                        for (int j = 0; j < edges.length(); j++) {
                            JSONObject edge = edges.getJSONObject(j);
                            edgesList.add(new Edge(edge.getInt("weight"), edge.getInt("to")));
                        }

                        Vertex v = new Vertex(vertex.getInt("id"), edgesList);
                        graph.put(v.getId(), v);
                    }


                    // Process the graph with Bellman Ford algorithm
                    BellmanFord bf = new BellmanFord(graph, json.getInt("start"));
                    List<Vertex> results = bf.getResults();

                    JSONObject resultsJson = new JSONObject();
                    JSONArray resultsArray = new JSONArray();
                    for(Vertex v: results) {
                        JSONObject result = new JSONObject();
                        result.put("id", v.getId());
                        result.put("distance", v.getDistanceFromStart());
                        result.put("isInNegativeLoop", v.isInNegativeLoop());
                        resultsArray.put(result);
                    }
                    resultsJson.put("vertices", resultsArray);

                    out.println(resultsJson.toString());
                    System.out.println("RESULTS: ");
                    System.out.println(resultsJson.toString());

                } catch (Exception e) {
                    System.out.println(e.getMessage());
                    out.println("Invalid JSON (or some other error lol)");
                }
                stop();
            }
        } catch (Exception e) {
            System.out.println("Somethinks ded");
            System.out.println(e.getMessage());
        }

    }

    public void stop() throws IOException {
        in.close();
        out.close();
        clientSocket.close();
        serverSocket.close();
    }
}
