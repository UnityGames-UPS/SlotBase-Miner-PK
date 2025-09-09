using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using System;
using UnityEngine.SceneManagement;
using UnityEngine.Networking;
using DG.Tweening;
using System.Linq;
using Newtonsoft.Json;
using Best.SocketIO;
using Best.SocketIO.Events;
using Newtonsoft.Json.Linq;
using System.Runtime.Serialization;
using Best.HTTP.Shared;

public class SocketIOManager : MonoBehaviour
{
    [SerializeField]
    private SlotBehaviour slotManager;

    [SerializeField]
    private UIManager uiManager;

    internal GameData initialData = null;
    internal UiData initUIData = null;
    internal Root resultData = null;
    internal Player playerdata = null;
    [SerializeField]
    internal List<string> bonusdata = null;
    //WebSocket currentSocket = null;
    internal bool isResultdone = false;
    [SerializeField] internal JSFunctCalls JSManager;
    private SocketManager manager;

    protected string SocketURI = null;
    protected string TestSocketURI = "http://localhost:5000/";

    [SerializeField]
    private string testToken;

    protected string gameID = "SL-MNR";
    private Socket gameSocket;
    protected string nameSpace = "playground";

    internal bool isLoading = true;

    internal bool SetInit = false;

    private const int maxReconnectionAttempts = 6;
    private readonly TimeSpan reconnectionDelay = TimeSpan.FromSeconds(10);

    private bool isConnected = false; //Back2 Start
    private bool hasEverConnected = false;
    private const int MaxReconnectAttempts = 5;
    private const float ReconnectDelaySeconds = 2f;

    private float lastPongTime = 0f;
    private float pingInterval = 2f;
    private float pongTimeout = 3f;
    private bool waitingForPong = false;
    private int missedPongs = 0;
    private const int MaxMissedPongs = 5;
    private Coroutine PingRoutine; //Back2 end
    public GameObject RaycastBlocker;

    private void Awake()
    {
        //HACK: The below code is to disable the unity logs
        //Debug.unityLogger.logEnabled = false;

        // Debug.Log("checking awake call Ekansh");
        isLoading = true;
        SetInit = false;
    }

    private void Start()
    {
        //OpenWebsocket();
        //   Debug.Log("checking Start call Ekansh");
        OpenSocket();
    }

    void ReceiveAuthToken(string jsonData)
    {
        Debug.Log("Received data: " + jsonData);

        // Parse the JSON data
        var data = JsonUtility.FromJson<AuthTokenData>(jsonData);
        SocketURI = data.socketURL;
        myAuth = data.cookie;
        nameSpace = data.nameSpace;

        // Proceed with connecting to the server using myAuth and socketURL
    }

    string myAuth = null;

    private void OpenSocket()
    {
        SocketOptions options = new SocketOptions(); //Back2 Start
        options.AutoConnect = false;
        options.Reconnection = false;
        options.Timeout = TimeSpan.FromSeconds(3); //Back2 end
        options.ConnectWith = Best.SocketIO.Transports.TransportTypes.WebSocket; //BackendChanges

#if UNITY_WEBGL && !UNITY_EDITOR
            JSManager.SendCustomMessage("authToken");
            StartCoroutine(WaitForAuthToken(options));
#else
        Func<SocketManager, Socket, object> authFunction = (manager, socket) =>
        {
            return new
            {
                token = testToken,
            };
        };
        options.Auth = authFunction;
        // Proceed with connecting to the server
        SetupSocketManager(options);
#endif
    }

