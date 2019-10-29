using System;
using System.Collections.Generic;
using GameServer;
using FTServer.Projects.Slither.Packet;
using GameServer.Package;
using GamingRoom.Gaming.Packet;
using GamingRoom.Gaming.Room.GameLogic.DropSystem;
using GamingRoom.Gaming.Room.GameLogic.GameEventSystem;
using GamingServer.Gaming.Packet;
using SlitherEvo;
using GameServer.Packet;
using FTServer.Example;
using ConsoleApp1.CallbackHandler;

namespace ConsoleApp1
{
    public class GamerFlow
    {
        public static void Update(IGamerEntity gamer, float deltaTime)
        {
            gamer.input.CheckStayTime();
            if (gamer.input.IsError)
            {
                Console.WriteLine("Gamer 在單一狀態停留超過最大時間，判斷為是有問題的 AI");
                //gamer.Dispose();
                throw new InvalidOperationException("AI input is error.");
                //return;
            }          

            if (gamer.input.TrySetNextLevel(GamerInput.Level.ConnectedNotRegister))
            {
                Console.WriteLine("ConnectedNotRegister");
                gamer._lobbyHandler.SendToServer(EClientLobbyCode.Register, gamer.account.Info.Name);
            }
            if (gamer.input.TrySetNextLevel(GamerInput.Level.WaitingJoinRoom))
            {
                Console.WriteLine("WaitingJoinRoom");
                if (int.TryParse(Environment.GetEnvironmentVariable("RoomID"), out int roomId) && roomId >= 0)
                {
                    JoinRoom(gamer, roomId);
                }
                else
                {
                    QuickJoin(gamer);
                }
            }
            if (gamer.input.TrySetNextLevel(GamerInput.Level.WaitEnterGaming))
            {
                Console.WriteLine("WaitEnterGaming");
                if (!gamer.input.LobbyReady)
                {
                    gamer.input.LobbyReady = true;
                    gamer._lobbyHandler.SendToServer(EClientLobbyCode.PlayerReady, gamer.input.LobbyReady);
                }
            }

            if (gamer.input.Now == GamerInput.Level.Gaming)
            {   
                if (gamer.input.IsOverGame)
                {
                    Console.WriteLine("Game Over.");
                    // close the application
                    Environment.Exit(0);
                    //gamer.input.ResetToLobby();
                }
                else
                {
                    gamer.botProxy.GameUpdate(TimeSpan.FromMilliseconds(deltaTime)/*TimeSpan.FromMilliseconds(TaskAgent.Delay)*/);
                }
            }
        }

        private static void JoinRoom(IGamerEntity gamer, int roomId)
        {
            Console.WriteLine($"Join Room[{roomId}].");
            var input = new JoinRoomInput
            {
                RoomID = roomId,
                Token = gamer.accessToken,
                Name = gamer.account.Info.Name,
                SkinID = gamer.account.Snake.Skin.EquipID
            };
            gamer._lobbyHandler.SendToServer(EClientLobbyCode.JoinRoom, input);
        }

        private static void QuickJoin(IGamerEntity gamer)
        {
            Console.WriteLine("Quick join Room.");
            Console.WriteLine(gamer.account.Info.Name);
            var input = new QuickJoinInput
            {
                Price = 10,
                RoomType = (byte)EJoinRoomType.Standard,
                Token = gamer.accessToken,
                Name = gamer.account.Info.Name,
                SkinID = gamer.account.Snake.Skin.EquipID
            };
            gamer._lobbyHandler.SendToServer(EClientLobbyCode.QuickJoin, input);
        }

        public static void FConnectToServer(IGamerEntity gamer)
        {
            gamer.input.TrySetNextLevel(GamerInput.Level.NotConnect);
        }

