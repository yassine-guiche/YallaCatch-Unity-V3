using System;
using Newtonsoft.Json;
using UnityEngine;
using YallaCatch.Networking;

namespace YallaCatch.Managers
{
    /// <summary>
    /// Legacy compatibility shim.
    /// Native WebSocket implementation was removed in favor of Socket.IO.
    /// Existing scenes/scripts that still reference WebSocketManager will forward to SocketIOClient.
    /// </summary>
    [Obsolete("WebSocketManager is deprecated. Use YallaCatch.Networking.SocketIOClient instead.")]
    public class WebSocketManager : MonoBehaviour
    {
        public static WebSocketManager Instance { get; private set; }

        public event Action OnConnected;
        public event Action OnDisconnected;
        public event Action<string, object> OnEventReceived;
        public event Action<string> OnError;

        private bool bridgeHooksRegistered;

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
            Debug.Log("[WebSocketManager] Deprecated compatibility shim active. Forwarding to SocketIOClient.");
            HookSocketIOBridge();
        }

        private void OnDestroy()
        {
            UnhookSocketIOBridge();
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void Connect()
        {
            HookSocketIOBridge();
            SocketIOClient.Instance?.Connect();
        }

        public void Disconnect()
        {
            SocketIOClient.Instance?.Disconnect();
        }

        public void Subscribe(string room)
        {
            SocketIOClient.Instance?.Subscribe(room);
        }

        public void Unsubscribe(string room)
        {
            SocketIOClient.Instance?.Unsubscribe(room);
        }

        private void HookSocketIOBridge()
        {
            if (bridgeHooksRegistered || SocketIOClient.Instance == null)
            {
                return;
            }

            SocketIOClient.Instance.OnConnected += ForwardConnected;
            SocketIOClient.Instance.OnDisconnected += ForwardDisconnected;
            SocketIOClient.Instance.OnError += ForwardError;
            SocketIOClient.Instance.OnEventReceived += ForwardEvent;
            bridgeHooksRegistered = true;
        }

        private void UnhookSocketIOBridge()
        {
            if (!bridgeHooksRegistered || SocketIOClient.Instance == null)
            {
                return;
            }

            SocketIOClient.Instance.OnConnected -= ForwardConnected;
            SocketIOClient.Instance.OnDisconnected -= ForwardDisconnected;
            SocketIOClient.Instance.OnError -= ForwardError;
            SocketIOClient.Instance.OnEventReceived -= ForwardEvent;
            bridgeHooksRegistered = false;
        }

        private void ForwardConnected()
        {
            OnConnected?.Invoke();
        }

        private void ForwardDisconnected()
        {
            OnDisconnected?.Invoke();
        }

        private void ForwardError(string error)
        {
            OnError?.Invoke(error);
        }

        private void ForwardEvent(string eventName, string data)
        {
            object payload = data;
            if (!string.IsNullOrWhiteSpace(data))
            {
                try
                {
                    payload = JsonConvert.DeserializeObject<object>(data);
                }
                catch
                {
                    payload = data;
                }
            }

            OnEventReceived?.Invoke(eventName, payload);
        }
    }
}
