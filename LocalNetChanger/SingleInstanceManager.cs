using System.IO.Pipes;
using System.Text;

namespace LocalNetChanger;

internal static class SingleInstanceManager
{
    private const string MutexName = "LocalNetChanger.SingleInstance";
    private const string PipeName = "LocalNetChanger.IPC";
    private const string OpenSettingsCommand = "open-settings";

    private static Mutex? _mutex;

    public static bool TryAcquire()
    {
        _mutex = new Mutex(true, MutexName, out var createdNew);
        return createdNew;
    }

    public static void Signal(bool openSettings)
    {
        try
        {
            using var client = new NamedPipeClientStream(".", PipeName, PipeDirection.Out);
            client.Connect(1500);
            using var writer = new StreamWriter(client, Encoding.UTF8) { AutoFlush = true };
            writer.WriteLine(openSettings ? OpenSettingsCommand : "activate");
        }
        catch
        {
            // Calisan ornek bulunamadi veya yanit vermedi.
        }
    }

    public static void StartServer(Action<bool> onCommand)
    {
        Task.Run(async () =>
        {
            while (true)
            {
                try
                {
                    await using var server = new NamedPipeServerStream(PipeName);
                    await server.WaitForConnectionAsync();

                    using var reader = new StreamReader(server, Encoding.UTF8);
                    var command = (await reader.ReadLineAsync())?.Trim();
                    var openSettings = string.Equals(command, OpenSettingsCommand, StringComparison.OrdinalIgnoreCase);
                    onCommand(openSettings);
                }
                catch
                {
                    await Task.Delay(250);
                }
            }
        });
    }

    public static void Release()
    {
        if (_mutex == null)
            return;

        try
        {
            _mutex.ReleaseMutex();
            _mutex.Dispose();
        }
        catch
        {
            // Yoksay
        }
        finally
        {
            _mutex = null;
        }
    }
}
