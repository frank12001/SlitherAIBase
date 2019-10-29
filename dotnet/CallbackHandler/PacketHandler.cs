using System;
using FTServer.Operator;
using System.Collections.Generic;
using GameServer;
//using GamingRoom.Gaming.Room.GameLogic.GameEventSystem;
namespace FTServer.Operator
{
    public class PacketHandler : CallbackHandler
    {
        public const byte GamePacketOperationCode = (byte)EOperationCode.GameLogic;
        public const byte GameRoomOperationCode   = (byte)EOperationCode.GameRoom;
        
        public readonly byte OperationCode;

        public Action<Dictionary<byte, object>> Receive;
        

        public PacketHandler(byte OperationCode)
        {
            this.OperationCode = OperationCode;

        }


        public void Send(Dictionary<byte, object> packet)
        {
            gameService.Deliver(OperationCode, packet);
        }

        #region Override Methods

        /// <summary>
        /// 接收Server封包後的回呼
        /// </summary>
        /// <param name="server_packet">接收到的封包</param>
        public override void ServerCallback(Dictionary<byte, object> server_packet)
        {
            //LogProxy.WriteLine("PacketHandler OperatorCode = " + OperationCode);
            Receive?.Invoke(server_packet);
        }

        #endregion
    }

    public interface IDataReceiver
    {
        byte OperationCode { get; }
        byte SwitchCode { get; }
        void GameDataReceive(object objs);
    }

    public interface IPacketReceiver
    {
        byte OperationCode { get; }
        void GamePacketReceive(Dictionary<byte, object> packet);
    }

}

