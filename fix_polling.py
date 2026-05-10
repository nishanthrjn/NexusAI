content = open("src/NexusAI.Web/Components/Pages/Home.razor", "r", encoding="utf-8").read()

# Fix the polling to use InvokeAsync correctly
old = """    private void StartPolling(string sessionId)
    {
        _pollTimer?.Dispose();
        _pollTimer = new System.Timers.Timer(3000);
        _pollTimer.Elapsed += async (_, _) => await PollSession(sessionId);
        _pollTimer.Start();
    }"""

new = """    private void StartPolling(string sessionId)
    {
        _pollTimer?.Dispose();
        _pollTimer = new System.Timers.Timer(3000);
        _pollTimer.Elapsed += (_, _) => InvokeAsync(() => PollSession(sessionId));
        _pollTimer.AutoReset = true;
        _pollTimer.Start();
    }"""

content = content.replace(old, new)
open("src/NexusAI.Web/Components/Pages/Home.razor", "w", encoding="utf-8").write(content)
print("Done")
