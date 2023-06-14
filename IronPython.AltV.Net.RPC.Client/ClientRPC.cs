using AltV.Net.Client;
using IronPython.AltV.Net.RPC.Shared;
using System.Text.Json;

namespace IronPython.AltV.Net.RPC.Client;

public static class ClientRPC
{
    static RPCRequestsController RequestsController = new RPCRequestsController();
    static RPCHandlersClientController Controller = new RPCHandlersClientController();

    public static void On<TRequestBody, TResponseBody>(string methodName, Func<TRequestBody, TResponseBody> handler) where TRequestBody : new() =>
        Controller.On(methodName, handler);

    public static Task<TResponseBody> ExecuteServerMethod<TResponseBody, TRequestBody>(string methodName, TRequestBody body)
    {
        var tcs = new TaskCompletionSource<TResponseBody>();
        var request = RequestsController.AddRequest(methodName, body);

        Alt.EmitServer("__ironPython:RPC:execute", JsonSerializer.Serialize(request));

        RequestsController.OnNewReponse += (newResponse) =>
        {
            if (newResponse.Id != request.Id) return;

            var response = JsonSerializer.Deserialize<TResponseBody>(newResponse.ResponseBody);

            if(!tcs.TrySetResult(response))
            {
                tcs.SetCanceled();
            }
        };

        return tcs.Task;
    }

    public static void InitRPCClient()
    {
        Alt.OnServer<string, string>("__ironPython:RPC:setResponse", (messageId, responseBody) =>
        {
            Task.Run(() =>
            {
                try
                {
                    RequestsController.AddResponse(Guid.Parse(messageId), responseBody);
                }
                catch (Exception) { }
            });
        });

        Alt.OnServer<string>("__ironPython:RPC:execute", (jsonMessage) =>
        {
            Task.Run(() =>
            {
                var message = JsonSerializer.Deserialize<RPCRequest>(jsonMessage)!;

                var handler = Controller.GetHandler(message.MethodName);

                var result = Controller.ExecuteLocalHandlerWithoutExecutor(handler, message.RequestBody);

                Alt.EmitServer("__ironPython:RPC:setResponse", $"{message.Id}", JsonSerializer.Serialize(result));
            });
        });
    }
}
