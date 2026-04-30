using System;
using System.IO;

try
{
    using var game = new SpaceRace.Game1();
    game.Run();
}
catch (Exception ex)
{
    // WinExe doesn't show stderr in PowerShell — write any startup crash to disk
    // so we can read it after the fact.
    File.WriteAllText("crash.log", $"{DateTime.Now:O}\n{ex}\n");
    throw;
}
