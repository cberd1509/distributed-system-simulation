using DistributedSystemSimulation.Models;

namespace DistributedSystemSimulation.Tests;

public class NodeTests
{
    [Fact]
    public void Node_ShouldStartAsFollower()
    {
        var node = new Node(1);
        Assert.Equal(NodeRole.Follower, node.CurrentRole);
    }

    [Fact]
    public void Node_ShouldAcceptNeighbor()
    {
        var node1 = new Node(1);
        var node2 = new Node(2);

        node1.AddNeighbor(node2);

        Assert.Contains(node2, node1.Neighbors);
    }

    [Fact]
    public void Node_ShouldElectLeader()
    {
        var node1 = new Node(1);
        var node2 = new Node(2);
        var node3 = new Node(3);

        node1.AddNeighbor(node2);
        node1.AddNeighbor(node3);
        node2.AddNeighbor(node1);
        node2.AddNeighbor(node3);
        node3.AddNeighbor(node1);
        node3.AddNeighbor(node2);

        node1.StartElection();

        Assert.Equal(NodeRole.Leader, node1.CurrentRole);
        Assert.Equal(NodeRole.Follower, node2.CurrentRole);
        Assert.Equal(NodeRole.Follower, node3.CurrentRole);
    }

    [Fact]
    public void Node_ShouldHandleProposalAsLeader()
    {
        var leader = new Node(1);
        var follower1 = new Node(2);
        var follower2 = new Node(3);

        leader.AddNeighbor(follower1);
        leader.AddNeighbor(follower2);
        follower1.AddNeighbor(leader);
        follower2.AddNeighbor(leader);

        leader.StartElection();
        leader.ProposeState(10);

        Assert.Equal(10, leader.CurrentState);
        Assert.Equal(10, follower1.CurrentState);
        Assert.Equal(10, follower2.CurrentState);
    }

    [Fact]
    public void Node_ShouldForwardProposalToLeader()
    {
        var leader = new Node(1);
        var follower1 = new Node(2);
        var follower2 = new Node(3);

        leader.AddNeighbor(follower1);
        leader.AddNeighbor(follower2);
        follower1.AddNeighbor(leader);
        follower2.AddNeighbor(leader);

        leader.StartElection();
        follower1.ProposeState(15);

        Assert.Equal(15, leader.CurrentState);
        Assert.Equal(15, follower1.CurrentState);
        Assert.Equal(15, follower2.CurrentState);
    }

    [Fact]
    public void Node_ShouldHandleOfflineScenario()
    {
        var leader = new Node(1);
        var follower1 = new Node(2);
        var follower2 = new Node(3);

        leader.AddNeighbor(follower1);
        leader.AddNeighbor(follower2);
        follower1.AddNeighbor(leader);
        follower2.AddNeighbor(leader);

        leader.StartElection();
        leader.ProposeState(10);

        // Simulate leader going offline
        leader.SimulateOffline();

        Assert.True(leader.IsOffline);
        Assert.Equal(NodeRole.Follower, leader.CurrentRole);
        Assert.Null(follower1.CurrentLeaderId);
        Assert.Null(follower2.CurrentLeaderId);
    }

    [Fact]
    public void Node_ShouldSynchronizeStateWhenComingOnline()
    {
        var leader = new Node(1);
        var follower1 = new Node(2);
        var follower2 = new Node(3);

        leader.AddNeighbor(follower1);
        leader.AddNeighbor(follower2);
        follower1.AddNeighbor(leader);
        follower2.AddNeighbor(leader);

        leader.StartElection();
        leader.ProposeState(20);

        follower1.SimulateOffline();
        leader.ProposeState(30);
        follower1.SimulateOnline();

        Assert.Equal(30, follower1.CurrentState);
        Assert.Equal(leader.Id, follower1.CurrentLeaderId);
    }

    [Fact]
    public void Node_ShouldMaintainTermConsistency()
    {
        var node1 = new Node(1);
        var node2 = new Node(2);

        node1.AddNeighbor(node2);
        node2.AddNeighbor(node1);

        node1.StartElection();
        int initialTerm = node1.CurrentTerm;

        Assert.Equal(initialTerm, node2.CurrentTerm);
        Assert.Equal(NodeRole.Leader, node1.CurrentRole);
        Assert.Equal(node1.Id, node2.CurrentLeaderId);
    }

    [Fact]
    public void OfflineNode_ShouldNotParticipateInElection()
    {
        var node1 = new Node(1);
        var node2 = new Node(2);
        var node3 = new Node(3);

        node1.AddNeighbor(node2);
        node1.AddNeighbor(node3);
        node2.AddNeighbor(node1);
        node2.AddNeighbor(node3);
        node3.AddNeighbor(node1);
        node3.AddNeighbor(node2);

        node1.SimulateOffline();
        node2.StartElection();

        Assert.Equal(NodeRole.Follower, node1.CurrentRole);
        Assert.Equal(NodeRole.Leader, node2.CurrentRole);
        Assert.Equal(node2.Id, node3.CurrentLeaderId);
        Assert.Null(node1.CurrentLeaderId);
    }

    [Fact]
    public void OfflineNode_ShouldNotReceiveProposals()
    {
        var leader = new Node(1);
        var offlineNode = new Node(2);
        var onlineNode = new Node(3);

        leader.AddNeighbor(offlineNode);
        leader.AddNeighbor(onlineNode);
        offlineNode.AddNeighbor(leader);
        onlineNode.AddNeighbor(leader);

        leader.StartElection();
        offlineNode.SimulateOffline();

        leader.ProposeState(10);

        Assert.Equal(0, offlineNode.CurrentState); // State should remain unchanged
        Assert.Equal(10, onlineNode.CurrentState);
    }

    [Fact]
    public void OfflineNode_ShouldNotProposeState()
    {
        var leader = new Node(1);
        var offlineNode = new Node(2);
        var onlineNode = new Node(3);

        leader.AddNeighbor(offlineNode);
        leader.AddNeighbor(onlineNode);
        offlineNode.AddNeighbor(leader);
        onlineNode.AddNeighbor(leader);

        leader.StartElection();
        offlineNode.SimulateOffline();

        offlineNode.ProposeState(15);

        Assert.Equal(0, leader.CurrentState); // Leader's state should remain unchanged
        Assert.Equal(0, onlineNode.CurrentState);
        Assert.Equal(0, offlineNode.CurrentState);
    }

    [Fact]
    public void OfflineNode_ShouldNotVote()
    {
        var node1 = new Node(1);
        var offlineNode = new Node(2);
        var node3 = new Node(3);

        node1.AddNeighbor(offlineNode);
        node1.AddNeighbor(node3);
        offlineNode.AddNeighbor(node1);
        offlineNode.AddNeighbor(node3);
        node3.AddNeighbor(node1);
        node3.AddNeighbor(offlineNode);

        offlineNode.SimulateOffline();
        node1.StartElection();

        Assert.Null(offlineNode.VotedFor);
        Assert.Equal(NodeRole.Leader, node1.CurrentRole); // Should still become leader with remaining votes
    }
}
