using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using YallaCatch.API;
using YallaCatch.UI;
using YallaCatch.Managers;
#if !UNITY_WEBGL
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
#endif

namespace YallaCatch.Networking
{
    /// <summary>
    /// Minimal Socket.IO (Engine.IO v4 over WebSocket) client for Unity.
    /// Supports:
    /// - Auth via Authorization header and Socket.IO auth payload
    /// - Engine.IO open / ping / pong
    /// - Socket.IO connect / event / error / disconnect packets
    /// - Event dispatch for notification + game_update streams
    /// </summary>
    public class SocketIOClient : MonoBehaviour
    {
        public static SocketIOClient Instance { get; private set; }

        [Header("Configuration")]
        [SerializeField] private string socketUrl = "ws://localhost:3000/socket.io/?EIO=4&transport=websocket";
        [SerializeField] private bool autoConnect = true;
        [SerializeField] private bool deriveSocketUrlFromApiBase = true;
        [SerializeField] private float authTokenRetryWindowSeconds = 2f;
        [SerializeField] private float connectTimeoutSeconds = 10f;
        [SerializeField] private bool autoReconnect = true;
        [SerializeField] private float reconnectInitialDelaySeconds = 1f;
        [SerializeField] private float reconnectMaxDelaySeconds = 15f;
        [SerializeField] private int reconnectMaxAttempts = 8; // <= 0 => unlimited

        private WebSockerWrapper ws;
        private bool isConnected;
        private bool isConnecting;
        private bool isWaitingForAuthToken;
        private bool isShuttingDown;
        private bool manualDisconnectRequested;
        private bool disconnectEventRaised;
        private float connectStartedAt;
        private float lastEnginePingAt;
        private float enginePingTimeoutSeconds = 70f;
        private int reconnectAttemptCount;
        private string lastConnectedAccessToken = string.Empty;
        private Coroutine reconnectCoroutine;
        private readonly Queue<Action> mainThreadQueue = new Queue<Action>();
        private readonly object queueLock = new object();
        private readonly HashSet<string> subscribedRooms = new HashSet<string>(StringComparer.Ordinal);

        // Events
        public event Action OnConnected;
        public event Action OnDisconnected;
        public event Action<string, string> OnEventReceived; // (eventName, serialized JSON payload)
        public event Action<string> OnError;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            if (APIClient.Instance != null)
            {
                APIClient.Instance.OnLoggedOut += HandleLoggedOut;
                APIClient.Instance.OnTokensUpdated += HandleTokensUpdated;
            }

            if (autoConnect && APIClient.Instance != null && APIClient.Instance.IsAuthenticated)
            {
                Connect();
            }
        }

        public void Connect()
        {
            manualDisconnectRequested = false;
            CancelReconnect();

            if (isConnected || isConnecting || ws != null)
            {
                return;
            }

            if (APIClient.Instance == null)
            {
                FailConnection("LIVE SIGNAL UNAVAILABLE: API client not initialized.");
                return;
            }

            string token = APIClient.Instance.GetAccessToken();
            if (string.IsNullOrEmpty(token))
            {
                if (!isWaitingForAuthToken)
                {
                    StartCoroutine(WaitForTokenAndConnect());
                }
                return;
            }

            string fullUrl = ResolveSocketUrl();
            Debug.Log($"[SocketIO] Connecting to {fullUrl} (auth token via Socket.IO handshake payload)");
            StartCoroutine(ConnectRoutine(fullUrl, token));
        }

        public void Disconnect()
        {
            DisconnectInternal(clearSubscriptions: false, manual: true);
        }

