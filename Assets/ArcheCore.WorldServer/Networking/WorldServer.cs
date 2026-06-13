using ArcheCore.Client.Networking.W2C;
using ArcheCore.WorldServer.Managers;
using ArcheCore.WorldServer.Networking;
using ArcheCore.WorldServer.Networking.C2W;
using ArcheCore.WorldServer.ServerConfig;
using LiteNetLib;
using Shared;


namespace ArcheCore.WorldServer
{
    public class WorldServer : INetEventListener
    {
        public NetManager Server { get; private set; }

        private PacketDispatcher packetDispatcher;
        private PlayerManager playerManager;
        private readonly SpawnManager spawnManager = new();

        private const string ConnectionKey = "MMO";

        public void Start()
        {
            spawnManager.SpawnInitialCubes();
            playerManager = new PlayerManager(spawnManager);

            packetDispatcher = new PacketDispatcher();

            RegisterPackets();

            Server = new NetManager(this);
            Server.Start(ConfigService.Config.Port);

            WorldLogger.Info(
                $"Server started on port {ConfigService.Config.Port}");
        }

        private void RegisterPackets()
        {
            packetDispatcher.Register(
                Opcode.Authenticate,
                new C2WAuthenticateHandler(playerManager));

            packetDispatcher.Register(
                Opcode.PlayerMove,
                new C2WMovementHandler(playerManager));
        }

        public void Update()
        {
            // Drain any actions queued by async handlers
            // (e.g. authentication callbacks)
            playerManager.DrainActions();

            Server?.PollEvents();
        }

        public void Stop()
        {
            Server?.Stop();
        }

        public void OnPeerConnected(NetPeer peer)
        {
            WorldLogger.Info(
                $"Client connected: {peer.Address}");

            // Waiting for Authenticate packet
        }

        public void OnPeerDisconnected(
            NetPeer peer,
            DisconnectInfo info)
        {
            playerManager.HandlePlayerDisconnected(peer);
        }

        public void OnConnectionRequest(
            ConnectionRequest request)
        {
            request.AcceptIfKey(ConnectionKey);
        }

        public void OnNetworkReceive(
            NetPeer peer,
            NetPacketReader reader,
            byte channel,
            DeliveryMethod delivery)
        {
            Opcode packet =
                (Opcode)reader.GetUShort();

            packetDispatcher.Handle(
                packet,
                peer,
                reader);

            reader.Recycle();
        }

        public void OnNetworkError(
            System.Net.IPEndPoint endPoint,
            System.Net.Sockets.SocketError error)
        {
            WorldLogger.Warning(
                $"Network Error: {error}");
        }

        public void OnNetworkLatencyUpdate(
            NetPeer peer,
            int latency)
        {
        }

        public void OnNetworkReceiveUnconnected(
            System.Net.IPEndPoint endPoint,
            NetPacketReader reader,
            UnconnectedMessageType messageType)
        {
        }
    }
}