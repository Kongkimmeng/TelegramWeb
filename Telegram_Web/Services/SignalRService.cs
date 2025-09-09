using Microsoft.AspNetCore.SignalR.Client;

namespace Telegram_Web.Services
{
    public class SignalRService
    {
        public HubConnection? HubConnection { get; private set; }
        public event Action? OnStatusChanged;

        public HubConnectionState State => HubConnection?.State ?? HubConnectionState.Disconnected;

        public async Task InitializeAsync(string hubUrl)
        {
            if (HubConnection != null && HubConnection.State != HubConnectionState.Disconnected)
                return; // already initialized

            HubConnection = new HubConnectionBuilder()
                .WithUrl(hubUrl, options =>
                {
                    options.HttpMessageHandlerFactory = (handler) =>
                    {
                        if (handler is HttpClientHandler clientHandler)
                        {
                            clientHandler.MaxRequestContentBufferSize = 1024 * 1024 * 50; // 50 MB
                        }
                        return handler;
                    };
                })
                .WithAutomaticReconnect()
                .Build();

            // notify UI when status changes
            HubConnection.Closed += async (error) =>
            {
                OnStatusChanged?.Invoke();
                await Task.Delay(2000);
                await HubConnection.StartAsync();
            };
            HubConnection.Reconnected += (connectionId) =>
            {
                OnStatusChanged?.Invoke();
                return Task.CompletedTask;
            };
            HubConnection.Reconnecting += (error) =>
            {
                OnStatusChanged?.Invoke();
                return Task.CompletedTask;
            };

            await HubConnection.StartAsync();
            OnStatusChanged?.Invoke();
        }

        public async Task BroadcastAsync<T>(string method, T message)
        {
            if (HubConnection != null && State == HubConnectionState.Connected)
            {
                await HubConnection.InvokeAsync(method, message);
            }
        }
    }
}
