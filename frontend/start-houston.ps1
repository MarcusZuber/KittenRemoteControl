param([ValidateRange(1024,65535)][int]$Port = 8000)
$ErrorActionPreference = 'Stop'
Set-Location -LiteralPath $PSScriptRoot
Write-Host "Houston: http://localhost:$Port/houston.html"
Write-Host 'Keep this window open. In Houston use host localhost and datalink port 8085.'
foreach ($name in @('py', 'python3', 'python')) {
    $command = Get-Command $name -ErrorAction SilentlyContinue
    if ($command) {
        $prefix = @()
        if ($name -eq 'py') { $prefix = @('-3') }
        & $command.Source @prefix -m http.server $Port --bind 127.0.0.1
        exit $LASTEXITCODE
    }
}
throw 'Python 3 is required for HTTP hosting. Install Python or open houston.html directly in your browser.'
