# Custom URI Scheme Registration PoC

This repository is a Proof of Concept (PoC) demonstrating how to register a custom URI scheme (e.g., `com.awesome.myapp://`) on Windows using a C# WPF application.

## Features

- **Custom URI Scheme Registration**: Registers a URI scheme in the Windows Registry, allowing the application to be launched by navigating to a URL with that scheme.
- **Single-Instance Application**: Ensures that only one instance of the application can run at a time.
- **Inter-Process Communication (IPC)**: If the application is already running, any new attempts to launch it via the URI scheme will forward the launch arguments (the URI itself) to the existing instance.

## How It Works

1.  **URI Registration**: The application adds entries to the Windows Registry under `HKEY_CURRENT_USER\SOFTWARE\Classes\` to associate the custom scheme (`com.awesome.myapp`) with the application's executable. This is handled in `MainWindow.xaml.cs`.
2.  **Single-Instance Check**: On startup, the application attempts to create a named `Mutex`.
    - If the `Mutex` already exists, it means another instance is running. The new instance acts as a client, sends its command-line arguments to the primary instance, and then exits.
    - If the `Mutex` does not exist, this is the primary instance. It creates the `Mutex` and starts a `NamedPipeServer` to listen for messages from other instances.
3.  **Argument Forwarding**: Communication between instances is handled using **Named Pipes**. The client instance writes the URI arguments to the pipe, and the server instance reads them, updating the main window accordingly.

## How to Use

1.  Build and run the application.
2.  Click the **"Register URI Scheme"** button in the main window.
3.  Open a command prompt or the Run dialog (`Win + R`) and enter a URI like:
    ```
    com.awesome.myapp://some-data-from-the-uri
    ```
4.  The running application will come to the foreground and display the URI that was used to launch it.

## Testing from the Browser

You can also test the URI scheme by opening the `index.html` file in your web browser. This file contains several links that use the `com.awesome.myapp://` protocol. Clicking these links will launch the application and pass the specific URI to it.

This demonstrates a robust way to handle custom protocols, ensuring a smooth user experience by consolidating all requests into a single application window.
