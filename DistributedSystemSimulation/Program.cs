using DistributedSystemSimulation.Models;

Console.WriteLine("Starting Distributed System Simulation");

// Initialize the nodes
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

// Elect an initial leader
node1.StartElection();

// After leader is elected, propose new state
node1.ProposeState(10);

// Propose a new state from node2, this will be forwarded to the leader
node2.ProposeState(15);

// Simulate node2 going offline
node2.SimulateOffline();

// Leader proposes another state while node2 is offline, it should only be received by node3
node1.ProposeState(20);

// Simulate node2 coming back online
node2.SimulateOnline();

// Propose a new state from node1, this will be received by node2 and node3
node1.ProposeState(30);

// Set the node1 offline, this will trigger a new election of a new leader
node1.SimulateOffline();

// Propose a new state from node3, this will be received by node2 which is now the leader, only node3 will receive the proposal
node3.ProposeState(40);

// Simulate node1 coming back online
node1.SimulateOnline();

// Propose a new state from node1, as the leader is now node2, it will be forwarded to it, and then node3 and node1 will receive the proposal
node1.ProposeState(50);

// Display logs of each node
node1.DisplayLog();
node2.DisplayLog();
node3.DisplayLog();

Console.WriteLine("Simulation Completed");
