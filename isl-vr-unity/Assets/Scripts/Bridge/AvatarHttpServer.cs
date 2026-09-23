using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using UnityEngine;
using System.IO;
using SignLoop.Avatar;

namespace SignLoop.Bridge {
    public class AvatarHttpServer : MonoBehaviour {
        private TcpListener listener;
        private ISLSignPlayer signPlayer;
        private string lastMessage = "";
        private bool hasNewMessage = false;

        void Start() {
            signPlayer = FindObjectOfType<ISLSignPlayer>();
            if (signPlayer == null) {
                Debug.LogError("No ISLSignPlayer found in scene!");
                return;
            }
            try {
                listener = new TcpListener(IPAddress.Any, 8080);
                listener.Start();
                Task.Run(ListenLoop);
                Debug.Log("<color=green>[VR Bridge] TCP Server listening for React on port 8080</color>");
            } catch (Exception e) {
                Debug.LogError("Failed to start TCP server. Port 8080 might be in use! " + e.Message);
            }
        }

        async Task ListenLoop() {
            while (listener != null) {
                try {
                    var client = await listener.AcceptTcpClientAsync();
                    _ = Task.Run(() => HandleClient(client));
                } catch { }
            }
        }

        void HandleClient(TcpClient client) {
            try {
                using (var stream = client.GetStream())
                using (var reader = new StreamReader(stream))
                using (var writer = new StreamWriter(stream)) {
                    string request = reader.ReadLine();
                    if (string.IsNullOrEmpty(request)) return;

                    int contentLength = 0;
                    string line;
                    while (!string.IsNullOrEmpty(line = reader.ReadLine())) {
                        if (line.StartsWith("Content-Length:")) {
                            int.TryParse(line.Substring(15).Trim(), out contentLength);
                        }
                    }

                    if (request.StartsWith("OPTIONS")) {
                        WriteCORSResponse(writer);
                        return;
                    }

                    if (request.StartsWith("POST") && contentLength > 0) {
                        char[] buffer = new char[contentLength];
                        reader.Read(buffer, 0, contentLength);
                        lastMessage = new string(buffer).Trim();
                        hasNewMessage = true;
                        WriteCORSResponse(writer);
                    }
                }
            } catch { }
            finally { client.Close(); }
        }

        void WriteCORSResponse(StreamWriter writer) {
            writer.Write("HTTP/1.1 200 OK\r\n");
            writer.Write("Access-Control-Allow-Origin: *\r\n");
            writer.Write("Access-Control-Allow-Methods: POST, OPTIONS\r\n");
            writer.Write("Access-Control-Allow-Headers: Content-Type\r\n");
            writer.Write("Content-Length: 2\r\n\r\nOK");
            writer.Flush();
        }

        void Update() {
            if (hasNewMessage && signPlayer != null) {
                hasNewMessage = false;
                Debug.Log($"<color=cyan>[VR Bridge] Received command from React: {lastMessage}</color>");
                signPlayer.PlaySentence(lastMessage);
            }
        }

        void OnDestroy() {
            if (listener != null) {
                listener.Stop();
                listener = null;
            }
        }
    }
}
