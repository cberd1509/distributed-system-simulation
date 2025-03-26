using System.IO.Compression;

namespace DistributedSystemSimulation.Models;

/// <summary>
/// The role of the node in the distributed system
/// </summary>
public enum NodeRole
{
    Follower,
    Candidate,
    Leader
}

/// <summary>
/// A node in the distributed system
/// </summary>
public class Node
{
    /// <summary>
    /// The unique ID of the node
    /// </summary>
    public int Id { get; }

    /// <summary>
    /// The neighbors of the node, contains the
    /// </summary>
    public HashSet<Node> Neighbors { get; } = [];

    /// <summary>
    /// The log entries of the node
    /// </summary>
    public List<string> LogEntries { get; } = [];

    /// <summary>
    /// Whether the node is offline or not (Simulated)
    /// </summary>
    public bool IsOffline { get; set; } = false;

    /// <summary>
    /// The current role of the node
    /// </summary>
    public NodeRole CurrentRole { get; set; } = NodeRole.Follower;

    /// <summary>
    /// The current term of the leadership election
    /// </summary>
    public int CurrentTerm { get; set; } = 0;

    /// <summary>
    /// The node that this node voted for in the current term
    /// </summary>
    public int? VotedFor { get; set; } = null;

    /// <summary>
    /// The votes received from other nodes in the current term
    /// </summary>
    public Dictionary<int, int> VotesReceived { get; } = new();

    /// <summary>
    /// The current state of the node
    /// </summary>
    public int CurrentState { get; set; } = 0;

    /// <summary>
    /// The ID of the current leader
    /// </summary>
    public int? CurrentLeaderId { get; set; } = null;

    /// <summary>
    /// Initializes a new instance of the Node class
    /// </summary>
    /// <param name="id">The ID of the node</param>
    public Node(int id)
    {
        Id = id;
    }

    /// <summary>
    /// Proposes a new state for the node
    /// </summary>
    /// <param name="state">The new state to propose</param>
    public void ProposeState(int state)
    {
        if (IsOffline)
        {
            Log($"Node {Id} is offline, cannot propose state");
            return;
        }

        if (CurrentRole == NodeRole.Leader)
        {
            // Leader handles proposal directly
            CurrentState = state;
            Log($"Leader {Id} proposing new state: {state}");

            foreach (var neighbor in Neighbors)
            {
                neighbor.HandleProposal(state, Id);
            }
        }
        else if (CurrentLeaderId != null)
        {
            // Forward proposal to the leader
            var leader = Neighbors.FirstOrDefault(n => n.Id == CurrentLeaderId);
            if (leader != null)
            {
                Log($"Node {Id} forwarding proposal {state} to Leader {CurrentLeaderId}");
                leader.ProposeState(state);
            }
            else
            {
                Log($"Leader {CurrentLeaderId} not found, cannot forward proposal");
            }
        }
        else
        {
            Log($"Node {Id} has no leader information, cannot forward proposal");
        }
    }

    /// <summary>
    /// Handles a proposal from a neighbor
    /// </summary>
    /// <param name="state">The state to propose</param>
    /// <param name="proposerId">The ID of the node that proposed the state</param>
    public void HandleProposal(int state, int proposerId)
    {
        if (IsOffline)
        {
            Log($"Node {Id} is offline, cannot handle proposal from {proposerId}");
            return;
        }

        if (state >= CurrentState)
        {
            CurrentState = state;
            Log($"Node {Id} accepted proposal {state} from Leader {proposerId}");
        }
    }

    /// <summary>
    /// Adds a neighbor to the node
    /// </summary>
    /// <param name="neighbor">The neighbor to add</param>
    public void AddNeighbor(Node neighbor)
    {
        Neighbors.Add(neighbor);

        if (CurrentRole == NodeRole.Leader)
        {
            neighbor.ReceiveHeartbeat(CurrentTerm, Id);
            SynchronizeFollower(neighbor);
        }
    }

    /// <summary>
    /// Simulates the node going offline, if the node is the leader, it will clear the leader id of all neighbors and start an election with the first online neighbor
    /// </summary>
    public void SimulateOffline()
    {
        IsOffline = true;
        Log($"Node {Id} is now offline");

        // If the node is the leader, it should start an election
        // In a real scenario, we would have heartbeat mechanism to detect if the leader is offline and elect a new leader
        if (CurrentRole == NodeRole.Leader)
        {
            // Clear the leader id for this and all neighbors, next time they will start an election
            CurrentLeaderId = null;
            CurrentRole = NodeRole.Follower;

            foreach (var neighbor in Neighbors)
            {
                neighbor.CurrentLeaderId = null;
                neighbor.CurrentRole = NodeRole.Follower;
            }

            // Let any of the remaining online neighbors start an election
            var firstOnlineNeighbor = Neighbors.FirstOrDefault(n => !n.IsOffline);
            firstOnlineNeighbor?.StartElection();
        }
    }

    /// <summary>
    /// Simulates the node coming back online, the node will ask its neighbors who the leader is and synchronize its state with the leader
    /// </summary>
    public void SimulateOnline()
    {
        IsOffline = false;
        Log($"Node {Id} is back online.");

        // Ask neighbors synchronously who the leader is
        foreach (var neighbor in Neighbors.Where(n => !n.IsOffline))
        {
            if (neighbor.CurrentLeaderId.HasValue)
            {
                CurrentLeaderId = neighbor.CurrentLeaderId;
                Log($"Discovered Leader {CurrentLeaderId} from Node {neighbor.Id}");

                var leader = Neighbors.FirstOrDefault(n => n.Id == CurrentLeaderId);
                leader?.SynchronizeFollower(this);
                return;
            }

            // Additionally, if neighbor itself is the leader
            if (neighbor.CurrentRole == NodeRole.Leader)
            {
                CurrentLeaderId = neighbor.Id;
                Log($"Discovered Leader {CurrentLeaderId} directly (Neighbor Node {neighbor.Id})");

                neighbor.SynchronizeFollower(this);
                return;
            }
        }

        // No neighbor knows the leader; only then start election
        Log("No known leader found in neighbors. Starting election.");
        StartElection();
    }

