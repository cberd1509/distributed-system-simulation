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
    private readonly List<string> _logEntries = [];

    /// <summary>
    /// Whether the node is offline or not (Simulated)
    /// </summary>
    private bool IsOffline { get; set; } = false;

    /// <summary>
    /// The current role of the node
    /// </summary>
    private NodeRole _currentRole = NodeRole.Follower;

    /// <summary>
    /// The current term of the leadership election
    /// </summary>
    private int _currentTerm = 0;

    /// <summary>
    /// The node that this node voted for in the current term
    /// </summary>
    private int? _votedFor = null;

    /// <summary>
    /// The votes received from other nodes in the current term
    /// </summary>
    private Dictionary<int, int> _votesReceived = new();

    /// <summary>
    /// The current state of the node
    /// </summary>
    private int _currentState = 0;

    /// <summary>
    /// The ID of the current leader
    /// </summary>
    private int? _currentLeaderId = null;

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

        if (_currentRole == NodeRole.Leader)
        {
            // Leader handles proposal directly
            _currentState = state;
            Log($"Leader {Id} proposing new state: {state}");

            foreach (var neighbor in Neighbors)
            {
                neighbor.HandleProposal(state, Id);
            }
        }
        else if (_currentLeaderId != null)
        {
            // Forward proposal to the leader
            var leader = Neighbors.FirstOrDefault(n => n.Id == _currentLeaderId);
            if (leader != null)
            {
                Log($"Node {Id} forwarding proposal {state} to Leader {_currentLeaderId}");
                leader.ProposeState(state);
            }
            else
            {
                Log($"Leader {_currentLeaderId} not found, cannot forward proposal");
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

        if (state >= _currentState)
        {
            _currentState = state;
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

        if (_currentRole == NodeRole.Leader)
        {
            neighbor.ReceiveHeartbeat(_currentTerm, Id);
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
        if (_currentRole == NodeRole.Leader)
        {
            // Clear the leader id for this and all neighbors, next time they will start an election
            _currentLeaderId = null;
            _currentRole = NodeRole.Follower;

            foreach (var neighbor in Neighbors)
            {
                neighbor._currentLeaderId = null;
                neighbor._currentRole = NodeRole.Follower;
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
            if (neighbor._currentLeaderId.HasValue)
            {
                _currentLeaderId = neighbor._currentLeaderId;
                Log($"Discovered Leader {_currentLeaderId} from Node {neighbor.Id}");

                var leader = Neighbors.FirstOrDefault(n => n.Id == _currentLeaderId);
                leader?.SynchronizeFollower(this);
                return;
            }

            // Additionally, if neighbor itself is the leader
            if (neighbor._currentRole == NodeRole.Leader)
            {
                _currentLeaderId = neighbor.Id;
                Log($"Discovered Leader {_currentLeaderId} directly (Neighbor Node {neighbor.Id})");

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
        if (_currentRole != NodeRole.Leader)
        {
            Log($"Cannot synchronize follower {follower.Id}, not the leader.");
            return;
        }

        follower.ReceiveSynchronization(_currentState);
    }

    /// <summary>
    /// Receives a synchronization from the leader
    /// </summary>
    /// <param name="state">The state to synchronize with</param>
    public void ReceiveSynchronization(int state)
    {
        if (state > _currentState)
        {
            Log($"Synchronized state updated from {_currentState} to {state}");
            _currentState = state;
        }
        else
        {
            Log($"No synchronization needed. Current state is up-to-date: {_currentState}");
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

        _currentRole = NodeRole.Candidate;
        _currentTerm++;
        _votesReceived.Clear();
        _votesReceived[Id] = _currentTerm;
        _votedFor = Id;

        Log($"Started election for term {_currentTerm}");

        foreach (var neighbor in Neighbors)
        {
            neighbor.RequestVote(_currentTerm, Id);
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
        if (_currentRole != NodeRole.Candidate || term != _currentTerm)
            return;

        _votesReceived[voterId] = term;
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
            neighbor.ReceiveHeartbeat(_currentTerm, Id);
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

        bool leaderChanged = _currentLeaderId != leaderId;

        if (term >= _currentTerm || leaderChanged)
        {
            _currentTerm = term;
            _currentRole = NodeRole.Follower;
            _currentLeaderId = leaderId;
            Log($"Heartbeat received from Leader {leaderId} for term {term}");

            if (leaderChanged || _currentState == 0)
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
        if (_currentRole == NodeRole.Leader)
            return;

        var majority = (Neighbors.Count + 1) / 2 + 1;
        if (_votesReceived.Count >= majority)
        {
            _currentRole = NodeRole.Leader;
            _currentLeaderId = Id; // set self as leader
            Log($"Node {Id} became Leader for term {_currentTerm}");
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

        if (term > _currentTerm)
        {
            _currentTerm = term;
            _votedFor = null;
            _currentRole = NodeRole.Follower;
        }

        if (_votedFor == null)
        {
            _votedFor = candidateId;
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
        _logEntries.Add(message);
    }

    /// <summary>
    /// Displays the log entries for the node
    /// </summary>
    public void DisplayLog()
    {
        Console.WriteLine($"Node {Id} Log:");
        foreach (var entry in _logEntries)
        {
            Console.WriteLine(entry);
        }
    }
}
