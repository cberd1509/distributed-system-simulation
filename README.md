# Distributed System Simulation

This is a simulation of a distributed system using the Raft algorithm. For this, we're simulating a distributed system with 3 nodes.

## System Requirements

- .NET 8.0
- Visual Studio 2022 or Visual Studio Code with C# Dev Kit installed
- .NET CLI

## How to run

1. Clone the repository
2. cd into the DistributedSystemSimulation folder
3. ```dotnet run```
4. The output will be displayed in the console

## Step by Step Simulation

1. We initialize the three nodes with a unique Id.
2. Then, we run a process to elect the initial leader.
3. Node 1 is elected as the leader and then proposes a new state.
4. Node 2 and Node 3 receive the proposal and acknowledge it, updating their state.
5. Node 2 then propose a state. As it is not the leader, it will forward the proposal to the leader. Which is Node 1, it will receive the proposal and acknowledge it, updating its state, then it will propose a new state to Node 2 and Node 3.
6. Node 2 will go offline, it will not receive any new proposals.
7. Node 1 will propose a new state again, this time, only Node 3 will receive the proposal. Node 2 will not receive the proposal as it is offline.
8. Node 2 will go back online, it will discover that Node 1 is the leader and will synchronize its state with Node 1.
9. Node 1 will propose a new state again, this time, Node 2 and Node 3 will receive the proposal.
10. Node 1 will go offline, it will not receive any new proposals. Also, a new leader needs to be elected. Node 2 gets elected as the new leader.
11. Node 3 will propose a new state, as it is not the leader, it will forward the proposal to the leader. Which is now Node 2, it will receive the proposal and acknowledge it, updating its state, then it will propose a new state to Node 1 and Node 3. As Node 1 is offline, it will not receive the proposal, only Node 3 will receive the proposal and acknowledge it.
12. Node 1 will go back online, it will discover that Node 2 is the leader and will synchronize its state with Node 2. It will remain as a follower.
13. Node 1 will propose a new state again, this time, as it is not the leader, it will forward the proposal to the leader. Which is now Node 2, it will receive the proposal and acknowledge it, updating its state, then it will propose a new state to Node 1 and Node 3. Now Node 1 and Node 3 will receive the proposal and acknowledge it.
14. Program will display the individual logs of each node and the final state of the distributed system.

<img width="780" alt="image" src="https://github.com/user-attachments/assets/a6c70f26-0d35-483c-95cc-382cf29a81be" />


## Running Tests

We're using xUnit for the tests. To run the tests, you can use the following command:
1. ```dotnet test```


