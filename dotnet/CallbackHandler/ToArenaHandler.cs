using System;
using System.Collections.Generic;
using System.Text;
using ConsoleApp1;
using GameServer;
using GameServer.Packet;

namespace FTServer.Operator
{
    public class ToArenaHandler : CallbackHandler
    {
        IGamerEntity GamerEntity;
        public void SetCallBack(IGamerEntity gamerEntity)
        {
            GamerEntity = gamerEntity;
        }

        public override void ServerCallback(Dictionary<byte, object> payload)
        {
            foreach (var pair in payload)
            {
                if (!Enum.TryParse(pair.Key.ToString(), out EServerToArenaCode code))
                {
                    Console.WriteLine($"Parse EServerToArenaCode fail, Value:{pair.Key}");
                    return;
                }

                switch (code)
                {
                    case EServerToArenaCode.EnterArena:
                        Console.WriteLine($"{DateTime.Now}: EServerToArenaCode.EnterArena, Data:{pair.Value}");
                        GamerEntity?.fireReceiver.fireReceiveEnterArena((EnterArenaPacket)pair.Value);
                        break;
                    case EServerToArenaCode.KickOff:
                        Console.WriteLine($"{DateTime.Now}: EServerToArenaCode.KickOff, Msg:{pair.Value}");
                        break;
                    case EServerToArenaCode.DeleteArenaPlayers:
                        byte[] deleteSlots = (byte[])pair.Value;
                        Console.WriteLine($"{DateTime.Now}: EServerToArenaCode.DeleteArenaPlayers, DeleteSlots:{toString(deleteSlots)}");
                        GamerEntity?.fireReceiver.fireDeleteArenaPlayers(deleteSlots);
                        break;
                    case EServerToArenaCode.StartGame:
                        Console.WriteLine($"{DateTime.Now}: EServerToArenaCode.StartGame");
                        GamerEntity?.fireReceiver.fireStartGame();
                        break;
                }
            }

            string toString(byte[] slots)
            {
                var builder = new StringBuilder();

                if (slots.Length == 0)
                    return "Empty";

                foreach (var slot in slots)
                {
                    builder.Append(slot);
                    builder.Append(",");
                }

                return builder.ToString();
            }
        }

        public void SendLoading(byte percent)
        {
            if (percent > 100)
                percent = 100;

            Console.WriteLine($"EClientToArenaCode.LoadingPercent, Percent:{percent}");
            send(EClientToArenaCode.LoadingPercent, percent);
        }

        public void SendReady()
        {
            Console.WriteLine("Send EClientToArenaCode.Ready...");
            send(EClientToArenaCode.Ready, 0);
        }

        private void send(EClientToArenaCode code, object data)
        {
            var payload = new Dictionary<byte, object>();
            payload.Add((byte)code, data);
            gameService.Deliver((byte)EOperationCode.ToArena, payload);
        }
    }
}