using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FTServer;
using FTServer.Example;
using SlitherEvo;
using FTServer.Operator;
using ConsoleApp1.CallbackHandler;
using FTServer.Projects.Slither.Packet;
using GameServer;
using GameServer.Package;
using GamingRoom.Gaming.Packet;
using GamingRoom.Gaming.Room.GameLogic.DropSystem;
using GamingRoom.Gaming.Room.GameLogic.GameEventSystem;
using SlitherEvo.Synchronize;
using SlitherGame.Controller;
using GameServer.Packet;

namespace ConsoleApp1
{
    public class GamerEntity : IDisposable, IGamerEntity, IFireReceiver, IPlayerInput, IPlayerOutput
    {   
        private static readonly TaskAgent NetworkService = new TaskAgent();
        private static readonly TaskAgent Logic = new TaskAgent();
        private bool _IsDispose = false;

        public readonly string _devicedId;
        public Urls urls { get; private set; }
        public string accessToken { get; private set; }
        public Account account { get; private set; }
        public GamerInput input { get; private set; }
        //房間玩家ID名字對應列表
        public Dictionary<int, string> mPlayerNameList { get; private set; }

        private Connect _Connect;
        public _System _systemHandler { get; private set; }
        public GamingHandler _gamingHandler { get; private set; }
        public LobbyHandler _lobbyHandler { get; private set; }
        public ToArenaHandler _toArenaHandler { get; private set; }
        public PacketHandler _gamePacketHandler { get; private set; }
        public PacketHandler _gameRoomHandler { get; private set; }
        public IFireReceiver fireReceiver => this;

        public IBotProxy botProxy { get; private set; }

        private SimWorld World;

        public GamerEntity(string id)
        {
            _devicedId = id;
            input = new GamerInput();
            mPlayerNameList = new Dictionary<int, string>();
            botProxy = new ConsoleBotProxy();
        }

        public GamerEntity(string id, IBotProxy bot)
        {
            _devicedId = id;
            input = new GamerInput();
            mPlayerNameList = new Dictionary<int, string>();
            botProxy = bot;
        }

        ~GamerEntity()
        {
            if (_IsDispose) return;
            Dispose();
            _IsDispose = true;
        }

        public void QueryGameUrl(string agentAddr, string serverVer, string serverType)
        {
            //基本的 Http 初始流程
            var initResult = Init.Http(agentAddr, _devicedId, serverVer, serverType);
            urls = initResult.Item1;
            accessToken = initResult.Item2;
            account = initResult.Item3;
        }

        public void Authenticate(string userAddr)
        {
            accessToken = Init.Authentication(userAddr, _devicedId);
        }

        public void Login(string userAddr, ELoginType loginType = ELoginType.NewAccount)
        {
            switch (loginType)
            {
                case ELoginType.NewAccount:
                    account = Init.CreateAccount(userAddr, accessToken);
                    break;
                case ELoginType.Login:
                    account = Init.AILogin(userAddr, accessToken);
                    break;
            }
        }

        public void Start(string gameUrl)
        {
            //強連線流程
            Uri gamingUrl = new Uri(gameUrl);

            var initGameResult = Init.Rudp(gamingUrl);

            _Connect = initGameResult.Item1;
            _systemHandler = initGameResult.Item2;
            _lobbyHandler = initGameResult.Item3;
            _toArenaHandler = initGameResult.Item4;
            _gamingHandler = initGameResult.Item5;
            _gamePacketHandler = initGameResult.Item6;
            _gameRoomHandler = initGameResult.Item7;
            _systemHandler.ConnectToServer();
            _systemHandler.Connect += ConnectedToServer;
            _lobbyHandler.RecvPacket += ReceiveLobbyPacket;
            _toArenaHandler.SetCallBack(this);
            _gamePacketHandler.Receive = ReceiveGamePacket;

            this.onReceiveEnterArena += ReceiveToArena;
            this.onDeleteArenaPlayers += ReceiveDeletePlayer;

            NetworkService.RegisteredToNetworkTask(_Connect.Service);
            Logic.RegisteredToNetworkTask(Update);
        }