    private IEnumerator WaitForAuthToken(SocketOptions options)
    {
        while (myAuth == null)
        {
            Debug.Log("My Auth is null");
            yield return null;
        }
        while (SocketURI == null)
        {
            Debug.Log("My Socket is null");
            yield return null;
        }
        Debug.Log("My Auth is not null");
        // Once myAuth is set, configure the authFunction
        Func<SocketManager, Socket, object> authFunction = (manager, socket) =>
        {
            return new
            {
                token = myAuth
            };
        };
        options.Auth = authFunction;
        // options.HTTPRequestCustomizationCallback= (SocketManager req, HTTPRequest context)=>{
        //     context.SetHeader("Cookie", $"AWSALBTG={AWSALBTG};, AWSALBTGCORS={AWSALBTGCORS}");
        //     context.SetHeader("X-Custom-Header", "your_custom_value");

        // };
        Debug.Log("Auth function configured with token: " + myAuth);

        // Proceed with connecting to the server
        SetupSocketManager(options);

        yield return null;
    }

    private void SetupSocketManager(SocketOptions options)
    {
        // Create and setup SocketManager
#if UNITY_EDITOR
        this.manager = new SocketManager(new Uri(TestSocketURI), options);
#else
        this.manager = new SocketManager(new Uri(SocketURI), options);
#endif
        if (string.IsNullOrEmpty(nameSpace) | string.IsNullOrWhiteSpace(nameSpace))
        {
            gameSocket = this.manager.Socket;
        }
        else
        {
            Debug.Log("Namespace used :" + nameSpace);
            gameSocket = this.manager.GetSocket("/" + nameSpace);
        }
        // Set subscriptions
        gameSocket.On<ConnectResponse>(SocketIOEventTypes.Connect, OnConnected);
        gameSocket.On<string>(SocketIOEventTypes.Disconnect, OnDisconnected);
        gameSocket.On<Error>(SocketIOEventTypes.Error, OnError);
        gameSocket.On<string>("message", OnListenEvent);
        gameSocket.On<string>("game:init", OnListenEvent);
        gameSocket.On<string>("result", OnListenEvent);
        gameSocket.On<bool>("socketState", OnSocketState);
        gameSocket.On<string>("internalError", OnSocketError);
        gameSocket.On<string>("alert", OnSocketAlert);
        gameSocket.On<string>("pong", OnPongReceived);
        gameSocket.On<string>("AnotherDevice", OnSocketOtherDevice);
        manager.Open();
    }

    // Connected event handler implementation
    void OnConnected(ConnectResponse resp)
    {
        Debug.Log("✅ Connected to server.");

        if (hasEverConnected)
        {
            uiManager.CheckAndClosePopups();
        }

        isConnected = true;
        hasEverConnected = true;
        waitingForPong = false;
        missedPongs = 0;
        lastPongTime = Time.time;
        SendPing();
    }

    private void OnDisconnected(string response)
    {
        Debug.LogWarning("⚠️ Disconnected from server.");
        isConnected = false;
        uiManager.DisconnectionPopup();
        ResetPingRoutine();
    }
    private void OnPongReceived(string data) //Back2 Start
    {
        Debug.Log("✅ Received pong from server.");
        waitingForPong = false;
        missedPongs = 0;
        lastPongTime = Time.time;
        Debug.Log($"⏱️ Updated last pong time: {lastPongTime}");
        Debug.Log($"📦 Pong payload: {data}");
    } //

    private void OnError(Error err)
    {
        Debug.LogError("Error: " + err);
    #if Unity_WEBGL && !UNITY_EDITOR
        JSManager.SendCustomMessage("error");
        #endif
    }

    private void OnListenEvent(string data)
    {
        Debug.Log("Received some_event with data: " + data);
        ParseResponse(data);
    }

    private void OnSocketState(bool state)
    {
        if (state)
        {
            Debug.Log("my state is " + state);
        }
    }
    private void OnSocketError(string data)
    {
        Debug.Log("Received error with data: " + data);
    }
    private void OnSocketAlert(string data)
    {
        Debug.Log("Received alert with data: " + data);
    }

    private void OnSocketOtherDevice(string data)
    {
        Debug.Log("Received Device Error with data: " + data);
        uiManager.ADfunction();
    }