        private void DisconnectInternal(bool clearSubscriptions, bool manual)
        {
            if (clearSubscriptions)
            {
                subscribedRooms.Clear();
            }

            manualDisconnectRequested = manual;
            CancelReconnect();

            if (!isConnected && !isConnecting && ws == null)
            {
                return;
            }

            Debug.Log("[SocketIO] Disconnecting");
            isConnected = false;
            isConnecting = false;
            isWaitingForAuthToken = false;
            connectStartedAt = 0f;
            lastConnectedAccessToken = string.Empty;

            if (ws != null)
            {
                ws.OnTextMessage -= HandleTransportTextAnyThread;
                ws.OnTransportError -= HandleTransportErrorAnyThread;
                ws.OnTransportClosed -= HandleTransportClosedAnyThread;

                // Socket.IO namespace disconnect packet (best effort) before closing transport.
                ws.SendText("41");
                var closingWs = ws;
                ws = null;
                closingWs.Close();
            }

            RaiseDisconnectedOnce();
        }

        private IEnumerator ConnectRoutine(string url, string authToken)
        {
            isConnecting = true;
            disconnectEventRaised = false;
            connectStartedAt = Time.time;
            lastEnginePingAt = Time.time;

            var wrapper = new WebSockerWrapper();
            wrapper.OnTextMessage += HandleTransportTextAnyThread;
            wrapper.OnTransportError += HandleTransportErrorAnyThread;
            wrapper.OnTransportClosed += HandleTransportClosedAnyThread;

            bool connectFinished = false;
            Exception connectException = null;

            wrapper.Connect(url, authToken, () =>
            {
                EnqueueMainThread(() =>
                {
                    connectFinished = true;
                });
            }, ex =>
            {
                EnqueueMainThread(() =>
                {
                    connectException = ex;
                    connectFinished = true;
                });
            });

            while (!connectFinished)
            {
                if (Time.time - connectStartedAt > connectTimeoutSeconds)
                {
                    wrapper.Close();
                    wrapper.OnTextMessage -= HandleTransportTextAnyThread;
                    wrapper.OnTransportError -= HandleTransportErrorAnyThread;
                    wrapper.OnTransportClosed -= HandleTransportClosedAnyThread;

                    isConnecting = false;
                    FailConnection("LIVE SIGNAL UNAVAILABLE: Socket connect timeout.");
                    yield break;
                }

                yield return null;
            }

            if (connectException != null)
            {
                wrapper.OnTextMessage -= HandleTransportTextAnyThread;
                wrapper.OnTransportError -= HandleTransportErrorAnyThread;
                wrapper.OnTransportClosed -= HandleTransportClosedAnyThread;
                isConnecting = false;
                FailConnection($"LIVE SIGNAL UNAVAILABLE: {connectException.Message}");
                yield break;
            }

            ws = wrapper;
            // Wait for server Engine.IO open + Socket.IO connect ack handled in incoming packet parser.
        }

        private IEnumerator WaitForTokenAndConnect()
        {
            isWaitingForAuthToken = true;

            float elapsed = 0f;
            while (elapsed < authTokenRetryWindowSeconds)
            {
                if (APIClient.Instance != null && !string.IsNullOrEmpty(APIClient.Instance.GetAccessToken()))
                {
                    isWaitingForAuthToken = false;
                    Connect();
                    yield break;
                }

                elapsed += Time.deltaTime;
                yield return null;
            }

            isWaitingForAuthToken = false;
            Debug.LogWarning("[SocketIO] Cannot connect without authentication.");
            OnError?.Invoke("LIVE SIGNAL UNAVAILABLE: Sign in required.");
        }

        public void Emit(string eventName, object data)
        {
            if (!isConnected || ws == null)
            {
                return;
            }

            string payload = $"42[\"{eventName}\",{JsonConvert.SerializeObject(data)}]";
            Debug.Log($"[SocketIO] Emitting: {payload}");
            ws.SendText(payload);
        }

        public void JoinRoom(string roomName)
        {
            if (string.IsNullOrWhiteSpace(roomName)) return;
            Emit("join_room", new { room = roomName });
        }

        public void Subscribe(string roomName)
        {
            if (string.IsNullOrWhiteSpace(roomName)) return;
            subscribedRooms.Add(roomName);

            if (isConnected)
            {
                JoinRoom(roomName);
            }
        }

        public void Unsubscribe(string roomName)
        {
            if (string.IsNullOrWhiteSpace(roomName)) return;
            subscribedRooms.Remove(roomName);

            if (isConnected)
            {
                Emit("leave_room", new { room = roomName });
            }
        }

