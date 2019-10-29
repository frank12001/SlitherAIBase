using System;
using System.Threading.Tasks;

namespace ConsoleApp1
{
    public class TaskAgent
    {
        public const int Delay = 5;
        private event Action CallbackPool;
        private Task _Main;

        public void RegisteredToNetworkTask(Action action)
        {
            CallbackPool += action;
            if (_Main != null) return;

            _Main = Task.Run(Function)
                .ContinueWith(
                    (task) => 
                    {
                        Console.WriteLine(task.Exception);
                        Environment.Exit(-1);
                    }, 
                    TaskContinuationOptions.OnlyOnFaulted);

            async Task Function()
            {
                while (true)
                {
                    CallbackPool?.Invoke();
                    await Task.Delay(Delay);
                }
            }
        }

        public void UnRegisteredToNetworkTask(Action action)
        {
            CallbackPool -= action;
        }
    }
}