    private void SendPing()
    {
        ResetPingRoutine();
        PingRoutine = StartCoroutine(PingCheck());
    }
    void ResetPingRoutine()
    {
        if (PingRoutine != null)
        {
            StopCoroutine(PingRoutine);
        }
        PingRoutine = null;
    }


    private IEnumerator PingCheck()
    {
        while (true)
        {
            Debug.Log($"🟡 PingCheck | waitingForPong: {waitingForPong}, missedPongs: {missedPongs}, timeSinceLastPong: {Time.time - lastPongTime}");

            if (missedPongs == 0)
            {
                uiManager.CheckAndClosePopups();
            }

            // If waiting for pong, and timeout passed
            if (waitingForPong)
            {
                if (missedPongs == 2)
                {
                    uiManager.ReconnectionPopup();
                }
                missedPongs++;
                Debug.LogWarning($"⚠️ Pong missed #{missedPongs}/{MaxMissedPongs}");

                if (missedPongs >= MaxMissedPongs)
                {
                    Debug.LogError("❌ Unable to connect to server — 5 consecutive pongs missed.");
                    isConnected = false;
                    uiManager.DisconnectionPopup();
                    yield break;
                }
            }

            // Send next ping
            waitingForPong = true;
            lastPongTime = Time.time;
            Debug.Log("📤 Sending ping...");
            SendDataWithNamespace("ping");
            yield return new WaitForSeconds(pingInterval);
        }
    } //Back2 end


    private void AliveRequest()
    {
        SendDataWithNamespace("YES I AM ALIVE");
    }

    private void SendDataWithNamespace(string eventName, string json = null)
    {
        // Send the message
        if (gameSocket != null && gameSocket.IsOpen)
        {
            if (json != null)
            {
                gameSocket.Emit(eventName, json);
                Debug.Log("JSON data sent: " + json);
            }
            else
            {
                gameSocket.Emit(eventName);
            }
        }
        else
        {
            Debug.LogWarning("Socket is not connected.");
        }
    }


    void CloseGame()
    {
      Debug.Log("Unity: Closing Game");
      StartCoroutine(CloseSocket());
    }
    
    internal IEnumerator CloseSocket() //Back2 Start
    {
        RaycastBlocker.SetActive(true);
        ResetPingRoutine();

        Debug.Log("Closing Socket");

        manager?.Close();
        manager = null;

        Debug.Log("Waiting for socket to close");

        yield return new WaitForSeconds(0.5f);

        Debug.Log("Socket Closed");

#if UNITY_WEBGL && !UNITY_EDITOR
    JSManager.SendCustomMessage("OnExit"); //Telling the react platform user wants to quit and go back to homepage
#endif
    }


    private void ParseResponse(string jsonObject)
    {
        Debug.Log(jsonObject);
        Root myData = JsonConvert.DeserializeObject<Root>(jsonObject);

        string id = myData.id;

        switch (id)
        {
            case "initData":
                {
                    initialData = myData.gameData;
                    initUIData = myData.uiData;
                    playerdata = myData.player;
                    // bonusdata = myData.message.BonusData;
                    if (!SetInit)
                    {
                        Debug.Log(jsonObject);
                        List<string> InitialReels = ConvertListOfListsToStrings(initialData.lines);
                        InitialReels = RemoveQuotes(InitialReels);
                        PopulateSlotSocket(InitialReels);
                        SetInit = true;

                        Debug.Log(string.Concat("<color=green><b>", "Done Init Data", "</b></color>"));
                    }
                    else
                    {
                        RefreshUI();
                    }
                    break;
                }
            case "ResultData":
                {
                    Debug.Log(jsonObject);
                    // myData.message.GameData.FinalResultReel = ConvertListOfListsToStrings(myData.message.GameData.ResultReel);
                    // myData.message.GameData.FinalsymbolsToEmit = TransformAndRemoveRecurring(myData.message.GameData.symbolsToEmit);
                    resultData = myData;
                    playerdata = myData.player;
                    isResultdone = true;
                    Debug.Log(string.Concat("<color=green><b>", "Done Result Data", "</b></color>"));
                    break;
                }
            case "ExitUser":
                {
                    if (this.manager != null)
                    {
                        Debug.Log("Dispose my Socket");
                        this.manager.Close();
                    }
#if UNITY_WEBGL && !UNITY_EDITOR
                    JSManager.SendCustomMessage("onExit");
#endif
                    break;
                }
        }
    }