        private void Update()
        {
            DrainMainThreadQueue();

            if (isConnected && (Time.time - lastEnginePingAt) > enginePingTimeoutSeconds)
            {
                Debug.LogWarning("[SocketIO] Ping timeout. Disconnecting.");
                DisconnectInternal(clearSubscriptions: false, manual: false);
                ScheduleReconnect("ping_timeout");
                OnError?.Invoke("LIVE SIGNAL UNAVAILABLE: Connection timed out.");
            }
        }

        private void HandleTransportTextAnyThread(string text)
        {
            EnqueueMainThread(() => HandleTransportPacket(text));
        }

        private void HandleTransportErrorAnyThread(string error)
        {
            EnqueueMainThread(() =>
            {
                Debug.LogWarning($"[SocketIO] Transport error: {error}");
                if (!isConnected)
                {
                    isConnecting = false;
                }
                DispatchConnectionError(error);
                OnError?.Invoke($"LIVE SIGNAL ERROR: {error}");

                if (!isConnected && ws == null)
                {
                    ScheduleReconnect("transport_error_preconnect");
                }
            });
        }

        private void HandleTransportClosedAnyThread(string reason)
        {
            EnqueueMainThread(() =>
            {
                bool wasActive = isConnected || isConnecting;
                bool wasManual = manualDisconnectRequested;
                isConnected = false;
                isConnecting = false;
                lastConnectedAccessToken = string.Empty;

                if (ws != null)
                {
                    ws.OnTextMessage -= HandleTransportTextAnyThread;
                    ws.OnTransportError -= HandleTransportErrorAnyThread;
                    ws.OnTransportClosed -= HandleTransportClosedAnyThread;
                    ws = null;
                }

                if (wasActive)
                {
                    Debug.Log($"[SocketIO] Transport closed: {reason}");
                    DispatchConnectionStatus(false, reason);
                    RaiseDisconnectedOnce();
                    if (!wasManual)
                    {
                        ScheduleReconnect(reason);
                    }
                }
            });
        }

        private void HandleTransportPacket(string packet)
        {
            if (string.IsNullOrEmpty(packet))
            {
                return;
            }

            // Engine.IO + Socket.IO packets over WebSocket transport
            // Examples:
            // 0{...}          -> Engine.IO open
            // 2               -> Engine.IO ping
            // 3               -> Engine.IO pong
            // 40{...}         -> Socket.IO connect
            // 42["event",{}]  -> Socket.IO event
            // 44{...}         -> Socket.IO connect error

            if (packet.StartsWith("0", StringComparison.Ordinal))
            {
                HandleEngineOpen(packet);
                return;
            }

            if (packet == "2")
            {
                lastEnginePingAt = Time.time;
                // Engine.IO pong
                ws?.SendText("3");
                return;
            }

            if (packet == "3")
            {
                // Pong ack (if we ever send pings manually)
                return;
            }

            if (packet.StartsWith("40", StringComparison.Ordinal))
            {
                HandleSocketConnected(packet);
                return;
            }

            if (packet.StartsWith("41", StringComparison.Ordinal))
            {
                Debug.Log("[SocketIO] Namespace disconnected by server.");
                Disconnect();
                return;
            }

            if (packet.StartsWith("42", StringComparison.Ordinal))
            {
                HandleSocketEventPacket(packet);
                return;
            }

            if (packet.StartsWith("44", StringComparison.Ordinal))
            {
                string errorPayload = packet.Length > 2 ? packet.Substring(2) : "Socket.IO connect error";
                Debug.LogWarning($"[SocketIO] Connect error packet: {errorPayload}");
                FailConnection($"LIVE SIGNAL UNAVAILABLE: {errorPayload}");
                return;
            }

            if (packet.StartsWith("4", StringComparison.Ordinal))
            {
                // Other Socket.IO packets not yet handled.
                Debug.Log($"[SocketIO] Unhandled packet: {packet}");
                return;
            }

            Debug.Log($"[SocketIO] Engine.IO packet: {packet}");
        }

