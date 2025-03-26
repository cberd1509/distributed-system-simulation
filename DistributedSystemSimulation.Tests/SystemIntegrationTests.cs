using DistributedSystemSimulation.Models;

namespace DistributedSystemSimulation.Tests;

public class SystemIntegrationTests
{
    [Fact]
    public void CompleteSystemFlow_ShouldWorkAsExpected()
    {
        // Initialize nodes
        var node1 = new Node(1);
        var node2 = new Node(2);
        var node3 = new Node(3);

        // Setup network
        node1.AddNeighbor(node2);
        node1.AddNeighbor(node3);
        node2.AddNeighbor(node1);
        node2.AddNeighbor(node3);
        node3.AddNeighbor(node1);
        node3.AddNeighbor(node2);

        // Test initial election
        node1.StartElection();
        Assert.Equal(NodeRole.Leader, node1.CurrentRole);
        Assert.Equal(NodeRole.Follower, node2.CurrentRole);
        Assert.Equal(NodeRole.Follower, node3.CurrentRole);

        // Test state propagation
        node1.ProposeState(10);
        Assert.Equal(10, node1.CurrentState);
        Assert.Equal(10, node2.CurrentState);
        Assert.Equal(10, node3.CurrentState);

        // Test leader failure and recovery
        node1.SimulateOffline();
        Assert.True(node1.IsOffline);

        // One of the remaining nodes should become leader
        Assert.True(node2.CurrentRole == NodeRole.Leader || node3.CurrentRole == NodeRole.Leader);

        // Test state updates with new leader
        var newLeader = node2.CurrentRole == NodeRole.Leader ? node2 : node3;
        newLeader.ProposeState(20);
        Assert.Equal(20, node2.CurrentState);
        Assert.Equal(20, node3.CurrentState);

        // Test rejoining node synchronization
        node1.SimulateOnline();
        Assert.Equal(20, node1.CurrentState);
    }
}