    internal void closeSocketReactnativeCall()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
      JSManager.SendCustomMessage("onExit");
#endif
    }


    private void RefreshUI()
    {
        uiManager.InitialiseUIData(initUIData.paylines);
    }

    private void PopulateSlotSocket(List<string> LineIds)
    {
        slotManager.shuffleInitialMatrix();
        for (int i = 0; i < LineIds.Count; i++)
        {
            slotManager.FetchLines(LineIds[i], i);
        }
        slotManager.SetInitialUI();
        isLoading = false;
#if UNITY_WEBGL && !UNITY_EDITOR
        JSManager.SendCustomMessage("OnEnter");
#endif
        RaycastBlocker.SetActive(false);

    }

    internal void AccumulateResult(int currBet)
    {
        isResultdone = false;
        MessageData message = new MessageData();
        message.type = "SPIN";
        message.payload = new Data();
        message.payload.betIndex = currBet;
        // Serialize message data to JSON
        string json = JsonUtility.ToJson(message);
        SendDataWithNamespace("request", json);
    }

    private List<string> RemoveQuotes(List<string> stringList)
    {
        for (int i = 0; i < stringList.Count; i++)
        {
            stringList[i] = stringList[i].Replace("\"", ""); // Remove inverted commas
        }
        return stringList;
    }

    private List<string> ConvertListListIntToListString(List<List<int>> listOfLists)
    {
        List<string> resultList = new List<string>();

        foreach (List<int> innerList in listOfLists)
        {
            // Convert each integer in the inner list to string
            List<string> stringList = new List<string>();
            foreach (int number in innerList)
            {
                stringList.Add(number.ToString());
            }

            // Join the string representation of integers with ","
            string joinedString = string.Join(",", stringList.ToArray()).Trim();
            resultList.Add(joinedString);
        }

        return resultList;
    }

    private List<string> ConvertListOfListsToStrings(List<List<int>> inputList)
    {
        List<string> outputList = new List<string>();

        foreach (List<int> row in inputList)
        {
            string concatenatedString = string.Join(",", row);
            outputList.Add(concatenatedString);
        }

        return outputList;
    }

    private List<string> TransformAndRemoveRecurring(List<List<string>> originalList)
    {
        // Flattened list
        List<string> flattenedList = new List<string>();
        foreach (List<string> sublist in originalList)
        {
            flattenedList.AddRange(sublist);
        }

        // Remove recurring elements
        HashSet<string> uniqueElements = new HashSet<string>(flattenedList);

        // Transformed list
        List<string> transformedList = new List<string>();
        foreach (string element in uniqueElements)
        {
            transformedList.Add(element.Replace(",", ""));
        }

        return transformedList;
    }
}

[Serializable]
public class BetData
{
    public double currentBet;
    public double currentLines;
    public double spins;
}

[Serializable]
public class AuthData
{
    public string GameID;
    //public double TotalLines;
}

[Serializable]
public class MessageData
{
    // public BetData data;
    // public string id;

    public string type;
    public Data payload;
}
[Serializable]
public class Data
{
    public int betIndex;
    public string Event;
    public double lastWinning;
    public int index;

}
[Serializable]
public class ExitData
{
    public string id;
}

[Serializable]
public class InitData
{
    public AuthData Data;
    public string id;
}

[Serializable]
public class AbtLogo
{
    public string logoSprite { get; set; }
    public string link { get; set; }
}