        public static void FReceiveLobbyPacket(IGamerEntity gamer, Dictionary<byte, object> packet)
        {
            if (!Enum.TryParse(packet[0].ToString(), out EServerLobbyCode code))
            {
                LogProxy.WriteError($"Parse ServerLobbyCode fail, Value:{packet[0]}");
                return;
            }

            switch (code)
            {
                case EServerLobbyCode.Rooms:
                    onGetRoomList();
                    break;
                case EServerLobbyCode.AllPeers:
                    onGetAllRoommates();
                    break;
                case EServerLobbyCode.RoomReady:
                    onRoommateReady();
                    break;
            }

            void onGetRoomList()
            {
                var webRoomPacket = (RoomPacket[])packet[1];
                int waitingRoomCount = int.Parse(packet[2].ToString());
                int battleRoomCount = int.Parse(packet[3].ToString());

                gamer.input.SetLevel();
            }
            void onGetAllRoommates()
            {
                var playerPackets = (PlayerPacket[])packet[1];
                int comeDown = int.Parse(packet[2].ToString());
                var roomID = packet[3].ToString();
                gamer.mPlayerNameList.Clear();

                for (int i = 0; i < playerPackets.Length; i++)
                {
                    if (playerPackets[i].SlotID == -1)
                        continue;
                    gamer.mPlayerNameList.Add(playerPackets[i].SlotID, playerPackets[i].Name);
                    if (playerPackets[i].Name == gamer.account.Info.Name)
                    {
                        gamer.input.SlotID = playerPackets[i].SlotID;
                    }
                }
                gamer.input.TrySetNextLevel(GamerInput.Level.WaitingPeers);
            }
            void onRoommateReady()
            {
                //當 有玩家按 Ready 時觸發
            }
        }

        public static void FReceiveToArena(IGamerEntity gamer, EnterArenaPacket packet, Action<byte> onEnterArena)
        {
            //if (gamer.input.Now == GamerInput.Level.WaitEnterGaming)
            //{
            //    gamer.input.SetLevel(GamerInput.Level.WaitSendLoading);
            //    gamer._toArenaHandler.SendLoading(100);
            //    gamer.input.SetLevel(GamerInput.Level.WaitDeletePlayer);
            //}
            //gamer.botProxy.GameStart(gamer.account, (byte)gamer.input.SlotID, new BotEvents(gamer, gamer));
            if (gamer.input.TrySetNextLevel(GamerInput.Level.WaitSendLoading))
            {
                gamer._toArenaHandler.SendLoading(100);
            }
            
            onEnterArena?.Invoke((byte)gamer.input.SlotID);
            //gamer.botProxy.GameStart(gamer.account, (byte)gamer.input.SlotID, new BotEvents(gamer, gamer));
        }

        public static void FDeletePlayer(IGamerEntity gamer, byte[] slots)
        {
            if (gamer.input.TrySetNextLevel(GamerInput.Level.WaitDeletePlayer))
            {
                gamer._toArenaHandler.SendReady();
            }
            //if (gamer.input.Now == GamerInput.Level.WaitDeletePlayer)
            //{
            //    gamer._toArenaHandler.SendReady();
            //    gamer.input.SetLevel(GamerInput.Level.WaitWorldState);
            //}
        }