        private void HandleEngineOpen(string packet)
        {
            lastEnginePingAt = Time.time;

            try
            {
                if (packet.Length > 1)
                {
                    JObject openPayload = JObject.Parse(packet.Substring(1));
                    if (openPayload["pingTimeout"] != null)
                    {
                        // Keep a little buffer over backend pingTimeout.
                        enginePingTimeoutSeconds = Mathf.Max(10f, (openPayload["pingTimeout"]!.Value<float>() / 1000f) + 5f);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SocketIO] Failed to parse open payload: {ex.Message}");
            }

            // Complete Socket.IO namespace connect. Also include auth token payload for backend handshake.auth.token support.
            string token = APIClient.Instance != null ? APIClient.Instance.GetAccessToken() : null;
            if (!string.IsNullOrWhiteSpace(token))
            {
                ws?.SendText($"40{{\"token\":\"{EscapeJsonString(token)}\"}}");
            }
            else
            {
                ws?.SendText("40");
            }
        }

        private void HandleSocketConnected(string packet)
        {
            if (isConnected)
            {
                return;
            }

            isConnecting = false;
            isConnected = true;
            manualDisconnectRequested = false;
            reconnectAttemptCount = 0;
            disconnectEventRaised = false;
            lastEnginePingAt = Time.time;
            lastConnectedAccessToken = APIClient.Instance != null ? (APIClient.Instance.GetAccessToken() ?? string.Empty) : string.Empty;
            CancelReconnect();

            Debug.Log("[SocketIO] Handshake initiated...");
            OnConnected?.Invoke();
            DispatchConnectionStatus(true, null);
            UIManager.Instance?.PulseLiveSignal();

            // Default subscriptions aligned with backend websocket.ts (room joins are flushed once below).
            subscribedRooms.Add("game");

            string userRoom = AuthManager.Instance?.CurrentUser?.Id;
            if (string.IsNullOrWhiteSpace(userRoom))
            {
                userRoom = APIClient.Instance?.UserId;
            }
            if (!string.IsNullOrWhiteSpace(userRoom))
            {
                subscribedRooms.Add(userRoom);
            }

            FlushRoomSubscriptions();
        }

        private void FlushRoomSubscriptions()
        {
            foreach (string room in subscribedRooms)
            {
                if (!string.IsNullOrWhiteSpace(room))
                {
                    JoinRoom(room);
                }
            }
        }

        private void HandleSocketEventPacket(string packet)
        {
            try
            {
                string payload = packet.Substring(2);
                if (string.IsNullOrWhiteSpace(payload))
                {
                    return;
                }

                JArray arr = JArray.Parse(payload);
                if (arr.Count == 0)
                {
                    return;
                }

                string eventName = arr[0]?.ToString();
                if (string.IsNullOrWhiteSpace(eventName))
                {
                    return;
                }

                string eventPayload = arr.Count > 1 && arr[1] != null
                    ? arr[1].ToString(Formatting.None)
                    : "{}";
                DispatchEvent(eventName, NormalizeRealtimePayloadJson(eventPayload));
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SocketIO] Failed to parse event packet '{packet}': {ex.Message}");
            }
        }

        private void DispatchEvent(string eventName, string data)
        {
            OnEventReceived?.Invoke(eventName, data);
            DispatchDerivedEventAlias(eventName, data);
            UIManager.Instance?.PulseLiveSignal();
        }

