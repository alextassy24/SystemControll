// Create an instance of the terminal with options
var term = new Terminal({
	cursorBlink: true, // Enable cursor blinking
	theme: {
		background: "#1e1e1e", // VS Code-like dark background
		foreground: "#ffffff", // White text
	},
});

// Attach the terminal to the HTML element
term.open(document.getElementById("terminal"));

// Optional: Write some placeholder text or a welcome message
term.writeln("Welcome to the terminal!");

// Variable to store the full command
let command = "";

// Listen for data (keystrokes)
term.onData(function (data) {
	const key = data;

	// Handle 'Enter' key (ASCII code 13)
	if (key === "\r") {
		// Send the command to the server when 'Enter' is pressed
		$.ajax({
			url: "/Home/ExecuteCommand",
			method: "POST",
			data: { command: command },
			success: function (response) {
				// Write the response from the server to the terminal
				term.write("\r\n" + response + "\r\n");
			},
			error: function () {
				term.write("\r\nError executing command\r\n");
			},
		});

		// Clear the command after it's sent
		command = "";
		term.write("\r\n"); // Move to the next line
	} else if (key === "\u007F") {
		// Handle 'Backspace' (ASCII code for backspace)
		if (command.length > 0) {
			command = command.slice(0, -1);
			term.write("\b \b"); // Erase last character on terminal
		}
	} else {
		// Append the keystroke to the command string
		command += key;
		term.write(key); // Echo the keystroke in the terminal
	}
});
