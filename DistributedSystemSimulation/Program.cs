using DistributedSystemSimulation.Models;

Console.WriteLine("Starting Distributed System Simulation");

var node1 = new Node(1);
var node2 = new Node(2);
var node3 = new Node(3);

// Neighbors setup
node1.AddNeighbor(node2);
node1.AddNeighbor(node3);
node2.AddNeighbor(node1);
node2.AddNeighbor(node3);
node3.AddNeighbor(node1);
node3.AddNeighbor(node2);

// Elect a leader
node1.StartElection();

// After leader is elected, propose new state
node1.ProposeState(10); // if node1 is leader, succeeds

// Propose a new state from node2
node2.ProposeState(15);

// Simulate offline scenario
node2.SimulateOffline();

// Leader proposes another state
node1.ProposeState(20);

// Simulate node2 coming back online
node2.SimulateOnline();

// Propose a new state
node1.ProposeState(30);

// Set the node1 offline
node1.SimulateOffline();

// Propose a new state from node3
node3.ProposeState(40);

// Simulate node1 coming back online
node1.SimulateOnline();

// Propose a new state
node1.ProposeState(50);

// Display logs
node1.DisplayLog();
node2.DisplayLog();
node3.DisplayLog();

Console.WriteLine("Simulation Completed");