        private void DispatchDerivedEventAlias(string eventName, string data)
        {
            if (string.IsNullOrWhiteSpace(eventName) || string.IsNullOrWhiteSpace(data))
            {
                return;
            }

            // Align with backend websocket.ts + admin websocket service behavior:
            // - game_event and room_event often carry a payload.type describing the real event.
            if (!eventName.Equals("game_event", StringComparison.Ordinal) &&
                !eventName.Equals("room_event", StringComparison.Ordinal) &&
                !eventName.Equals("game_update", StringComparison.Ordinal))
            {
                return;
            }

            try
            {
                var token = JToken.Parse(data);
                string alias = token["type"]?.ToString();
                if (string.IsNullOrWhiteSpace(alias) ||
                    alias.Equals(eventName, StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                OnEventReceived?.Invoke(alias, data);
            }
            catch
            {
                // Best-effort aliasing only.
            }
        }

        private void DispatchConnectionStatus(bool connected, string reason)
        {
            var payload = new JObject
            {
                ["connected"] = connected
            };

            if (!string.IsNullOrWhiteSpace(reason))
            {
                payload["reason"] = reason;
            }

            OnEventReceived?.Invoke("connection_status", payload.ToString(Formatting.None));
        }

        private void DispatchConnectionError(string error)
        {
            var payload = new JObject
            {
                ["error"] = string.IsNullOrWhiteSpace(error) ? "socket_error" : error
            };
            OnEventReceived?.Invoke("connection_error", payload.ToString(Formatting.None));
        }

        private string ResolveSocketUrl()
        {
            if (!deriveSocketUrlFromApiBase || APIClient.Instance == null || string.IsNullOrWhiteSpace(APIClient.Instance.BaseUrl))
            {
                return socketUrl;
            }

            try
            {
                var apiUri = new Uri(APIClient.Instance.BaseUrl);
                string scheme = apiUri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase) ? "wss" : "ws";
                int port = apiUri.IsDefaultPort ? -1 : apiUri.Port;
                string authority = port > 0 ? $"{apiUri.Host}:{port}" : apiUri.Host;
                return $"{scheme}://{authority}/socket.io/?EIO=4&transport=websocket";
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SocketIO] Failed to derive socket URL from API base. Using configured socketUrl. Error: {ex.Message}");
                return socketUrl;
            }
        }

        private void FailConnection(string message)
        {
            Debug.LogWarning(message);
            isConnecting = false;
            isConnected = false;
            OnError?.Invoke(message);
            ScheduleReconnect("connect_failed");
        }

        private void RaiseDisconnectedOnce()
        {
            if (disconnectEventRaised)
            {
                return;
            }

            disconnectEventRaised = true;
            OnDisconnected?.Invoke();
        }

        private void HandleTokensUpdated()
        {
            if (isShuttingDown || APIClient.Instance == null || !APIClient.Instance.IsAuthenticated)
            {
                return;
            }

            string currentToken = APIClient.Instance.GetAccessToken() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(currentToken))
            {
                return;
            }

            // If connected with an older token, reconnect so backend handshake auth stays current.
            if (isConnected && !string.IsNullOrWhiteSpace(lastConnectedAccessToken) &&
                !string.Equals(lastConnectedAccessToken, currentToken, StringComparison.Ordinal))
            {
                Debug.Log("[SocketIO] Access token updated. Reconnecting realtime transport.");
                StartCoroutine(ReconnectAfterTokenUpdate());
                return;
            }

