using System.Diagnostics;
using LinuxSystemControlApp.Models;
using Microsoft.AspNetCore.Mvc;
using Renci.SshNet;
using Renci.SshNet.Sftp;

namespace LinuxSystemControlApp.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    public IActionResult Index()
    {
        return View();
    }

    [HttpPost]
    public IActionResult TestSshConnection()
    {
        string server = "193.230.3.37"; // Your server IP
        int port = 22; // Default SSH port
        string user = "iciadmin"; // Your username
        string password = "2020ZAQ!2wsx"; // Your password

        using var client = new SshClient(server, port, user, password);
        try
        {
            client.Connect();
            if (client.IsConnected)
            {
                _logger.LogInformation(
                    "Successfully connected to {Server} as {User}.",
                    server,
                    user
                );
                return Content("SSH connection successful");
            }
            else
            {
                _logger.LogError("Failed to connect to {Server}.", server);
                return Content("Failed to connect to server");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to SSH server: {Message}", ex.Message);
            return Content($"Error: {ex.Message}");
        }
    }

    [HttpPost]
    public IActionResult ExecuteCommand(TerminalViewModel model, string command)
    {
        _logger.LogInformation(
            "Received connection details: Server={Server}, Port={Port}, User={User}",
            model.Server,
            model.Port,
            model.User
        );

        using var client = new SshClient(model.Server, model.Port, model.User, model.Password);
        try
        {
            client.Connect();
            using SshCommand cmd = client.RunCommand(command);
            return Content(cmd.Result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect or execute command");
            return Content("Error executing command: " + ex.Message);
        }
    }

    [HttpPost]
    public IActionResult Connect(string user, int port, string password, string server)
    {
        var model = new TerminalViewModel
        {
            Server = server,
            Port = port,
            User = user,
            Password = password
        };
        return View("Terminal", model);
    }

    public IActionResult Terminal(TerminalViewModel model) => View(model);

    private static SftpClient ConnectToSftp(string server, int port, string user, string password)
    {
        var sftpClient = new SftpClient(server, port, user, password);
        sftpClient.Connect();
        return sftpClient;
    }

    [HttpPost]
    public IActionResult LoadRootDirectories(TerminalViewModel model)
    {
        if (
            string.IsNullOrEmpty(model.Server)
            || string.IsNullOrEmpty(model.User)
            || string.IsNullOrEmpty(model.Password)
        )
        {
            return BadRequest("Server, user, and password cannot be null or empty.");
        }

        var sftpClient = ConnectToSftp(model.Server, model.Port, model.User, model.Password);
        var rootDirectories = sftpClient.ListDirectory("/").ToList();

        // Separate directories and files
        var directories = rootDirectories
            .Where(f => f.IsDirectory && f.Name != "." && f.Name != "..")
            .OrderBy(f => f.Name) // Sort directories by name
            .ToList();

        var files = rootDirectories
            .Where(f => !f.IsDirectory)
            .OrderBy(f => f.Name) // Optionally sort files by name
            .ToList();

        // Combine sorted directories and files
        var sortedContents = directories.Concat(files).ToList();

        sftpClient.Disconnect();

        // Return the sorted list of directories and files
        ViewBag.Server = model.Server;
        ViewBag.Port = model.Port;
        ViewBag.User = model.User;
        ViewBag.Password = model.Password;

        return PartialView("_DirectoryListPartial", sortedContents);
    }

    [HttpPost]
    public IActionResult LoadFolderContents(
        [FromForm] string path,
        [FromForm] string server,
        [FromForm] int port,
        [FromForm] string user,
        [FromForm] string password
    )
    {
        var sftpClient = new SftpClient(server, port, user, password);
        sftpClient.Connect();

        var folderContents = sftpClient.ListDirectory(path).ToList();

        var directories = folderContents
            .Where(f => f.IsDirectory && f.Name != "." && f.Name != "..")
            .OrderBy(f => f.Name)
            .ToList();

        var files = folderContents.Where(f => !f.IsDirectory).OrderBy(f => f.Name).ToList();

        var sortedContents = directories.Concat(files).ToList();

        sftpClient.Disconnect();

        ViewBag.Server = server;
        ViewBag.Port = port;
        ViewBag.User = user;
        ViewBag.Password = password;

        return PartialView("_DirectoryListPartial", sortedContents);
    }
}