        public void Start()
        {   
            //強連線流程
            Uri gamingUrl = new Uri(urls.GamingUrl);

            var initGameResult = Init.Rudp(gamingUrl);

            _Connect = initGameResult.Item1;
            _systemHandler = initGameResult.Item2;
            _lobbyHandler = initGameResult.Item3;
            _toArenaHandler = initGameResult.Item4;
            _gamingHandler = initGameResult.Item5;
            _gamePacketHandler = initGameResult.Item6;
            _gameRoomHandler = initGameResult.Item7;         
            _systemHandler.ConnectToServer();
            _systemHandler.Connect += ConnectedToServer;
            _lobbyHandler.RecvPacket += ReceiveLobbyPacket;
            _toArenaHandler.SetCallBack(this);
            _gamePacketHandler.Receive = ReceiveGamePacket;

            this.onReceiveEnterArena += ReceiveToArena;
            this.onDeleteArenaPlayers += ReceiveDeletePlayer;

            NetworkService.RegisteredToNetworkTask(_Connect.Service);
            Logic.RegisteredToNetworkTask(Update);
        }

        private void ConnectedToServer()
        {
            GamerFlow.FConnectToServer(this);
        }
        private void ReceiveLobbyPacket(Dictionary<byte, object> packet)
        {
            GamerFlow.FReceiveLobbyPacket(this, packet);
        }
        private void ReceiveToArena(EnterArenaPacket packet)
        {
            GamerFlow.FReceiveToArena(this, packet, 
                (slot) => botProxy.GameStart(account, slot, new BotEvents(this, this)));
        }
        private void ReceiveDeletePlayer(byte[] slots)
        {
            GamerFlow.FDeletePlayer(this, slots);
        }
        private void ReceiveGamePacket(Dictionary<byte, object> obj)
        {
            GamerFlow.FReceiveGamePacket(this, obj,out World);
        }

        private void Update()
        {
            GamerFlow.Update(this, TaskAgent.Delay);
        }        

        public void Dispose()
        {
            Logic.UnRegisteredToNetworkTask(Update);
            NetworkService.UnRegisteredToNetworkTask(_Connect.Service);
            _Connect?.Dispose();
            _IsDispose = true;
        }

        #region 實作 IPlayerInput , IPlayerOutput 介面
        public event Action<EnvironmentPacket> onReceiveEnvironment;
        public void fireReceiveEnvironment(EnvironmentPacket r)
        {
            onReceiveEnvironment?.Invoke(r);
        }

        public event Action<GameEvent[]> onReceiveGameEvent;
        public void fireReceiveGameEvent(GameEvent[] r)
        {
            onReceiveGameEvent?.Invoke(r);
        }

        public event Action<DropItemPacket> onReceiveDropItem;
        public void fireReceiveDropItem(DropItemPacket r)
        {
            onReceiveDropItem?.Invoke(r);
        }

        public event Action<int> onReceiveBonuspot;
        public void fireReceiveBonuspot(int r)
        {
            onReceiveBonuspot?.Invoke(r);
        }

        public event Action<GameResultPacket> onReceiveGameResult;
        public void fireReceiveGameResult(GameResultPacket r)
        {   
            onReceiveGameResult?.Invoke(r);
        }

        public event Action<GamersPacket> onReceiveGamerInfo;
        public void fireReceiveGamerInfo(GamersPacket r)
        {
            onReceiveGamerInfo?.Invoke(r);
        }

        public event Action<object> onReceiveBroadcast;
        public void fireReceiveBroadcast(object r)
        {
            onReceiveBroadcast?.Invoke(r);
        }

        public event Action<string> onReceivePureData;
        public void fireReceivePureData(string r)
        {
            onReceivePureData?.Invoke(r);
        }

        public event Action<object> onReceiveGameStart;
        public void fireReceiveGameStart(object r)
        {
            onReceiveGameStart?.Invoke(r);
        }

        public event Action<Dictionary<string, byte>> onReceiveGamerSlots;
        public void fireReceiveGamerSlots(Dictionary<string, byte> r)
        {
            onReceiveGamerSlots?.Invoke(r);
        }