    /// <summary>
    /// Synchronizes the state of a follower with the current state
    /// </summary>
    /// <param name="follower">The follower to synchronize with</param>
    public void SynchronizeFollower(Node follower)
    {
        if (CurrentRole != NodeRole.Leader)
        {
            Log($"Cannot synchronize follower {follower.Id}, not the leader.");
            return;
        }

        follower.ReceiveSynchronization(CurrentState);
    }

    /// <summary>
    /// Receives a synchronization from the leader
    /// </summary>
    /// <param name="state">The state to synchronize with</param>
    public void ReceiveSynchronization(int state)
    {
        if (state > CurrentState)
        {
            Log($"Synchronized state updated from {CurrentState} to {state}");
            CurrentState = state;
        }
        else
        {
            Log($"No synchronization needed. Current state is up-to-date: {CurrentState}");
        }
    }

    /// <summary>
    /// Starts an election using the current node as the candidate
    /// </summary>
    public void StartElection()
    {
        if (IsOffline)
        {
            Log($"Node {Id} is offline, cannot start election");
            return;
        }

        CurrentRole = NodeRole.Candidate;
        CurrentTerm++;
        VotesReceived.Clear();
        VotesReceived[Id] = CurrentTerm;
        VotedFor = Id;

        Log($"Started election for term {CurrentTerm}");

        foreach (var neighbor in Neighbors)
        {
            neighbor.RequestVote(CurrentTerm, Id);
        }

        CheckElectionResult();
    }

    /// <summary>
    /// Receives a vote from a neighbor
    /// </summary>
    /// <param name="term">The term of the vote</param>
    /// <param name="voterId">The ID of the node that voted</param>
    public void ReceiveVote(int term, int voterId)
    {
        if (CurrentRole != NodeRole.Candidate || term != CurrentTerm)
            return;

        VotesReceived[voterId] = term;
        Log($"Received vote from Node {voterId} in term {term}");
        CheckElectionResult();
    }

    /// <summary>
    /// Sends a heartbeat to all neighbors. In a online scenario, this would be done by the leader to all followers and would be done periodically.
    /// </summary>
    private void SendHeartbeat()
    {
        if (IsOffline)
        {
            Log($"Node {Id} is offline, no heartbeat needed");
            return;
        }

        foreach (var neighbor in Neighbors)
        {
            neighbor.ReceiveHeartbeat(CurrentTerm, Id);
        }
    }

    /// <summary>
    /// Receives a heartbeat from a leader
    /// </summary>
    /// <param name="term">The term of the heartbeat</param>
    /// <param name="leaderId">The ID of the leader that sent the heartbeat</param>
    public void ReceiveHeartbeat(int term, int leaderId)
    {
        if (IsOffline)
        {
            Log($"Node {Id} is offline, missed heartbeat from Leader {leaderId}");
            return;
        }

        bool leaderChanged = CurrentLeaderId != leaderId;

        if (term >= CurrentTerm || leaderChanged)
        {
            CurrentTerm = term;
            CurrentRole = NodeRole.Follower;
            CurrentLeaderId = leaderId;
            Log($"Heartbeat received from Leader {leaderId} for term {term}");

            if (leaderChanged || CurrentState == 0)
            {
                var leader = Neighbors.FirstOrDefault(n => n.Id == leaderId);
                leader?.SynchronizeFollower(this);
            }
        }
    }

    /// <summary>
    /// Checks if this node won the election
    /// </summary>
    private void CheckElectionResult()
    {
        if (CurrentRole == NodeRole.Leader)
            return;

        var majority = (Neighbors.Count + 1) / 2 + 1;
        if (VotesReceived.Count >= majority)
        {
            CurrentRole = NodeRole.Leader;
            CurrentLeaderId = Id; // set self as leader
            Log($"Node {Id} became Leader for term {CurrentTerm}");
            SendHeartbeat();
        }
    }

    /// <summary>
    /// Requests a vote from a neighbor
    /// </summary>
    /// <param name="term">The term of the vote</param>
    /// <param name="candidateId">The ID of the node that requested the vote</param>
    public void RequestVote(int term, int candidateId)
    {
        if (IsOffline)
        {
            Log($"Node {Id} is offline, cannot vote for {candidateId}");
            return;
        }

        if (term > CurrentTerm)
        {
            CurrentTerm = term;
            VotedFor = null;
            CurrentRole = NodeRole.Follower;
        }

        if (VotedFor == null)
        {
            VotedFor = candidateId;
            Log($"Voted for Node {candidateId} in term {term}");
            var candidate = Neighbors.FirstOrDefault(n => n.Id == candidateId);
            candidate?.ReceiveVote(term, Id);
        }
    }

    /// <summary>
    /// Logs a message to the console and the log entries list
    /// </summary>
    /// <param name="message">The message to log</param>
    public void Log(string message)
    {
        Console.WriteLine($"Node {Id}: {message}");
        LogEntries.Add(message);
    }

    /// <summary>
    /// Displays the log entries for the node
    /// </summary>
    public void DisplayLog()
    {
        Console.WriteLine($"Node {Id} Log:");
        foreach (var entry in LogEntries)
        {
            Console.WriteLine(entry);
        }
    }
}
