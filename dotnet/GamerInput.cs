
using System.Diagnostics;

namespace ConsoleApp1
{
    public class GamerInput
    {
        private const float TestMaxStayTime = 180000;
        private const float TestGamingStayTime = TestMaxStayTime * 4;
        public Level Now { get; private set; }
        public Stopwatch _Stopwatch { get; private set; }
        public bool LobbyReady;
        public bool IsStartGame;
        public bool IsOverGame;
        public bool IsError;
        public int SlotID;

        public enum Level
        {
            NotConnect, ConnectedNotRegister, WaitingRoomList, WaitingJoinRoom, WaitingPeers,
            WaitEnterGaming,
            WaitSendLoading,
            WaitDeletePlayer,
            WaitWorldState,
            Gaming,
        }

        public GamerInput()
        {
            Now = Level.NotConnect;
            _Stopwatch = Stopwatch.StartNew();
            LobbyReady = false;
            IsStartGame = false;
            IsOverGame = false;
            IsError = false;
            SlotID = 0;
        }

        public void ResetToLobby()
        {
            SetLevel(Level.ConnectedNotRegister);
            LobbyReady = false;
            IsStartGame = false;
            IsOverGame = false;
            SlotID = 0;
        }

        public void SetLevel(Level level)
        {
            Now = level;
            _Stopwatch.Restart();
        }
        public void SetLevel()
        {
            Now++;
            _Stopwatch.Restart();
        }

        public bool TrySetNextLevel(Level nowLevel)
        {
            bool result = (Now == nowLevel);
            if (result)
                SetLevel();
            return result;
        }

        public void CheckStayTime()
        {
            if (IsError)
                return;
            var maxStayTime = TestMaxStayTime;
            if (Now == Level.Gaming)
            {
                maxStayTime = TestGamingStayTime;
            }
            if (_Stopwatch.ElapsedMilliseconds > maxStayTime)
            {
                IsError = true;
                _Stopwatch.Stop();
            }
        }
    }
}