            if (autoConnect && !isConnected && !isConnecting && ws == null)
            {
                Connect();
            }
        }

        private IEnumerator ReconnectAfterTokenUpdate()
        {
            // Prevent auto-reconnect loop while we intentionally cycle the transport.
            DisconnectInternal(clearSubscriptions: false, manual: true);
            yield return null;

            if (!isShuttingDown && APIClient.Instance != null && APIClient.Instance.IsAuthenticated)
            {
                Connect();
            }
        }

        private void ScheduleReconnect(string reason)
        {
            if (isShuttingDown || manualDisconnectRequested || !autoReconnect)
            {
                return;
            }

            if (APIClient.Instance == null || !APIClient.Instance.IsAuthenticated)
            {
                return;
            }

            if (reconnectMaxAttempts > 0 && reconnectAttemptCount >= reconnectMaxAttempts)
            {
                Debug.LogWarning($"[SocketIO] Reconnect limit reached ({reconnectMaxAttempts}). Last reason: {reason}");
                return;
            }

            if (reconnectCoroutine != null || isConnected || isConnecting || ws != null)
            {
                return;
            }

            float delay = Mathf.Min(reconnectMaxDelaySeconds, reconnectInitialDelaySeconds * Mathf.Pow(2f, reconnectAttemptCount));
            reconnectAttemptCount++;
            reconnectCoroutine = StartCoroutine(ReconnectAfterDelay(delay, reason, reconnectAttemptCount));
        }

        private IEnumerator ReconnectAfterDelay(float delay, string reason, int attempt)
        {
            Debug.Log($"[SocketIO] Reconnect scheduled in {delay:0.0}s (attempt {attempt}) after: {reason}");
            yield return new WaitForSeconds(delay);
            reconnectCoroutine = null;

            if (isShuttingDown || manualDisconnectRequested || APIClient.Instance == null || !APIClient.Instance.IsAuthenticated)
            {
                yield break;
            }

            Connect();
        }

        private void CancelReconnect()
        {
            if (reconnectCoroutine != null)
            {
                StopCoroutine(reconnectCoroutine);
                reconnectCoroutine = null;
            }
        }

        private void EnqueueMainThread(Action action)
        {
            if (action == null) return;
            lock (queueLock)
            {
                mainThreadQueue.Enqueue(action);
            }
        }

        private void DrainMainThreadQueue()
        {
            while (true)
            {
                Action action = null;
                lock (queueLock)
                {
                    if (mainThreadQueue.Count > 0)
                    {
                        action = mainThreadQueue.Dequeue();
                    }
                }

                if (action == null)
                {
                    break;
                }

                try
                {
                    action.Invoke();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[SocketIO] Main-thread dispatch error: {ex.Message}");
                }
            }
        }

        private static string EscapeJsonString(string value)
        {
            return (value ?? string.Empty).Replace("\\", "\\\\").Replace("\"", "\\\"");
        }

        private string NormalizeRealtimePayloadJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return "{}";
            }

            try
            {
                JToken token = JToken.Parse(json);
                token = NormalizeJsonIds(token);
                return token.ToString(Formatting.None);
            }
            catch
            {
                return json;
            }
        }

        private JToken NormalizeJsonIds(JToken token)
        {
            if (token == null)
            {
                return token;
            }

            if (token.Type == JTokenType.Object)
            {
                JObject obj = (JObject)token;

                if (obj.Count == 1 && obj.TryGetValue("$oid", out JToken oidToken) && oidToken.Type == JTokenType.String)
                {
                    return new JValue(oidToken.Value<string>());
                }

                foreach (var prop in new List<JProperty>(obj.Properties()))
                {
                    prop.Value = NormalizeJsonIds(prop.Value);
                }

                if (obj["id"] == null && obj["_id"] != null)
                {
                    obj["id"] = obj["_id"]!.DeepClone();
                }

                return obj;
            }

            if (token.Type == JTokenType.Array)
            {
                JArray array = (JArray)token;
                for (int i = 0; i < array.Count; i++)
                {
                    array[i] = NormalizeJsonIds(array[i]);
                }
                return array;
            }

            return token;
        }

        private void OnDestroy()
        {
            if (APIClient.Instance != null)
            {
                APIClient.Instance.OnLoggedOut -= HandleLoggedOut;
                APIClient.Instance.OnTokensUpdated -= HandleTokensUpdated;
            }

            if (Instance == this)
            {
                isShuttingDown = true;
                DisconnectInternal(clearSubscriptions: true, manual: true);
                isWaitingForAuthToken = false;
                subscribedRooms.Clear();
                Instance = null;
            }
        }

        private void HandleLoggedOut()
        {
            DisconnectInternal(clearSubscriptions: true, manual: true);
        }
    }

    // Kept for compatibility with previous code references (typo retained intentionally).
    internal class WebSockerWrapper
    {
        public event Action<string> OnTextMessage;
        public event Action<string> OnTransportError;
        public event Action<string> OnTransportClosed;

#if !UNITY_WEBGL
        private ClientWebSocket client;
        private CancellationTokenSource cts;
        private readonly byte[] receiveBuffer = new byte[8192];

        public bool IsOpen => client != null && client.State == WebSocketState.Open;

        public void Connect(string url, string bearerToken, Action onConnected, Action<Exception> onFailed)
        {
            _ = ConnectInternalAsync(url, bearerToken, onConnected, onFailed);
        }

        public void SendText(string message)
        {
            if (!IsOpen || string.IsNullOrEmpty(message))
            {
                return;
            }

            _ = SendTextInternalAsync(message);
        }

        public void Close()
        {
            _ = CloseInternalAsync();
        }

        private async Task ConnectInternalAsync(string url, string bearerToken, Action onConnected, Action<Exception> onFailed)
        {
            try
            {
                client = new ClientWebSocket();
                client.Options.KeepAliveInterval = TimeSpan.FromSeconds(20);

                if (!string.IsNullOrWhiteSpace(bearerToken))
                {
                    client.Options.SetRequestHeader("Authorization", $"Bearer {bearerToken}");
                }

                cts = new CancellationTokenSource();
                await client.ConnectAsync(new Uri(url), cts.Token);
                onConnected?.Invoke();

                _ = ReceiveLoopAsync(cts.Token);
            }
            catch (Exception ex)
            {
                onFailed?.Invoke(ex);
            }
        }

        private async Task SendTextInternalAsync(string message)
        {
            try
            {
                if (!IsOpen) return;
                byte[] bytes = Encoding.UTF8.GetBytes(message);
                await client.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, cts?.Token ?? CancellationToken.None);
            }
            catch (Exception ex)
            {
                OnTransportError?.Invoke(ex.Message);
            }
        }

        private async Task ReceiveLoopAsync(CancellationToken token)
        {
            var frameBuilder = new StringBuilder();

            try
            {
                while (!token.IsCancellationRequested && client != null)
                {
                    var segment = new ArraySegment<byte>(receiveBuffer);
                    WebSocketReceiveResult result = await client.ReceiveAsync(segment, token);

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        string reason = result.CloseStatusDescription ?? result.CloseStatus?.ToString() ?? "closed";
                        OnTransportClosed?.Invoke(reason);
                        return;
                    }

                    if (result.Count > 0)
                    {
                        frameBuilder.Append(Encoding.UTF8.GetString(receiveBuffer, 0, result.Count));
                    }

                    if (result.EndOfMessage)
                    {
                        string text = frameBuilder.ToString();
                        frameBuilder.Length = 0;

                        if (!string.IsNullOrEmpty(text))
                        {
                            OnTextMessage?.Invoke(text);
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Expected on shutdown.
            }
            catch (Exception ex)
            {
                OnTransportError?.Invoke(ex.Message);
            }
            finally
            {
                if (client != null && client.State != WebSocketState.Closed && client.State != WebSocketState.Aborted)
                {
                    try
                    {
                        await client.CloseAsync(WebSocketCloseStatus.NormalClosure, "closing", CancellationToken.None);
                    }
                    catch
                    {
                        // Ignore best-effort close errors.
                    }
                }

                OnTransportClosed?.Invoke("receive_loop_ended");
            }
        }

        private async Task CloseInternalAsync()
        {
            try
            {
                cts?.Cancel();

                if (client != null &&
                    (client.State == WebSocketState.Open || client.State == WebSocketState.CloseReceived))
                {
                    await client.CloseAsync(WebSocketCloseStatus.NormalClosure, "client_disconnect", CancellationToken.None);
                }
            }
            catch
            {
                // Ignore close exceptions during teardown.
            }
            finally
            {
                try { client?.Dispose(); } catch { /* ignore */ }
                client = null;
                try { cts?.Dispose(); } catch { /* ignore */ }
                cts = null;
            }
        }
#else
        public bool IsOpen => false;

        public void Connect(string url, string bearerToken, Action onConnected, Action<Exception> onFailed)
        {
            onFailed?.Invoke(new NotSupportedException("Socket.IO WebSocket transport is not supported on WebGL in this lightweight client. Use a platform plugin/client implementation."));
        }

        public void SendText(string message) { }
        public void Close() { }
#endif
    }
}