        public event Action<byte[]> onReceiveRMGamer;
        public void fireReceiveGMGamer(byte[] r)
        {
            onReceiveRMGamer?.Invoke(r);
        }

        public event Action<int> onReceiveCountDown;
        public void fireReceiveCountDown(int r)
        {
            onReceiveCountDown?.Invoke(r);
        }

        public event Action<float> onReceiveGameTime;
        public void fireReceiveGameTime(float r)
        {
            onReceiveGameTime?.Invoke(r);
        }

        public event Action<EnterArenaPacket> onReceiveEnterArena;
        public void fireReceiveEnterArena(EnterArenaPacket r)
        {
            onReceiveEnterArena?.Invoke(r);
        }

        public event Action<byte[]> onDeleteArenaPlayers;
        public void fireDeleteArenaPlayers(byte[] slots)
        {
            onDeleteArenaPlayers?.Invoke(slots);
        }

        public event Action onToArenaStartGame;
        public void fireStartGame()
        {
            onToArenaStartGame?.Invoke();
        }

        public void SendInput(float angle)
        {
            var direction = Direction.GetD32(angle);
            Dictionary<byte, object> packet = new Dictionary<byte, object>()
            {
                {0,ClientGameCode.DIRECTION },
                {1,(byte)direction}
            };
            _gamingHandler?.SendToServer(packet);
        }

        public void SendInput(Skill.Category skill)
        {
            Dictionary<byte, object> packet = new Dictionary<byte, object>()
            {
                {0,ClientGameCode.SKILL },
                {1,(byte)skill}
            };
            _gamingHandler?.SendToServer(packet);
        }

        public void Rebirth(ERebirth type)
        {
            var packet = new Dictionary<byte, object>()
            {
                {0, ClientGameCode.Rebirth },
                {1, (int)type },
            };
            _gamingHandler?.SendToServer(packet);
        }

        public void UseItem()
        {
            Dictionary<byte, object> packet = new Dictionary<byte, object>()
            {
                {0,ClientGameCode.Item }
            };
            _gamingHandler?.SendToServer(packet);
        }

        public void Broadcast(string data)
        {
            var packet = new Dictionary<byte, object>
            {
                {0, ClientGameCode.Broadcast },
                {1, data }
            };
            _gamingHandler?.SendToServer(packet);
        }

        public void Send(string data)
        {
            var packet = new Dictionary<byte, object>
            {
                {0, ClientGameCode.PureData },
                {1, data }
            };
            _gamingHandler?.SendToServer(packet);
        }
        #endregion
    }

    public interface IGamerEntity
    {
        string accessToken { get; }
        Account account { get; }
        GamerInput input { get; }

        _System _systemHandler { get; }
        GamingHandler _gamingHandler { get; }
        LobbyHandler _lobbyHandler { get; }
        ToArenaHandler _toArenaHandler { get; }
        PacketHandler _gamePacketHandler { get; }
        PacketHandler _gameRoomHandler { get; }
        IFireReceiver fireReceiver { get; }

        IBotProxy botProxy { get; }

        Dictionary<int, string> mPlayerNameList { get; }

        void QueryGameUrl(string agentAddr, string serverVer, string serverType);
        void Authenticate(string userAddr);
        void Login(string userAddr, ELoginType loginType);
        void Start(string gameUrl);
        void Start();
    }

    public interface IFireReceiver
    {
        void fireReceiveEnvironment(EnvironmentPacket r);
        void fireReceiveGameEvent(GameEvent[] r);
        void fireReceiveDropItem(DropItemPacket r);
        void fireReceiveBonuspot(int r);
        void fireReceiveGameResult(GameResultPacket r);
        void fireReceiveGamerInfo(GamersPacket r);
        void fireReceiveBroadcast(object r);
        void fireReceivePureData(string r);
        void fireReceiveGameStart(object r);
        void fireReceiveGamerSlots(Dictionary<string, byte> r);
        void fireReceiveGMGamer(byte[] r);
        void fireReceiveCountDown(int r);
        void fireReceiveGameTime(float r);
        void fireReceiveEnterArena(EnterArenaPacket r);
        void fireDeleteArenaPlayers(byte[] slots);
        void fireStartGame();

    }

}