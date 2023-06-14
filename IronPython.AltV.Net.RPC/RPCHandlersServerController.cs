using AltV.Net.Elements.Entities;
using System.Text.Json;

namespace IronPython.AltV.Net.RPC.Shared
{
    public class RPCHandlersServerController : RPCHandlersController
    {
        #region Global
        public void On<TRequestBody, TResponseBody>(string methodName, Func<IPlayer, TRequestBody, TResponseBody> handler) where TRequestBody : new() =>
            Handlers.Add(new RPCHandler
            {
                Name = methodName,
                MethodInfo = handler.Method,
                Target = handler.Target,
                BodyType = typeof(TRequestBody)
            });

        public object? ExecuteLocalHandler(RPCHandler handler, IPlayer player, string body) =>
            handler.MethodInfo.Invoke(handler.Target, new object[] { player, JsonSerializer.Deserialize(body, handler.BodyType)! });

        #endregion
    }
}
