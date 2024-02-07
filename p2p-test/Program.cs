
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;

var client = new UdpClient();
try
{
    client.Client.Bind(new IPEndPoint(IPAddress.Any, 8211));
}
catch (SocketException)
{
    Console.WriteLine("端口已被占用");
    Console.ReadLine();
    return;
}

client.Send(Encoding.ASCII.GetBytes("hi"), 2, "47.97.111.114", 8211);

var result = await client.ReceiveAsync();
byte[] data = result.Buffer;
var message = Encoding.ASCII.GetString(data);
Console.WriteLine($"IP:{result.RemoteEndPoint.Address} 端口:{result.RemoteEndPoint.Port} 信息:{message}");

client.Close();
client.Dispose();

Process.Start("E:\\Steamcmd\\steamapps\\common\\PalServer\\PalServer.exe");


var endpoint = new IPEndPoint(IPAddress.Parse(message.Split(':')[1]), Convert.ToInt32(message.Split(':')[2]));
client = new UdpClient(new IPEndPoint(IPAddress.Any, 8210));
#pragma warning disable CS4014 // 由于此调用不会等待，因此在调用完成前将继续执行当前方法
Task.Run(async () =>
    {
        while (true)
        {
            var result = await client.ReceiveAsync();
            byte[] data = result.Buffer;
            var message = Encoding.ASCII.GetString(data);
            Console.WriteLine($"IP:{result.RemoteEndPoint.Address} 端口:{result.RemoteEndPoint.Port} 信息:{message}");
        }
    }
);
#pragma warning restore CS4014 // 由于此调用不会等待，因此在调用完成前将继续执行当前方法
while (true)
{
    await Task.Delay(5000);
    client.Send(Encoding.ASCII.GetBytes("ping"), 4, endpoint);
}


void OpenApplication(string fileName, string arguments)
{
    try
    {
        // 如果路径包含文件名，则直接打开
        Process.Start(fileName);

        // 或者，如果需要指定工作目录和命令行参数，可以使用ProcessStartInfo对象
        ProcessStartInfo startInfo = new ProcessStartInfo();
        startInfo.FileName = fileName;
        // 如果是可执行文件在其他目录下，可以设置工作目录
        startInfo.WorkingDirectory = @"C:\Path\To\Application";
        // 添加命令行参数（如果应用需要）
        startInfo.Arguments = arguments;

        Process.Start(startInfo);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"无法打开应用程序: {ex.Message}");
    }
}