        public static void FReceiveGamePacket(IGamerEntity gamer, Dictionary<byte,object> packet,out SimWorld world)
        {
            foreach (byte key in packet.Keys)
            {
                if (!Enum.TryParse(key.ToString(), out EServerGameCode code))
                {
                    //LogProxy.WriteError($"Parse EServerGameCode fail, Value:{key}");
                    continue;
                }

                switch (code)
                {
                    //遊戲正式開始
                    case EServerGameCode.GameStart:
                        if (!gamer.input.IsStartGame)
                        {
                            gamer.input.IsStartGame = true;
                            gamer.input.SetLevel(GamerInput.Level.Gaming);
                            //LogProxy.WriteLine($"EServerGameCode.GameStart({gamer.account.Info.Name})");
                        }
                        break;
                    //遊戲訊息
                    case EServerGameCode.GamerInfo:
                        break;
                    //遊戲結果
                    case EServerGameCode.GameResult:
                        Console.WriteLine("Receive game result.");
                        //LogProxy.WriteLine($"EServerGameCode.GameResult({gamer.account.Info.Name})");
                        gamer.input.IsOverGame = true;
                        break;
                }
            }
            
            EnvironmentPacket? PacketEnv = null;
            GameEvent[] PacketGameEvent = null;
            DropItemPacket PacketDropItem = null;
            int? PacketBonuspot = null;
            GameResultPacket PacketGameResult = null;
            GamersPacket PacketGamersInfo = null;
            object PacketBroadcast = null;
            string PacketPureData = null;
            object PacketGameStart = null;
            Dictionary<string, byte> PacketGamerSlots = null;
            byte[] PacketGMGamer = null;
            int? PacketCountDown = null;
            float? PacketGameTime = null;
           
            foreach (var key in packet.Keys)
            {
                if (!Enum.TryParse(key.ToString(), out EServerGameCode code))
                    continue;
                switch (code)
                {
                    case EServerGameCode.Environment:
                        PacketEnv = (EnvironmentPacket)packet[key];
                        break;
                    case EServerGameCode.GameEvent:
                        PacketGameEvent = (GameEvent[])packet[key];
                        break;
                    case EServerGameCode.DropItem:
                        PacketDropItem = (DropItemPacket)packet[key];
                        break;
                    case EServerGameCode.Bonuspot:
                        PacketBonuspot = (int)packet[key];
                        break;
                    case EServerGameCode.GameResult:
                        PacketGameResult = (GameResultPacket)packet[key];
                        break;
                    case EServerGameCode.GamerInfo:
                        PacketGamersInfo = (GamersPacket)packet[key];
                        break;
                    case EServerGameCode.Broadcast:
                        PacketBroadcast = packet[key];
                        break;
                    case EServerGameCode.PureData:
                        PacketPureData = (string)packet[key];
                        break;
                    case EServerGameCode.GameStart:
                        PacketGameStart = packet[key];
                        break;
                    case EServerGameCode.GamerSlots:
                        PacketGamerSlots = (Dictionary<string, byte>)packet[key];
                        break;
                    case EServerGameCode.RMGamer:
                        PacketGMGamer = (byte[])packet[key];
                        break;
                    case EServerGameCode.CountDown:
                        PacketCountDown = (int)packet[key];
                        break;
                    case EServerGameCode.GameTime:
                        PacketGameTime = (float)packet[key];
                        break;
                }
            }

            if (PacketEnv != null)
                gamer.fireReceiver.fireReceiveEnvironment(PacketEnv.Value);
            if (PacketGameEvent != null)
                gamer.fireReceiver.fireReceiveGameEvent(PacketGameEvent);
            if (PacketDropItem != null)
                gamer.fireReceiver.fireReceiveDropItem(PacketDropItem);
            if (PacketBonuspot != null)
                gamer.fireReceiver.fireReceiveBonuspot(PacketBonuspot.Value);
            if (PacketGameResult != null)
                gamer.fireReceiver.fireReceiveGameResult(PacketGameResult);
            if (PacketGamersInfo != null)
                gamer.fireReceiver.fireReceiveGamerInfo (PacketGamersInfo);
            if (PacketBroadcast != null)
                gamer.fireReceiver.fireReceiveBroadcast(PacketBroadcast);
            if (PacketPureData != null)
                gamer.fireReceiver.fireReceivePureData(PacketPureData);
            if (PacketGameStart != null)
                gamer.fireReceiver.fireReceiveGameStart(PacketGameStart);
            if (PacketGamerSlots != null)
                gamer.fireReceiver.fireReceiveGamerSlots(PacketGamerSlots);
            if (PacketGMGamer != null)
                gamer.fireReceiver.fireReceiveGMGamer (PacketGMGamer);
            if (PacketCountDown != null)
                gamer.fireReceiver.fireReceiveCountDown(PacketCountDown.Value);
            if (PacketGameTime != null)
                gamer.fireReceiver.fireReceiveGameTime(PacketGameTime.Value);
            
            world = new SimWorld();
            if (PacketEnv != null)
                world.Environment = PacketEnv.Value;
            if (PacketGameEvent != null)
                world.GameEvent = PacketGameEvent;
            if (PacketDropItem!=null)
                world.DropItem = PacketDropItem;
            if (PacketBonuspot!=null)
                world.Bonuspot = PacketBonuspot.Value;
            if (PacketGameResult!=null)
                world.GameResult = PacketGameResult;
            if (PacketGamersInfo !=null)
                world.GamerInfo = PacketGamersInfo;
            if ( PacketBroadcast!=null)
                world.Broadcast = PacketBroadcast;
            if (PacketPureData!=null)
                world.PureData = PacketPureData;
            if (PacketGameStart!=null)
                world.GameStart = PacketGameStart;
            if (PacketGamerSlots!=null)
                world.GamerSlots = PacketGamerSlots;
            if (PacketGMGamer != null)
                world.RMGamer = PacketGMGamer;
            if (PacketCountDown != null)
                world.CountDown = PacketCountDown.Value;
            if (PacketGameTime!= null)
                world.GameTime = PacketGameTime.Value;
        }
    }
}
