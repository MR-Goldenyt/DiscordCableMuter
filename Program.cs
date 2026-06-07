using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;
using System.Diagnostics;
using System.Windows.Forms;

class Program
{
    static string svclPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "svcl.exe");
    static string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "muter.log");

    static void Log(string msg) =>
        File.AppendAllText(logPath, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {msg}{Environment.NewLine}");

    static void TrimLog()
    {
        if (!File.Exists(logPath)) return;

        var cutoff = DateTime.Now.AddDays(-7);
        var lines = File.ReadAllLines(logPath);

        var kept = lines.Where(line =>
        {
            if (line.Length < 19) return false;
            return DateTime.TryParse(line[..19], out var ts) && ts >= cutoff;
        }).ToArray();

        File.WriteAllLines(logPath, kept);
    }

    [STAThread]
    static void Main()
    {
        TrimLog();

        if (!File.Exists(svclPath))
        {
            Log("svcl.exe not found next to DiscordCableMuter.exe - exiting.");
            return;
        }

        var enumerator = new MMDeviceEnumerator();
        bool found = false;

        foreach (var device in enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active))
        {
            if (!device.FriendlyName.Contains("VB-Audio Virtual Cable"))
                continue;

            Log($"Watching: {device.FriendlyName}");
            found = true;

            // Scan sessions already running before the program started
            var sessionManager = device.AudioSessionManager;
            sessionManager.RefreshSessions();

            for (int i = 0; i < sessionManager.Sessions.Count; i++)
            {
                var existingSession = sessionManager.Sessions[i];
                string existingId = existingSession.GetSessionIdentifier;

                if (existingId.Contains("Discord", StringComparison.OrdinalIgnoreCase))
                {
                    Log("Discord already running on Cable Input at startup - muting existing session...");
                    var watcher = new SessionWatcher(svclPath, logPath);
                    existingSession.RegisterEventClient(watcher);
                    watcher.MuteNow();
                }
            }

            sessionManager.OnSessionCreated += OnSessionCreated;
        }

        if (!found)
        {
            Log("VB-Audio Virtual Cable not found!");
            return;
        }

        Log("Monitoring started.");
        Application.Run();
    }

    static void OnSessionCreated(object sender, IAudioSessionControl newSession)
    {
        var session = new AudioSessionControl(newSession);
        string id = session.GetSessionIdentifier;

        if (!id.Contains("Discord", StringComparison.OrdinalIgnoreCase))
            return;

        Log("Discord session detected on Cable Input - waiting for active state...");

        var watcher = new SessionWatcher(svclPath, logPath);
        session.RegisterEventClient(watcher);

        // Mute immediately if already active at the moment of detection
        if (session.State == AudioSessionState.AudioSessionStateActive)
        {
            Log("Session already active on detection - muting immediately...");
            watcher.MuteNow();
        }
    }
}

class SessionWatcher : IAudioSessionEventsHandler
{
    private readonly string _svclPath;
    private readonly string _logPath;

    public SessionWatcher(string svclPath, string logPath)
    {
        _svclPath = svclPath;
        _logPath = logPath;
    }

    void Log(string msg) =>
        File.AppendAllText(_logPath, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {msg}{Environment.NewLine}");

    public void MuteNow()
    {
        Log("Discord went active - muting...");

        var proc = new Process();
        proc.StartInfo.FileName = _svclPath;
        proc.StartInfo.Arguments = "/scomma \"\"";
        proc.StartInfo.RedirectStandardOutput = true;
        proc.StartInfo.UseShellExecute = false;
        proc.StartInfo.CreateNoWindow = true;
        proc.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
        proc.Start();

        string output = proc.StandardOutput.ReadToEnd();
        proc.WaitForExit();

        foreach (var line in output.Split('\n'))
        {
            if (line.Contains("Discord") &&
                line.Contains("Render") &&
                line.Contains("VB-Audio Virtual Cable") &&
                line.Contains(",Active,No,"))
            {
                var match = System.Text.RegularExpressions.Regex.Match(
                    line, @"\{0\.0\.0\.00000000\}[^,]+");

                if (match.Success)
                {
                    var mute = new Process();
                    mute.StartInfo.FileName = _svclPath;
                    mute.StartInfo.Arguments = $"/Mute \"{match.Value}\"";
                    mute.StartInfo.UseShellExecute = false;
                    mute.StartInfo.CreateNoWindow = true;
                    mute.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                    mute.Start();
                    mute.WaitForExit();
                    Log("Muted.");
                }
            }
        }
    }

    public void OnStateChanged(AudioSessionState state)
    {
        if (state != AudioSessionState.AudioSessionStateActive) return;
        MuteNow();
    }

    public void OnVolumeChanged(float volume, bool isMuted) { }
    public void OnDisplayNameChanged(string displayName) { }
    public void OnIconPathChanged(string iconPath) { }
    public void OnChannelVolumeChanged(uint channelCount, IntPtr newVolumes, uint channelIndex) { }
    public void OnGroupingParamChanged(ref Guid groupingId) { }
    public void OnSessionDisconnected(AudioSessionDisconnectReason disconnectReason) { }
}