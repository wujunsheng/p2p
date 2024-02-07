using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Remoting.Messaging;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Shell;

namespace p2p_client
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        private UdpClient udpClient;
        public MainWindow()
        {
            InitializeComponent();
            btn_stop_server.Visibility = Visibility.Hidden;
        }

        private async void btn_run_server_Click(object sender, RoutedEventArgs e)
        {
            btn_run_server.IsEnabled = false;
            var hole = new UdpClient();
            AddMessage("开始运行");
            #region 保存页面值
            int lp = Convert.ToInt32(local_port.Text);
            string hostIp = host_ip.Text;
            int hostPort = Convert.ToInt32(host_port.Text);
            var requestData = Encoding.UTF8.GetBytes($"Add:{server_name.Text}:{server_password.Text}:{client_password.Text}");
            var fp = t_file.Text;
            var fpa = t_file_args.Text;
            #endregion
#pragma warning disable CS4014 // 由于此调用不会等待，因此在调用完成前将继续执行当前方法
            Task.Run(async () =>
            {
                #region 端口连接
                try
                {
                    hole.Client.Bind(new IPEndPoint(IPAddress.Any, lp));
                }
                catch (SocketException)
                {
                    AddMessage("端口已被占用");
                    Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        btn_run_server.IsEnabled = true;
                    });
                    hole.Dispose();
                    return;
                }
                try
                {
                    hole.Send(requestData, requestData.Length, hostIp, hostPort);
                    var r = hole.ReceiveAsync();
                    var i = Task.WaitAny(new Task[] { r }, 3000);
                    if (i != 0)
                    {
                        Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            btn_run_server.IsEnabled = true;
                            AddMessage("服务器连接失败");
                        });
                        hole.Dispose();
                        return;
                    }
                    byte[] data = r.Result.Buffer;
                    var message = Encoding.UTF8.GetString(data);
                    if (message.StartsWith("error"))
                    {
                        var em = message.Split(':')[1];
                        AddMessage("!!!!!!!!!" + em + "!!!!!!!!!");
                        Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            btn_run_server.IsEnabled = true;
                            local_remote_host.Text = em;
                        });
                        return;
                    }
                    AddMessage($"IP:{r.Result.RemoteEndPoint} => {message}");
                    hole.Close();
                    hole.Dispose();
                    hole = null;
                    #endregion
                    #region 打开程序
                    if (!string.IsNullOrWhiteSpace(fp))
                    {
                        OpenApplication(fp, fpa);
                    }
                    #endregion
                    Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        var msg = message.Split(':');
                        if (msg.Length > 3)
                        {
                            AddMessage("!!!!!!!!!" + msg[3] + "!!!!!!!!!");
                        }
                        local_remote_host.Text = $"{msg[1]}:{msg[2]}";
                        btn_run_server.IsEnabled = true;
                        btn_run_server.Visibility = Visibility.Hidden;
                        btn_stop_server.Visibility = Visibility.Visible;
                    });
                    #region 持续访问保持端口
                    var endpoint = new IPEndPoint(IPAddress.Parse(message.Split(':')[1]), Convert.ToInt32(message.Split(':')[2]));
                    udpClient = new UdpClient(new IPEndPoint(IPAddress.Any, 0));
                    while (udpClient != null)
                    {
                        await Task.Delay(5000);
                        if (udpClient != null)
                        {
                            udpClient.Send(Encoding.UTF8.GetBytes("ping"), 4, endpoint);
                            Application.Current.Dispatcher.InvokeAsync(() =>
                            {
                                if (chk_show_request.IsChecked == true)
                                {
                                    AddMessage($"请求 => {endpoint} : ping");
                                }
                            });
                        }
                    }
                    #endregion
                }
                catch (Exception)
                {
                    if (hole != null)
                        hole.Dispose();
                    Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        AddMessage("启动异常");
                        btn_run_server.IsEnabled = true;
                    });
                }
            });
#pragma warning restore CS4014 // 由于此调用不会等待，因此在调用完成前将继续执行当前方法
        }

        private void AddMessage(string message)
        {
            var msg = $"[{DateTime.Now.ToLongTimeString()}] {message}";
            // 确保在UI线程上执行
            Application.Current.Dispatcher.InvokeAsync(() =>
            {
                info.Text = $"{msg}\n" + info.Text;
            });
        }

        private void btn_stop_server_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                udpClient.Dispose();
                udpClient = null;
            }
            catch
            {
                udpClient = null;
            }
            AddMessage("终止联通");
            local_remote_host.Text = "终止联通";
            btn_stop_server.Visibility = Visibility.Hidden;
            btn_run_server.Visibility = Visibility.Visible;
        }

        private void btn_file_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog();
            openFileDialog.Filter = "所有文件 (*.*)|*.*"; // 设置文件过滤器
            if (openFileDialog.ShowDialog() == true)
            {
                t_file.Text = openFileDialog.FileName;
            }
        }

        private void OpenApplication(string fileName, string arguments)
        {
            try
            {
                // 或者，如果需要指定工作目录和命令行参数，可以使用ProcessStartInfo对象
                ProcessStartInfo startInfo = new ProcessStartInfo();
                startInfo.FileName = fileName;
                // 添加命令行参数（如果应用需要）
                if (string.IsNullOrWhiteSpace(arguments))
                {
                    startInfo.Arguments = arguments;
                }

                Process.Start(startInfo);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"无法打开应用程序: {ex.Message}");
            }
        }

        private void btn_copy_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(remote_host.Text);
        }

        private async void btn_get_server_Click(object sender, RoutedEventArgs e)
        {
            btn_get_server.IsEnabled = false;
            var upd = new UdpClient(new IPEndPoint(IPAddress.Any, 0));
            var requestData = Encoding.UTF8.GetBytes($"Join:{c_server_name.Text}:{c_client_password.Text}");
            var r = upd.ReceiveAsync();
            upd.Send(requestData, requestData.Length, c_host_ip.Text, Convert.ToInt32(c_host_port.Text));
            try
            {
                var i = Task.WaitAny(new Task[] { r }, 3000);
                if (i == 0)
                {
                    byte[] data = r.Result.Buffer;
                    var message = Encoding.UTF8.GetString(data);
                    var msg = message.Split(':');
                    if (message.StartsWith("server"))
                    {
                        remote_host.Text = $"{msg[1]}:{msg[2]}";
                    }
                    else
                    {
                        remote_host.Text = $"{msg[1]}";
                    }
                }
                else
                {
                    remote_host.Text = "请求失败";
                }
            }
            catch
            {
                remote_host.Text = "请求失败";
            }
            finally
            {
                if (upd != null)
                {
                    upd.Close();
                    upd.Dispose();
                }
                upd = null;
                btn_get_server.IsEnabled = true;
            }
        }
    }
}
