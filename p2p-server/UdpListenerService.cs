using System;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Xml.XPath;

public class UdpListenerService
{
    private UdpClient _udpClient;
    private int _port;

    private List<server> servers = new List<server>();
    public UdpListenerService(int port)
    {
        _port = port;
        _udpClient = new UdpClient(_port);
    }

    public async Task ListenForUdpRequests()
    {
        while (true)
        {
            try
            {
                var result = await _udpClient.ReceiveAsync();
                byte[] data = result.Buffer;
                string message = Encoding.UTF8.GetString(data);

                // 在这里处理接收到的消息，并将消息转发给Web API进行进一步处理
                ProcessUdpMessage(result.RemoteEndPoint, message);
            }
            catch
            {
                _udpClient.Dispose();
                _udpClient = new UdpClient(_port);
            }
        }
    }

    private async void ProcessUdpMessage(IPEndPoint endPoint, string message)
    {
        Console.WriteLine(message);
        var request = "";
        var requestData = Encoding.UTF8.GetBytes(request);
        var msg = message.Split(':');
        switch (msg[0].ToLower())
        {
            case "add":
                request = $"hi:{endPoint.Address}:{endPoint.Port}";
                // 同名同密码做移除处理
                if (servers.Any(x => x.server_name == msg[1] && x.server_password == msg[2]))
                {
                    request = $"hi:{endPoint.Address}:{endPoint.Port}:发生了同名服务器覆盖";
                    servers.RemoveAll(x => x.server_name == msg[1] && x.server_password == msg[2]);
                    servers.Add(new server
                    {
                        server_host = endPoint.Address.ToString(),
                        server_port = endPoint.Port,
                        server_name = msg[1],
                        server_password = msg[2],
                        client_password = msg[3],
                    });
                }
                else if (servers.Any(x => x.server_name == msg[1] && x.server_password != msg[2]))
                {
                    request = $"error:存在同名服务器添加失败";
                }
                else
                {
                    servers.Add(new server
                    {
                        server_host = endPoint.Address.ToString(),
                        server_port = endPoint.Port,
                        server_name = msg[1],
                        server_password = msg[2],
                        client_password = msg[3],
                    });
                }
                Console.WriteLine(request);
                requestData = Encoding.UTF8.GetBytes(request);
                _udpClient.Send(requestData, requestData.Length, endPoint);
                break;
            case "join":
                var sj = servers.FirstOrDefault(x => x.server_name == msg[1] && x.client_password == msg[2]);
                if (sj == null)
                {
                    request = $"error:找不到对应名称服务器";
                }
                else
                {
                    request = $"server:{sj.server_host}:{sj.server_port}";
                }
                requestData = Encoding.UTF8.GetBytes(request);
                _udpClient.Send(requestData, requestData.Length, endPoint);
                break;
        }
    }


    private class server
    {
        public string server_host { get; set; }
        public int server_port { get; set; }
        public string server_name { get; set; }
        public string server_password { get; set; }
        public string client_password { get; set; }
        public IPEndPoint revIpEndPoint { get; set; }
    }
}