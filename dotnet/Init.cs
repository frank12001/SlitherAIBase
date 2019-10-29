using System;
using System.Net;
using ConsoleApp1.CallbackHandler;
using ConsoleApp1.Tool;
using FTServer;
using FTServer.Example;
using FTServer.Operator;
using GameServer;
using Newtonsoft.Json;
using RestSharp;
using SlitherEvo;

namespace ConsoleApp1
{
    public class Init
    {
        const string userKey = "0452DA61-B201-49A4-B403-37B0760FFBCB";

        public static Urls QueryGameServerUrl(string agentServerIP, string version, string type)
        {
            return QueryIP(agentServerIP, version, type);
        } 

        public static Tuple<Urls, string, Account> Http(string agentServerIP, string devicedId, string version, string type)
        {
            Console.WriteLine($"devicedId: {devicedId}");
            var urls = QueryIP(agentServerIP, version, type);
            var accessToken = Authentication(urls, devicedId);
            var account = CreateAccount(urls, accessToken);
            return Tuple.Create(urls, accessToken, account);
        }
        public static Tuple<Connect,_System,LobbyHandler, ToArenaHandler, GamingHandler,PacketHandler,PacketHandler> Rudp(Uri gamingServerIp)
        {
            Connect connect = new Connect(gamingServerIp.Host,gamingServerIp.Port,NetworkProtocol.RUDP);
            
            var system = new _System();
            connect.AddCallBackHandler(_System.OperatorCode, system);
            
            var gaming = new GamingHandler();
            connect.AddCallBackHandler(GamingHandler.OperationCode, gaming);

            var lobby = new LobbyHandler();
            connect.AddCallBackHandler(LobbyHandler.OperationCode, lobby);

            var toArena = new ToArenaHandler();
            connect.AddCallBackHandler((byte)EOperationCode.ToArena, toArena);

            var gamePacketHandler  = new PacketHandler(PacketHandler.GamePacketOperationCode);
            connect.AddCallBackHandler(PacketHandler.GamePacketOperationCode, gamePacketHandler);
            
            var  gameRoomHandler = new PacketHandler(PacketHandler.GameRoomOperationCode);
            connect.AddCallBackHandler(PacketHandler.GameRoomOperationCode,gameRoomHandler);
            
            //establish connection
            system.Connect += () => { Console.WriteLine("Connect to server.");
            };

            return Tuple.Create(connect,system,lobby, toArena, gaming,gamePacketHandler,gameRoomHandler);
        }
        private static Urls QueryIP(string agentServerIP, string clientVer, string loginType)
        {
            var input = JsonConvert.SerializeObject(new {ClientVersion = clientVer, LoginType = loginType });
            input = StringEncrypt.Encrypt(input, userKey);

            Uri agentServer = new Uri(agentServerIP);
            var client = new RestClient(agentServer);
            var request = new RestRequest("QueryIP");

            request.AddParameter("Json", input, ParameterType.QueryString);
            var response = client.Post(request);
            var content = response.Content;

            return JsonConvert.DeserializeObject<Urls>(content);
        }

        public static string Authentication(string userUrl, string devicedId)
        {
            // Cannot access local ip in k8s pod
            RestClient client = new RestClient(new Uri(userUrl));
            var request = new RestRequest("Authentication");
            var input = JsonConvert.SerializeObject(new { DeviceID = devicedId, SecretOpenID = "" });
            input = StringEncrypt.Encrypt(input, userKey);
            request.AddParameter("application/json", input, ParameterType.RequestBody);

            var response = client.Post(request);
            var content = StringEncrypt.Decrypt(response.Content, userKey);
            
            var definition = new { Code = "", Msg = "", AccessToken = "" };

            var result = JsonConvert.DeserializeAnonymousType(content, definition);

            return result.AccessToken;
        }

        private static string Authentication(Urls url,string devicedId)
        {
            var client = new RestClient(url.UserUrl);
            var request = new RestRequest("Authentication");
            var input = JsonConvert.SerializeObject(new {DeviceID = devicedId, SecretOpenID = ""});
            input = StringEncrypt.Encrypt(input, userKey);
            request.AddParameter("application/json", input, ParameterType.RequestBody);

            var response = client.Post(request);
            var content = StringEncrypt.Decrypt(response.Content, userKey);

            var definition = new {Code = "", Msg = "", AccessToken = ""};
            var result = JsonConvert.DeserializeAnonymousType(content, definition);

            return result.AccessToken;
        }

        public static Account CreateAccount(string userUrl, string accessToken)
        {
            var client = new RestClient(userUrl);
            var request = new RestRequest("Login");
            request.AddParameter("Authorization", accessToken, ParameterType.HttpHeader);
            var input = StringEncrypt.Encrypt(JsonConvert.SerializeObject(new { Language = "Taiwan" }), userKey);
            request.AddParameter("application/json", input, ParameterType.RequestBody);
            var response = client.Post(request);
            var content = StringEncrypt.Decrypt(response.Content, userKey);
            var definition = new { Code = "", Msg = "", Account = new SlitherEvo.Account() };

            return JsonConvert.DeserializeAnonymousType(content, definition).Account;
        }

        public static Account AILogin(string userUrl, string accessToken)
        {
            var client = new RestClient(userUrl);
            var request = new RestRequest("Login");
            request.AddParameter("Authorization", accessToken, ParameterType.HttpHeader);
            var input = StringEncrypt.Encrypt(JsonConvert.SerializeObject(new { Language = "Taiwan" }), userKey);
            request.AddParameter("application/json", input, ParameterType.RequestBody);
            var response = client.Post(request);
            var content = StringEncrypt.Decrypt(response.Content, userKey);
            var definition = new { Code = "", Msg = "", Account = new SlitherEvo.Account() };

            return JsonConvert.DeserializeAnonymousType(content, definition).Account;
        }

        public static Account CreateAccount(Urls urls, string accessToken)
        {
            var client = new RestClient(urls.UserUrl);
            var request = new RestRequest("Login");
            request.AddParameter("Authorization", accessToken, ParameterType.HttpHeader);
            var input = StringEncrypt.Encrypt(JsonConvert.SerializeObject(new { Language = "Taiwan" }), userKey);
            request.AddParameter("application/json", input, ParameterType.RequestBody);
            var response = client.Post(request);
            var content = StringEncrypt.Decrypt(response.Content, userKey);
            var definition = new {Code = "", Msg = "", Account = new SlitherEvo.Account()};

            return JsonConvert.DeserializeAnonymousType(content, definition).Account;
        }
    }

    public class Urls
    {
        public HttpStatusCode Code;
        [JsonProperty("UserIP")] public string UserUrl;
        [JsonProperty("AgentIP")] public string AgentUrl;
        [JsonProperty("BattleIP")] public string GamingUrl;
        [JsonProperty("MetricIP")] public string MetricUrl;
        [JsonProperty("AssetsBundleIP")] public string AssetsBundleUrl;
        [JsonProperty("ServerArea")] public string ServerArea;

        public override string ToString()
        {
            return $"Code: {Code}, UserUrl: {UserUrl}, AgentUrl: {AgentUrl}, MetricUrl: {MetricUrl}, AssetsBundleUrl:{AssetsBundleUrl}, ServerArea: {ServerArea}";
        }
    }

    public enum ELoginType
    {
        NewAccount,
        Login
    }
}