using System;
using System.Collections.Generic;
using GameServer;
using Newtonsoft.Json;

namespace ConsoleApp1.CallbackHandler
{
    //WebLobby
    public class LobbyHandler : FTServer.Operator.CallbackHandler
    {
        public const byte OperationCode = (byte)EOperationCode.Lobby;
        public event Action<Dictionary<byte, object>> RecvPacket;

        public override void ServerCallback(Dictionary<byte, object> server_packet)
        {
            if (RecvPacket == null)
                return;

            RecvPacket(server_packet);
        }

        public void SendToServer(/*Dictionary<byte, object> custom_packet*/EClientLobbyCode code, object obj)
        {
            
            gameService.Deliver(OperationCode, new Dictionary<byte, object>
            {
                {(byte)code, JsonConvert.SerializeObject(obj)}
            });
        }
    }
}