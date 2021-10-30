using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Raid.DataModel;
using SuperSocket.WebSocket;
using SuperSocket.WebSocket.Server;

namespace Raid.Service
{
    public class ModelService
    {
        private readonly List<IMessageScopeHandler> ScopeHandlers;
        private ILogger<ModelService> Logger;
        public ModelService(ILogger<ModelService> logger, IEnumerable<IMessageScopeHandler> handlers)
        {
            Logger = logger;
            ScopeHandlers = handlers.ToList();
        }

        private ValueTask ProcessMessage(ISocketSession session, WebSocketPackage message)
        {
            Logger.LogInformation(ServiceEvent.HandleMessage.EventId(), "ProcessMessage");
            using var sessionScope = Logger.BeginScope($"[SessionId = {session.Id}");

            try
            {
                var socketMessage = JsonConvert.DeserializeObject<SocketMessage>(message.Message);
                HandleMessageCore(session, socketMessage);
            }
            catch (Exception ex)
            {
                Logger.LogError(ServiceError.MessageHandlerFailure.EventId(), ex, "Failed to handle message");
            }

            return ValueTask.CompletedTask;
        }

        private void HandleMessageCore(ISocketSession session, SocketMessage socketMessage)
        {
            var messageScope = Logger.BeginScope($"[Scope = {socketMessage.Scope}, Channel = {socketMessage.Channel}]");

            try
            {
                IMessageScopeHandler handler = ScopeHandlers.FirstOrDefault(handler => handler.SupportsScope(socketMessage.Scope));
                if (handler != null)
                {
                    handler.HandleMessage(socketMessage, session);
                }
                else
                {
                    Logger.LogWarning(ServiceError.UnknownMessageScope.EventId(), $"Unknown scope '{socketMessage.Scope}'");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ServiceError.MessageHandlerFailure.EventId(), ex, "Failed to handle message");
            }
        }

        public static void HandleMessage(ISocketSession session, SocketMessage message)
        {
            RaidHost.Services.GetRequiredService<ModelService>()
                .HandleMessageCore(session, message);
        }

        public static ValueTask HandleMessage(WebSocketSession session, WebSocketPackage message)
        {
            return RaidHost.Services.GetRequiredService<ModelService>()
                .ProcessMessage(new SuperSocketAdapter(session), message);
        }
    }
}