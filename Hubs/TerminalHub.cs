using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Renci.SshNet;
using Renci.SshNet.Sftp;

namespace LinuxSystemControlApplication.Hubs
{
    public class TerminalHub : Hub
    {
        // Store the SSH and SFTP clients per connection
        private static readonly Dictionary<string, SshClient> _sshClients =
            new Dictionary<string, SshClient>();
        private static readonly Dictionary<string, SftpClient> _sftpClients =
            new Dictionary<string, SftpClient>();

        public async Task ConnectToServer(string server, int port, string user, string password)
        {
            if (!_sshClients.ContainsKey(Context.ConnectionId))
            {
                var sshClient = new SshClient(server, port, user, password);
                var sftpClient = new SftpClient(server, port, user, password);

                sshClient.Connect();
                sftpClient.Connect();

                _sshClients[Context.ConnectionId] = sshClient;
                _sftpClients[Context.ConnectionId] = sftpClient;

                await Clients.Caller.SendAsync("ReceiveCommandOutput", "Connected to server.");
            }
        }

        public async Task ExecuteCommand(string command)
        {
            if (_sshClients.ContainsKey(Context.ConnectionId))
            {
                var client = _sshClients[Context.ConnectionId];
                if (client.IsConnected)
                {
                    try
                    {
                        using var cmd = client.CreateCommand(command);
                        cmd.CommandTimeout = TimeSpan.FromMinutes(5); // Set a command timeout to handle long-running commands

                        // Execute the command asynchronously
                        var asyncResult = cmd.BeginExecute();
                        var output = new System.Text.StringBuilder();

                        // Read the output as it is being written to
                        using (var reader = new StreamReader(cmd.OutputStream))
                        {
                            while (!asyncResult.IsCompleted || !reader.EndOfStream)
                            {
                                output.Append(await reader.ReadToEndAsync());
                                await Task.Delay(100); // Small delay to prevent blocking
                            }
                        }

                        // Read the error stream
                        using (var errorReader = new StreamReader(cmd.ExtendedOutputStream))
                        {
                            if (!errorReader.EndOfStream)
                            {
                                var error = await errorReader.ReadToEndAsync();
                                if (!string.IsNullOrEmpty(error))
                                {
                                    output.Append($"\nError: {error}");
                                }
                            }
                        }

                        cmd.EndExecute(asyncResult); // Finalize the async execution

                        // Send the result back to the client
                        await Clients.Caller.SendAsync("ReceiveCommandOutput", output.ToString());
                    }
                    catch (Exception ex)
                    {
                        await Clients.Caller.SendAsync(
                            "ReceiveCommandOutput",
                            $"Error: {ex.Message}"
                        );
                    }
                }
                else
                {
                    await Clients.Caller.SendAsync(
                        "ReceiveCommandOutput",
                        "SSH client is not connected."
                    );
                }
            }
            else
            {
                await Clients.Caller.SendAsync("ReceiveCommandOutput", "No SSH client found.");
            }
        }

        public async Task ListRootDirectories()
        {
            if (_sftpClients.ContainsKey(Context.ConnectionId))
            {
                var sftpClient = _sftpClients[Context.ConnectionId];
                if (sftpClient.IsConnected)
                {
                    var rootDirectories = sftpClient.ListDirectory("/");

                    var directories = rootDirectories
                        .Where(f => f.IsDirectory && f.Name != "." && f.Name != "..") // Filter directories only
                        .Select(f => f.Name) // Get the directory names
                        .ToList();

                    // Send the directory list to the client
                    await Clients.Caller.SendAsync("ReceiveDirectoryList", directories);
                }
                else
                {
                    await Clients.Caller.SendAsync(
                        "ReceiveCommandOutput",
                        "SFTP client is not connected."
                    );
                }
            }
            else
            {
                await Clients.Caller.SendAsync("ReceiveCommandOutput", "No SFTP client found.");
            }
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            // Clean up SSH and SFTP connections when the SignalR connection is closed
            if (_sshClients.ContainsKey(Context.ConnectionId))
            {
                _sshClients[Context.ConnectionId].Disconnect();
                _sshClients.Remove(Context.ConnectionId);
            }

            if (_sftpClients.ContainsKey(Context.ConnectionId))
            {
                _sftpClients[Context.ConnectionId].Disconnect();
                _sftpClients.Remove(Context.ConnectionId);
            }

            await base.OnDisconnectedAsync(exception);
        }
    }
}