[Serializable]
public class GameData
{
    public List<List<string>> Reel { get; set; }
    public List<List<int>> lines { get; set; }
    public List<double> bets { get; set; }
    public bool canSwitchLines { get; set; }
    public List<int> LinesCount { get; set; }
    public List<int> autoSpin { get; set; }
    public List<List<string>> ResultReel { get; set; }
    public List<int> linesToEmit { get; set; }
    public List<List<string>> symbolsToEmit { get; set; }
    public double WinAmout { get; set; }
    public FreeSpins freeSpins { get; set; }
    public List<string> FinalsymbolsToEmit { get; set; }
    public List<string> FinalResultReel { get; set; }
    public double jackpot { get; set; }
    public bool isBonus { get; set; }
    public double BonusStopIndex { get; set; }
    public List<int> spinBonus { get; set; }
}

[Serializable]
public class FreeSpins
{
    public int count { get; set; }
    public bool isNewAdded { get; set; }
}

[Serializable]
public class Message
{
    public GameData GameData { get; set; }
    public UiData UIData { get; set; }
    public Player PlayerData { get; set; }
    public List<string> BonusData { get; set; }
}

[Serializable]
public class Root
{
    public string id { get; set; }
    public Message message { get; set; }
    public GameData gameData { get; set; }
    public UiData uiData { get; set; }
    public Player player { get; set; }

    public bool success { get; set; }
    public List<List<string>> matrix { get; set; }
    public Payload payload { get; set; }
    public Jackpot jackpot { get; set; }
    public FreeSpin freeSpin { get; set; }

}
[Serializable]
public class Payload
{
    public double winAmount { get; set; }
    public List<Win> wins { get; set; }
}
[Serializable]
public class Win
{
    public int line { get; set; }
    public List<int> positions { get; set; }
    public double amount { get; set; }
}


[Serializable]
public class Jackpot
{
    public bool isTriggered { get; set; }
    public double amount { get; set; }
}

[Serializable]
public class FreeSpin
{
    public int count { get; set; }
    public bool isFreeSpin { get; set; }
}

[Serializable]
public class UiData
{
    public Paylines paylines { get; set; }
    public List<string> spclSymbolTxt { get; set; }
    public AbtLogo AbtLogo { get; set; }
    public string ToULink { get; set; }
    public string PopLink { get; set; }
}

[Serializable]
public class Paylines
{
    public List<Symbol> symbols { get; set; }
}

[Serializable]
public class Symbol
{
    // public int ID { get; set; }
    // public string Name { get; set; }
    // [JsonProperty("multiplier")]
    // public object MultiplierObject { get; set; }

    // // This property will hold the properly deserialized list of lists of integers
    // [JsonIgnore]
    // public List<List<int>> Multiplier { get; private set; }

    // // Custom deserialization method to handle the conversion
    // [OnDeserialized]
    // internal void OnDeserializedMethod(StreamingContext context)
    // {
    //     // Handle the case where multiplier is an object (empty in JSON)
    //     if (MultiplierObject is JObject)
    //     {
    //         Multiplier = new List<List<int>>();
    //     }
    //     else
    //     {
    //         // Deserialize normally assuming it's an array of arrays
    //         Multiplier = JsonConvert.DeserializeObject<List<List<int>>>(MultiplierObject.ToString());
    //     }
    // }
    // public object defaultAmount { get; set; }
    // public object symbolsCount { get; set; }
    // public object increaseValue { get; set; }
    // public object description { get; set; }
    // public int freeSpin { get; set; }

    public int id { get; set; }
    public string name { get; set; }
    public List<int> multiplier { get; set; }
    public string description { get; set; }
}
[Serializable]
public class Player
{
    public double balance { get; set; }
    public double haveWon { get; set; }
    public double currentWining { get; set; }
}
[Serializable]
public class AuthTokenData
{
    public string cookie;
    public string socketURL;
    public string nameSpace;
}

