using Cinemachine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class GameManager : GenericSingletonClass<GameManager>
{

    private string _lobbyId;

    private RelayHostData _hostData;
    private RelayJoinData _joinData;

    // Setup events

    // Notify state update
    public UnityAction<string> UpdateState;
    // Notify Match found
    public UnityAction MatchFound;
    public GameObject serverPlayer { get; private set; }
    protected override void Awake()
    {
        base.Awake();
    }
    void Start()
    {

        //UnitySignIn();
        // Subscribe to NetworkManager events
        NetworkManager.Singleton.OnClientConnectedCallback += ClientConnected;
    }

    public async void UnitySignIn()
    {
        // Initialize unity services
        await UnityServices.InitializeAsync();

        // Setup events listeners
        SetupEvents();

        // Unity Login
        await SignInAnonymouslyAsync();
    }

    #region Network events

    private void ClientConnected(ulong id)
    {
        // Player with id connected to our session

        Debug.Log("Connected player with id: " + id);

        UpdateState?.Invoke("Player found!");
        //MatchFound?.Invoke();
    }

    #endregion


    #region UnityLogin

    void SetupEvents()
    {
        AuthenticationService.Instance.SignedIn += () =>
        {
            // Shows how to get a playerID
            Debug.Log($"PlayerID: {AuthenticationService.Instance.PlayerId}");

            // Shows how to get an access token
            Debug.Log($"Access Token: {AuthenticationService.Instance.AccessToken}");
        };

        AuthenticationService.Instance.SignInFailed += (err) =>
        {
            Debug.LogError(err);
        };

        AuthenticationService.Instance.SignedOut += () =>
        {
            Debug.Log("Player signed out.");
        };
    }

    async Task SignInAnonymouslyAsync()
    {
        try
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
            Debug.Log("Sign in anonymously succeeded!");
        }
        catch (Exception ex)
        {
            // Notify the player with the proper error message
            Debug.LogException(ex);
        }
    }

    #endregion

    public void AddNewServerPlayer(GameObject player)
    {
        /*serverPlayer = Instantiate(player);
        serverPlayer.SetActive(false);
        DontDestroyOnLoad(serverPlayer);*/
        SceneManager.LoadScene("GamePlay");
    }
    internal IEnumerator SetPlayerPos(Transform obj, CinemachineFreeLook cinemachineFreeLook)
    {
        WaitForEndOfFrame waitForEndOfFrame = new();
        NetworkObject networkObject = null;
        while (networkObject == null || !networkObject.IsSpawned)
        {
            if (networkObject == null) networkObject = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject();

            yield return waitForEndOfFrame;
        }
        serverPlayer = networkObject.gameObject;
        //var temp = Instantiate(serverPlayer, networkObject.gameObject.transform);
        //NetworkManager.Singleton.AddNetworkPrefab(serverPlayer);
        //temp.GetComponent<NetworkObject>().Spawn();
        //Destroy(serverPlayer);
        //serverPlayer = temp;
        //serverPlayer.SetActive(true);
        serverPlayer.transform.SetPositionAndRotation(obj.position, obj.rotation);
        cinemachineFreeLook.Follow = cinemachineFreeLook.LookAt = serverPlayer.GetComponentsInChildren<Transform>().Where(r => r.CompareTag("CinemachineTarget")).FirstOrDefault();
    }
    public void BackToMenu()
    {
        //NetworkManager.Singleton.PrefabHandler.RemoveHandler(serverPlayer);
        //NetworkManager.Singleton.RemoveNetworkPrefab(serverPlayer);
        Destroy(serverPlayer);
        NetworkManager.Singleton.DisconnectClient(NetworkManager.Singleton.LocalClientId);
        SceneManager.LoadScene(1);
    }

    #region Lobby

    public async void FindMatch()
    {
        Debug.Log("Looking for a lobby...");

        UpdateState?.Invoke("Looking for a match...");

        try
        {
            // Looking for a lobby

            // Add options to the matchmaking (mode, rank, etc..)
            QuickJoinLobbyOptions options = new QuickJoinLobbyOptions();

            // Quick-join a random lobby
            Lobby lobby = await Lobbies.Instance.QuickJoinLobbyAsync(options);

            Debug.Log("Joined lobby: " + lobby.Id);
            Debug.Log("Lobby Players: " + lobby.Players.Count);

            // Retrieve the Relay code previously set in the create match
            string joinCode = lobby.Data["joinCode"].Value;

            Debug.Log("Received code: " + joinCode);

            JoinAllocation allocation = await Relay.Instance.JoinAllocationAsync(joinCode);

            // Create Object
            _joinData = new RelayJoinData
            {
                Key = allocation.Key,
                Port = (ushort)allocation.RelayServer.Port,
                AllocationID = allocation.AllocationId,
                AllocationIDBytes = allocation.AllocationIdBytes,
                ConnectionData = allocation.ConnectionData,
                HostConnectionData = allocation.HostConnectionData,
                IPv4Address = allocation.RelayServer.IpV4
            };

            // Set transport data
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(
                _joinData.IPv4Address,
                _joinData.Port,
                _joinData.AllocationIDBytes,
                _joinData.Key,
                _joinData.ConnectionData,
                _joinData.HostConnectionData);

            // Finally start the client
            NetworkManager.Singleton.StartClient();

            // Trigger events
            UpdateState?.Invoke("Match found!");
            MatchFound?.Invoke();
        }
        catch (LobbyServiceException e)
        {
            // If we don't find any lobby, let's create a new one
            Debug.Log("Cannot find a lobby: " + e);
            CreateMatch();
        }
    }

    private async void CreateMatch()
    {
        Debug.Log("Creating a new lobby...");

        UpdateState?.Invoke("Creating a new match...");

        // External connections
        int maxConnections = 10;

        try
        {
            // Create RELAY object
            Allocation allocation = await Relay.Instance.CreateAllocationAsync(maxConnections);
            _hostData = new RelayHostData
            {
                Key = allocation.Key,
                Port = (ushort)allocation.RelayServer.Port,
                AllocationID = allocation.AllocationId,
                AllocationIDBytes = allocation.AllocationIdBytes,
                ConnectionData = allocation.ConnectionData,
                IPv4Address = allocation.RelayServer.IpV4
            };

            // Retrieve JoinCode
            _hostData.JoinCode = await Relay.Instance.GetJoinCodeAsync(allocation.AllocationId);

            string lobbyName = "game_lobby";
            int maxPlayers = 100;
            CreateLobbyOptions options = new CreateLobbyOptions();
            options.IsPrivate = false;

            // Put the JoinCode in the lobby data, visible by every member
            options.Data = new Dictionary<string, DataObject>()
            {
                {
                    "joinCode", new DataObject(
                        visibility: DataObject.VisibilityOptions.Member,
                        value: _hostData.JoinCode)
                },
            };

            // Create the lobby
            var lobby = await Lobbies.Instance.CreateLobbyAsync(lobbyName, maxPlayers, options);

            // Save Lobby ID for later uses
            _lobbyId = lobby.Id;

            Debug.Log("Created lobby: " + lobby.Id);

            // Heartbeat the lobby every 15 seconds.
            StartCoroutine(HeartbeatLobbyCoroutine(lobby.Id, 15));

            // Now that RELAY and LOBBY are set...

            // Set Transports data
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(
                _hostData.IPv4Address,
                _hostData.Port,
                _hostData.AllocationIDBytes,
                _hostData.Key,
                _hostData.ConnectionData);

            // Finally start host
            NetworkManager.Singleton.StartHost();

            UpdateState?.Invoke("Waiting for players...");
            MatchFound?.Invoke();
        }
        catch (LobbyServiceException e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    IEnumerator HeartbeatLobbyCoroutine(string lobbyId, float waitTimeSeconds)
    {
        var delay = new WaitForSecondsRealtime(waitTimeSeconds);
        while (true)
        {
            Lobbies.Instance.SendHeartbeatPingAsync(lobbyId);
            Debug.Log("Lobby Heartbit");
            yield return delay;
        }
    }

    private void OnDestroy()
    {
        // We need to delete the lobby when we're not using it
        //BackToMenu();
        //Lobbies.Instance.DeleteLobbyAsync(_lobbyId);
    }

    #endregion

    /// <summary>
    /// RelayHostData represents the necessary informations
    /// for a Host to host a game on a Relay
    /// </summary>
    public struct RelayHostData
    {
        public string JoinCode;
        public string IPv4Address;
        public ushort Port;
        public Guid AllocationID;
        public byte[] AllocationIDBytes;
        public byte[] ConnectionData;
        public byte[] Key;
    }

    /// <summary>
    /// RelayHostData represents the necessary informations
    /// for a Host to host a game on a Relay
    /// </summary>
    public struct RelayJoinData
    {
        public string JoinCode;
        public string IPv4Address;
        public ushort Port;
        public Guid AllocationID;
        public byte[] AllocationIDBytes;
        public byte[] ConnectionData;
        public byte[] HostConnectionData;
        public byte[] Key;
    }
}