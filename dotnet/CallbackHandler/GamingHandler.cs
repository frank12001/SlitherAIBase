using GameServer;
using System;
using System.Collections.Generic;

namespace ConsoleApp1.CallbackHandler
{
    //遊戲中
    [Serializable]
    public class GamingHandler : FTServer.Operator.CallbackHandler
    {
        public const byte OperationCode = (byte)EOperationCode.Gaming;
        #region Receive Server CallBack Event
        //接收世界狀態
        public event Action<object> ReceiveWorldState;
        #endregion

        /// <summary>
        /// 發送給 Sever
        /// </summary>
        /// <param name="custom_packet"></param>
        public void SendToServer(Dictionary<byte, object> custom_packet)
        {
            gameService.Deliver(OperationCode, custom_packet);
        }

        #region Receive Server CallBack (被動呼叫)
        public override void ServerCallback(Dictionary<byte, object> server_packet)
        {
            LogProxy.WriteLine("Dictionary Count = " + server_packet.Count + ", first : " + server_packet[0].ToString());
            byte switchCode = byte.Parse(server_packet[0].ToString());
            switch (switchCode)
            {
                case 5: //接收世界狀態
                    Receive_WorldState(server_packet);
                    break;
            }
        }

        #region Server CallBack 各項處理縮排

        private void Receive_WorldState(Dictionary<byte, object> packet)
        {
            object result = packet[1];
            if (ReceiveWorldState != null)
                ReceiveWorldState(result);
        }
        #endregion
        #endregion
    }
}