# Starts the local mail catcher (smtp4dev) and the app. Stop with Ctrl+C.
#   App:          URL printed by dotnet run
#   Mail catcher: http://localhost:5080 (SMTP on localhost:2525)
$ErrorActionPreference = 'Stop'
Set-Location $PSScriptRoot

function Test-SmtpGreeting([int]$Port) {
    $client = [Net.Sockets.TcpClient]::new()
    try {
        if (-not $client.ConnectAsync('localhost', $Port).Wait(2000)) { return $false }
        $stream = $client.GetStream()
        $stream.ReadTimeout = 2000
        return ([IO.StreamReader]::new($stream)).ReadLine() -like '220*'
    }
    catch { return $false }
    finally { $client.Dispose() }
}

# Native commands don't throw on failure in Windows PowerShell; check the exit code.
dotnet tool restore | Out-Null
if ($LASTEXITCODE -ne 0) { throw "dotnet tool restore failed (exit code $LASTEXITCODE)." }

$smtpArgs = @('tool', 'run', 'smtp4dev', '--', '--urls=http://localhost:5080', '--smtpport=2525', '--imapport=', '--pop3port=', '--db=')
$smtp = Start-Process -FilePath 'dotnet' -ArgumentList $smtpArgs -PassThru -WindowStyle Hidden

try {
    # smtp4dev runs hidden and keeps running even when it can't bind SMTP, so wait for a real
    # SMTP greeting: another program squatting on the port won't send one.
    $deadline = (Get-Date).AddSeconds(30)
    while (-not (Test-SmtpGreeting -Port 2525)) {
        if ($smtp.HasExited -or (Get-Date) -gt $deadline) {
            throw 'The mail catcher did not start. Are ports 2525 and 5080 free?'
        }
        Start-Sleep -Milliseconds 500
    }
    Write-Host 'Mail catcher: http://localhost:5080'

    dotnet run --project src/Notifier.Web
}
finally {
    # Kill the whole process tree: `dotnet tool run` starts smtp4dev as a child process.
    if (-not $smtp.HasExited) { taskkill /PID $smtp.Id /T /F | Out-Null }
}
