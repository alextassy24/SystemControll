# Linux System Control App

A web-based system control application built using **ASP.NET MVC 8.0** and **HTMX**, designed to provide a user-friendly interface for managing Linux system files and executing terminal commands. This app combines the functionality of tools like **PuTTY** and **WinSCP** into a single web-based platform with a **VS Code-like UI**.

## Features:

- **SSH-based Authentication:** Securely connect to your Linux system using SSH.
- **File Management:** Browse, upload, download, and manage files on your remote Linux system.
- **Terminal Access:** Execute Linux commands in real-time directly from the web interface.
- **VS Code-like UI:** Intuitive interface with a file explorer on the left and a terminal on the right.
- **Real-time updates:** Seamless real-time interaction using SignalR.

## Technologies:

- **ASP.NET MVC 8.0** for backend and MVC architecture
- **HTMX** for dynamic, seamless UI updates
- **SSH.NET** for SSH communication
- **SignalR** for real-time terminal updates
- **xterm.js** for a fully functional in-browser terminal
