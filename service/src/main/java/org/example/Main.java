package org.example;

public class Main {
    public static void main(String[] args) {
        SocketServer server = new SocketServer();
        System.out.println("Starting server");
        server.start(6969);
        System.out.println("Server is ded as hell");
    }
}