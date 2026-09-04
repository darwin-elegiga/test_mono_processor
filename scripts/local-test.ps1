<#
.SYNOPSIS
    Local verification of the .NET 10 migration of OLS Processor: restore, build, tests and,
    optionally, a local run with every external dependency replaced by a local stand-in.

.DESCRIPTION
    Phases 1-3 always run: restore, build (Release) and unit tests of the API, the worker and the
    test project. They need the .NET 10 SDK and access to the NuGet feed only.

    With -WithLocalRun the script also starts SQL Server and RabbitMQ from the repository's
    docker-compose (the stand-ins for the shared environments), applies the database schema,
    writes appsettings.Local.json files that point everything at localhost, generates a local
    signing key, starts the API and the worker, calls every endpoint and checks the worker's
    side effects. Nothing tracked by git is modified; every generated file is either git-ignored
    or added to .git/info/exclude.

    The script never fixes anything. Every failure is written to the report for the team.

.PARAMETER NuGetConfig
    Optional path to a nuget.config OUTSIDE the repository (for example a copy of the REST
    Payments one for the JFrog feeds). When given, restore uses only that file and the other
    dotnet commands run with --no-restore. The repository nuget.config is not touched.

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File scripts/local-test.ps1

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File scripts/local-test.ps1 -NuGetConfig C:\ols-local\nuget.jfrog.config -WithLocalRun

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File scripts/local-test.ps1 -WithLocalRun -KeepRunning
#>
[CmdletBinding()]
param(
    [string]$NuGetConfig,
    [string]$Configuration = 'Release',
    [switch]$WithLocalRun,
    [switch]$Coverage,
    [switch]$KeepRunning,
    [switch]$StopInfra,
    [int]$ApiPort = 5000,
    [int]$SqlPort = 5633,
    [int]$RabbitPort = 5672,
    [int]$RabbitManagementPort = 15680,
    [string]$SaPassword = 'Your_password123',
    [string]$LocalRoot = (Join-Path $env:LOCALAPPDATA 'ols-local'),
    [string]$ReportDir
)

$ErrorActionPreference = 'Continue'
Set-StrictMode -Version 2

# ------------------------------------------------------------------ paths and report
$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$Stamp = Get-Date -Format 'yyyyMMddHHmmss'
if (-not $ReportDir) { $ReportDir = Join-Path $RepoRoot "local-test-results-$Stamp" }
New-Item -ItemType Directory -Force -Path $ReportDir | Out-Null
New-Item -ItemType Directory -Force -Path (Join-Path $ReportDir 'http') | Out-Null
$SummaryFile = Join-Path $ReportDir 'summary.md'

$ApiProject = Join-Path $RepoRoot 'src\VPay.Ols.Processor.Api\VPay.Ols.Processor.Api.csproj'
$WorkerProject = Join-Path $RepoRoot 'src\VPay.Ols.Processor.QueueConsumers\VPay.Ols.Processor.QueueConsumers.csproj'
$TestProject = Join-Path $RepoRoot 'tests\VPay.Ols.Processor.Tests\VPay.Ols.Processor.Tests.csproj'
$ApiDir = Split-Path $ApiProject -Parent
$WorkerDir = Split-Path $WorkerProject -Parent
$SsdtDir = Join-Path $RepoRoot 'src\database\VPay.Ols.Processor.Database.SSDT'
$BaseUrl = "http://localhost:$ApiPort"
$ComposeFiles = @('-f', 'docker-compose.yml', '-f', 'docker-compose.override.yml', '-f', 'docker-compose.local.yml')

$Results = New-Object System.Collections.Generic.List[object]
$script:ApiProcess = $null
$script:WorkerProcess = $null

$ExpectedWarningCodes = @('SYSLIB0021', 'CS8600', 'CS8601', 'CS8602', 'CS8603', 'CS8604', 'CS8618', 'CS8619', 'CS8625',
    'NU1701', 'NU1901', 'NU1902', 'NU1903', 'NU1904', 'NU1905', 'xUnit1030')

function Write-Log {
    param([string]$Message)
    $line = "[{0}] {1}" -f (Get-Date -Format 'HH:mm:ss'), $Message
    Write-Host $line
    Add-Content -Path (Join-Path $ReportDir 'run.log') -Value $line
}

function Add-Result {
    param([string]$Phase, [string]$Check, [ValidateSet('PASS', 'FAIL', 'WARN', 'INFO', 'SKIP')][string]$Outcome, [string]$Detail = '')
    $Results.Add([pscustomobject]@{ Phase = $Phase; Check = $Check; Outcome = $Outcome; Detail = $Detail })
    $color = switch ($Outcome) { 'PASS' { 'Green' } 'FAIL' { 'Red' } 'WARN' { 'Yellow' } default { 'Gray' } }
    Write-Host ("[{0}] {1} - {2}" -f $Outcome, $Check, $Detail) -ForegroundColor $color
    Add-Content -Path (Join-Path $ReportDir 'run.log') -Value ("[{0}] {1} - {2}" -f $Outcome, $Check, $Detail)
}

function Quote-Arg {
    param([string]$Value)
    return '"' + $Value + '"'
}

function Invoke-Logged {
    # Runs a native command with stdout and stderr captured into one log file and returns the exit
    # code. Start-Process is used because Windows PowerShell 5.1 cannot redirect native stderr cleanly.
    param([string]$FilePath, [string[]]$Arguments, [string]$LogFile, [string]$WorkingDirectory = $RepoRoot)
    Write-Log ("> {0} {1}" -f $FilePath, ($Arguments -join ' '))
    $out = "$LogFile.out"
    $err = "$LogFile.err"
    $p = Start-Process -FilePath $FilePath -ArgumentList $Arguments -WorkingDirectory $WorkingDirectory -NoNewWindow -Wait -PassThru `
        -RedirectStandardOutput $out -RedirectStandardError $err
    $content = @()
    if (Test-Path $out) { $content += Get-Content $out }
    if (Test-Path $err) { $content += Get-Content $err }
    Set-Content -Path $LogFile -Value $content
    Remove-Item $out, $err -ErrorAction SilentlyContinue
    return $p.ExitCode
}

function Get-WarningSummary {
    param([string]$LogFile)
    $counts = @{}
    if (-not (Test-Path $LogFile)) { return $counts }
    foreach ($line in Get-Content $LogFile) {
        if ($line -match 'warning ([A-Za-z]+\d+)') {
            $code = $Matches[1]
            if ($counts.ContainsKey($code)) { $counts[$code]++ } else { $counts[$code] = 1 }
        }
    }
    return $counts
}

function Report-Warnings {
    param([string]$Phase, [string]$Label, [string]$LogFile)
    $counts = Get-WarningSummary $LogFile
    if ($counts.Count -eq 0) {
        Add-Result $Phase "$Label warnings" 'PASS' 'no warnings'
        return
    }
    $unexpected = @($counts.Keys | Where-Object { $ExpectedWarningCodes -notcontains $_ })
    $text = ($counts.GetEnumerator() | Sort-Object Name | ForEach-Object { "{0} x{1}" -f $_.Name, $_.Value }) -join ', '
    if ($unexpected.Count -gt 0) {
        Add-Result $Phase "$Label warnings" 'WARN' ("unexpected codes: {0} | all: {1}" -f ($unexpected -join ', '), $text)
    }
    else {
        Add-Result $Phase "$Label warnings" 'INFO' "only expected codes: $text"
    }
}

function Add-GitExclude {
    param([string[]]$Patterns)
    $exclude = Join-Path $RepoRoot '.git\info\exclude'
    if (-not (Test-Path (Split-Path $exclude -Parent))) { return }
    $existing = @()
    if (Test-Path $exclude) { $existing = @(Get-Content $exclude) }
    foreach ($p in $Patterns) {
        if ($existing -notcontains $p) { Add-Content -Path $exclude -Value $p }
    }
}

function Invoke-Http {
    # Uses curl.exe (shipped with Windows 10+) so multipart uploads work on Windows PowerShell 5.1.
    param([string]$Name, [string]$Method, [string]$Path, [string[]]$ExtraArgs = @())
    $headers = Join-Path $ReportDir "http\$Name.headers.txt"
    $body = Join-Path $ReportDir "http\$Name.body.txt"
    $stderr = Join-Path $ReportDir "http\$Name.curl.txt"
    $curlArgs = @('-s', '-S', '--max-time', '60', '-o', $body, '-D', $headers, '--stderr', $stderr, '-w', '%{http_code}')
    if ($Method -eq 'HEAD') { $curlArgs += '-I' } else { $curlArgs += @('-X', $Method) }
    $curlArgs += $ExtraArgs
    $curlArgs += "$BaseUrl$Path"
    $status = & curl.exe @curlArgs
    if (-not $status) { $status = '000' }
    $bodyText = ''
    if (Test-Path $body) { $bodyText = [IO.File]::ReadAllText($body) }
    $headerText = ''
    if (Test-Path $headers) { $headerText = [IO.File]::ReadAllText($headers) }
    return [pscustomobject]@{ Status = "$status".Trim(); Body = $bodyText; Headers = $headerText }
}

function Get-HeaderValue {
    param([string]$HeaderText, [string]$Name)
    foreach ($line in ($HeaderText -split "`r?`n")) {
        if ($line -match ('^' + [regex]::Escape($Name) + ':\s*(.*)$')) { return $Matches[1].Trim() }
    }
    return ''
}

function Get-Excerpt {
    param([string]$Text, [int]$Length = 300)
    if (-not $Text) { return '' }
    return ($Text.Substring(0, [Math]::Min($Length, $Text.Length)) -replace "`r?`n", ' ')
}

function Test-Http {
    param([string]$Check, [object]$Response, [string]$ExpectedStatus, [string]$BodyMustContain = '')
    if ($Response.Status -ne $ExpectedStatus) {
        Add-Result 'Smoke' $Check 'FAIL' ("expected {0}, got {1} | body: {2}" -f $ExpectedStatus, $Response.Status, (Get-Excerpt $Response.Body))
        return $false
    }
    if ($BodyMustContain -and ($Response.Body -notmatch [regex]::Escape($BodyMustContain))) {
        Add-Result 'Smoke' $Check 'FAIL' ("status {0} but body does not contain '{1}'" -f $Response.Status, $BodyMustContain)
        return $false
    }
    Add-Result 'Smoke' $Check 'PASS' "status $($Response.Status)"
    return $true
}

function Invoke-SqlInContainer {
    param([string]$Query, [string]$Database = 'master')
    $out = & docker exec ols-sqlserver /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P $SaPassword -d $Database -b -h -1 -W -Q $Query 2>&1 | Out-String
    return [pscustomobject]@{ ExitCode = $LASTEXITCODE; Output = $out.Trim() }
}

function Wait-Until {
    param([scriptblock]$Condition, [int]$TimeoutSeconds, [string]$What)
    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        if (& $Condition) { return $true }
        Start-Sleep -Seconds 3
    }
    Write-Log "timed out after $TimeoutSeconds s waiting for $What"
    return $false
}

function Write-SampleFiles {
    param([string]$Dir)
    New-Item -ItemType Directory -Force -Path $Dir | Out-Null
    $today = Get-Date -Format 'MMddyyyy'
    $utf8 = New-Object System.Text.UTF8Encoding($false)

    $auth = Join-Path $Dir "authorizations_$Stamp.txt"
    [IO.File]::WriteAllLines($auth, [string[]]@(
        "HEADER|STONEEAGLE|AUTHORIZED|$today|$today|$today",
        "5555930000003147|01122022 12:28:20|840|X|2100-00-00|0.10|990011|SE|||||1|546893",
        "5555930000003237|01122022 00:00:00|840|X|2100-00-00|0.10|990012|MS|BOGUSMD|BOGUSMD|6010|US|2|532086",
        "5555930000004152|01222022 01:12:22|840|X|2100-00-00|0.10|990013|SE|||||3|528972",
        "5555930000009999|01222022 01:12:22|840|X|2100-00-00|0.10|990014|SE|||||4|123456",
        "TRAILER|4"), $utf8)

    $posted = Join-Path $Dir "posted_transactions_$Stamp.txt"
    [IO.File]::WriteAllLines($posted, [string[]]@(
        "HEADER|STONEEAGLE|POSTED|$today|$today|$today|2",
        "5555930000003147|01122022|2200-2S-0000|0.10|+|840|990011|01122022 18:56:30|SE|||||||||1|546893",
        "5555930000003237|01122022|2200-2S-0000|0.10|-|840|990012|01122022 18:56:30|MS|BOGUSMD|BOGUSMD|6010|US|||||2|532086",
        "5555930000004332|01122022|2200-2S-0000|0.20|-|840|990019|01122022 18:56:30|SE||||||||||123456",
        "TRAILER|3"), $utf8)

    $nonfin = Join-Path $Dir "non_financial_$Stamp.txt"
    [IO.File]::WriteAllLines($nonfin, [string[]]@(
        "HEADER|STONEEAGLE|NON-FINANCIAL|$today|$today|$today|3",
        "5468930000001234|12102021|01102022|||The Stone Eagle Group||111 W. Spring Valley Road #100||Richardson|TX|750814016|US|9725551212||PYNNN|0.10|+||||||||0.10|||||||1|546893",
        "5468930000004321|12102021|01102022|||The Stone Eagle Group||111 W. Spring Valley Road #100||Richardson|TX|750814016|US|9725551212||PYNNN|0.10|-||||||||0.10|||||||2|546893",
        "5468930000004444|12102021|01102022|||The Stone Eagle Group||111 W. Spring Valley Road #100||Richardson|TX|750814016|US|9725551212||PYNNN|0.10|+||||||||0.10||||||||546893",
        "TRAILER|3"), $utf8)

    return [pscustomobject]@{ Authorizations = $auth; Posted = $posted; NonFinancial = $nonfin }
}

function Write-LocalSettings {
    # Writes appsettings.Local.json (git-ignored by *.Local.*) only when the file does not exist.
    param([string]$ProjectDir, [System.Collections.Specialized.OrderedDictionary]$Settings)
    $path = Join-Path $ProjectDir 'appsettings.Local.json'
    $label = "$(Split-Path $ProjectDir -Leaf) appsettings.Local.json"
    if (Test-Path $path) {
        Add-Result 'Config' $label 'INFO' 'existing file kept; delete it to regenerate'
        return
    }
    $json = $Settings | ConvertTo-Json -Depth 6
    [IO.File]::WriteAllText($path, $json, (New-Object System.Text.UTF8Encoding($false)))
    Add-Result 'Config' $label 'PASS' "written to $path"
}

function Stop-App {
    param($Process, [string]$Name)
    if ($null -eq $Process) { return }
    try {
        if (-not $Process.HasExited) {
            Stop-Process -Id $Process.Id -Force -ErrorAction SilentlyContinue
            Write-Log "$Name stopped (pid $($Process.Id))"
        }
    }
    catch { Write-Log "could not stop ${Name}: $($_.Exception.Message)" }
}

function Get-FirstNumber {
    param([string]$Text)
    if ($Text -match '(\d+)') { return [int]$Matches[1] }
    return 0
}

function Write-Summary {
    $lines = @()
    $lines += "# OLS Processor local test - $Stamp"
    $lines += ''
    $lines += "Repository: $RepoRoot"
    $lines += "Configuration: $Configuration"
    $lines += "NuGet config override: $(if ($NuGetConfig) { $NuGetConfig } else { '(repository nuget.config)' })"
    $lines += "Local run: $WithLocalRun"
    $lines += ''
    $lines += '| Phase | Check | Outcome | Detail |'
    $lines += '|---|---|---|---|'
    foreach ($r in $Results) {
        $detail = ($r.Detail -replace '\|', '\|') -replace "`r?`n", ' '
        $lines += "| $($r.Phase) | $($r.Check) | $($r.Outcome) | $detail |"
    }
    $pass = @($Results | Where-Object { $_.Outcome -eq 'PASS' }).Count
    $fail = @($Results | Where-Object { $_.Outcome -eq 'FAIL' }).Count
    $warn = @($Results | Where-Object { $_.Outcome -eq 'WARN' }).Count
    $lines += ''
    $lines += "Totals: $pass PASS, $fail FAIL, $warn WARN"
    $lines += ''
    $lines += 'Attach this folder to the report. Do not fix anything; send the findings back to the team.'
    [IO.File]::WriteAllLines($SummaryFile, [string[]]$lines)
    Write-Host ''
    Write-Host "Totals: $pass PASS, $fail FAIL, $warn WARN" -ForegroundColor Cyan
    Write-Host "Report: $SummaryFile" -ForegroundColor Cyan
    return $fail
}

# ------------------------------------------------------------------ phase 0: preflight
Write-Log "OLS Processor local test - repository $RepoRoot"
Add-GitExclude @('local-test-results-*/', 'smoke-results-*/', 'docker-compose.local.yml', 'keys/id_rsa_localonly', 'packages-*.txt')

$dotnetVersion = (& dotnet --version 2>&1 | Out-String).Trim()
if ($dotnetVersion -match '^10\.') {
    Add-Result 'Preflight' 'dotnet SDK' 'PASS' $dotnetVersion
}
else {
    Add-Result 'Preflight' 'dotnet SDK' 'FAIL' "expected a 10.x SDK, got '$dotnetVersion' (global.json requires 10.0.100 or newer)"
    Write-Summary | Out-Null
    exit 2
}
Invoke-Logged 'dotnet' @('--info') (Join-Path $ReportDir 'dotnet-info.txt') | Out-Null

$branch = (& git -C $RepoRoot rev-parse --abbrev-ref HEAD 2>&1 | Out-String).Trim()
Add-Result 'Preflight' 'git branch' 'INFO' $branch

if ($NuGetConfig) {
    if (Test-Path $NuGetConfig) {
        Add-Result 'Preflight' 'NuGet config override' 'INFO' $NuGetConfig
    }
    else {
        Add-Result 'Preflight' 'NuGet config override' 'FAIL' "file not found: $NuGetConfig"
        Write-Summary | Out-Null
        exit 2
    }
}

$projects = @(
    @{ Name = 'api'; Project = $ApiProject },
    @{ Name = 'worker'; Project = $WorkerProject },
    @{ Name = 'tests'; Project = $TestProject })

# ------------------------------------------------------------------ phase 1: restore
$restoreFailed = $false
foreach ($entry in $projects) {
    $log = Join-Path $ReportDir "restore-$($entry.Name).log"
    $args = @('restore', (Quote-Arg $entry.Project), '--disable-build-servers')
    if ($NuGetConfig) { $args += @('--configfile', (Quote-Arg $NuGetConfig)) }
    $code = Invoke-Logged 'dotnet' $args $log
    if ($code -eq 0) {
        Add-Result 'Restore' $entry.Name 'PASS' "exit 0"
    }
    else {
        $restoreFailed = $true
        $errors = @(Get-Content $log | Where-Object { $_ -match 'error NU\d+|Unable to load the service index|401|403' } | Select-Object -First 5) -join ' / '
        Add-Result 'Restore' $entry.Name 'FAIL' "exit $code | $errors | see $log"
    }
    Report-Warnings 'Restore' $entry.Name $log
}
if ($restoreFailed) {
    Add-Result 'Build' 'all projects' 'SKIP' 'restore failed'
    Add-Result 'Test' 'unit tests' 'SKIP' 'restore failed'
    Write-Summary | Out-Null
    exit 1
}
foreach ($entry in $projects[0..1]) {
    $pkgLog = Join-Path $ReportDir "packages-$($entry.Name).txt"
    Invoke-Logged 'dotnet' @('list', (Quote-Arg $entry.Project), 'package', '--include-transitive', '--no-restore') $pkgLog | Out-Null
    $suspects = @(Get-Content $pkgLog | Where-Object { $_ -match 'Microsoft\.AspNetCore\.Mvc\.Versioning|MediatR\.Extensions\.Microsoft|>\s*MediatR\s+.*\s(10|11)\.\d' })
    if ($suspects.Count -gt 0) {
        Add-Result 'Restore' "$($entry.Name) transitive packages" 'WARN' ("legacy packages still in the graph: " + ($suspects -join ' / '))
    }
    else {
        Add-Result 'Restore' "$($entry.Name) transitive packages" 'PASS' "no legacy versioning or MediatR 10/11 packages (see $pkgLog)"
    }
}

# ------------------------------------------------------------------ phase 2: build
$buildFailed = $false
foreach ($entry in $projects) {
    $log = Join-Path $ReportDir "build-$($entry.Name).log"
    $code = Invoke-Logged 'dotnet' @('build', (Quote-Arg $entry.Project), '-c', $Configuration, '--no-restore', '--disable-build-servers') $log
    if ($code -eq 0) {
        Add-Result 'Build' $entry.Name 'PASS' 'exit 0'
    }
    else {
        $buildFailed = $true
        $errors = @(Get-Content $log | Where-Object { $_ -match ' error [A-Z]+\d+' } | Select-Object -Unique -First 8) -join ' / '
        Add-Result 'Build' $entry.Name 'FAIL' "exit $code | $errors | see $log"
    }
    Report-Warnings 'Build' $entry.Name $log
}
if ($buildFailed) {
    Add-Result 'Test' 'unit tests' 'SKIP' 'build failed'
    Write-Summary | Out-Null
    exit 1
}

# ------------------------------------------------------------------ phase 3: tests
$testLog = Join-Path $ReportDir 'test.log'
$trx = Join-Path $ReportDir 'tests.trx'
$testArgs = @('test', (Quote-Arg $TestProject), '-c', $Configuration, '--no-build', '--logger', (Quote-Arg "trx;LogFileName=$trx"))
if ($Coverage) { $testArgs += @('--collect:"XPlat Code Coverage"', '--results-directory', (Quote-Arg (Join-Path $ReportDir 'coverage'))) }
$code = Invoke-Logged 'dotnet' $testArgs $testLog
$testText = Get-Content $testLog | Out-String
if ($testText -match 'Failed:\s+(\d+),\s+Passed:\s+(\d+),\s+Skipped:\s+(\d+),\s+Total:\s+(\d+)') {
    $summary = "failed $($Matches[1]), passed $($Matches[2]), skipped $($Matches[3]), total $($Matches[4])"
    if ($code -eq 0 -and [int]$Matches[1] -eq 0) {
        Add-Result 'Test' 'unit tests' 'PASS' $summary
    }
    else {
        $failing = @(Get-Content $testLog | Where-Object { $_ -match '^\s+Failed ' } | Select-Object -First 10) -join ' / '
        Add-Result 'Test' 'unit tests' 'FAIL' "$summary | $failing | see $testLog"
    }
}
else {
    Add-Result 'Test' 'unit tests' 'FAIL' "exit $code, could not parse results (see $testLog)"
}
if ($Coverage) {
    $coverageFiles = @(Get-ChildItem -Path (Join-Path $ReportDir 'coverage') -Recurse -Filter 'coverage.cobertura.xml' -ErrorAction SilentlyContinue)
    if ($coverageFiles.Count -gt 0 -and $coverageFiles[0].Length -gt 200) {
        Add-Result 'Test' 'coverage file' 'PASS' $coverageFiles[0].FullName
    }
    else {
        Add-Result 'Test' 'coverage file' 'WARN' 'no coverage.cobertura.xml produced (coverlet.collector on .NET 10 unverified)'
    }
}

if (-not $WithLocalRun) {
    Add-Result 'LocalRun' 'infrastructure, API, worker, smoke' 'SKIP' 'run again with -WithLocalRun'
    $failCount = Write-Summary
    if ($failCount -gt 0) { exit 1 } else { exit 0 }
}

# ------------------------------------------------------------------ phase 4: local stand-ins for the external services
try {
    $dockerVersion = (& docker --version 2>&1 | Out-String).Trim()
    if ($LASTEXITCODE -ne 0) {
        Add-Result 'Infra' 'docker' 'FAIL' 'docker is not available'
        throw 'docker missing'
    }
    Add-Result 'Infra' 'docker' 'INFO' $dockerVersion

    $composeLocal = Join-Path $RepoRoot 'docker-compose.local.yml'
    if (-not (Test-Path $composeLocal)) {
        $composeText = "services:`n  ols-processor-rabbitmq:`n    ports:`n      - `"${RabbitPort}:5672`"`n"
        [IO.File]::WriteAllText($composeLocal, $composeText, (New-Object System.Text.UTF8Encoding($false)))
        Add-Result 'Infra' 'docker-compose.local.yml' 'PASS' 'written (untracked, publishes the RabbitMQ port)'
    }
    $code = Invoke-Logged 'docker' (@('compose') + $ComposeFiles + @('up', '-d', 'ols-sqlserver', 'ols-processor-rabbitmq')) (Join-Path $ReportDir 'compose-up.log')
    if ($code -ne 0) {
        Add-Result 'Infra' 'compose up' 'FAIL' "exit $code (see compose-up.log)"
        throw 'compose failed'
    }
    Add-Result 'Infra' 'compose up' 'PASS' 'ols-sqlserver and ols-processor-rabbitmq started'

    $sqlReady = Wait-Until { (Invoke-SqlInContainer 'SELECT 1').ExitCode -eq 0 } 180 'SQL Server'
    if (-not $sqlReady) { Add-Result 'Infra' 'SQL Server ready' 'FAIL' 'sqlcmd inside ols-sqlserver did not answer in 180 s'; throw 'sql not ready' }
    Add-Result 'Infra' 'SQL Server ready' 'PASS' "localhost,$SqlPort"

    $rabbitReady = Wait-Until {
        $r = & curl.exe -s -o NUL -w '%{http_code}' -u rabbitmq:rabbitmq "http://localhost:$RabbitManagementPort/api/vhosts/Nacha"
        "$r".Trim() -eq '200'
    } 120 'RabbitMQ management API'
    if (-not $rabbitReady) { Add-Result 'Infra' 'RabbitMQ ready' 'FAIL' 'management API did not answer or vhost Nacha is missing'; throw 'rabbit not ready' }
    Add-Result 'Infra' 'RabbitMQ ready' 'PASS' "localhost:$RabbitPort, vhost Nacha loaded from nacha_definitions.json"

    # schema (the compose start-up script creates the database asynchronously; keep everything idempotent)
    $r = Invoke-SqlInContainer "IF DB_ID('SE_OLS') IS NULL CREATE DATABASE SE_OLS"
    if ($r.ExitCode -ne 0) { Add-Result 'Infra' 'database SE_OLS' 'FAIL' $r.Output; throw 'db create failed' }
    Add-Result 'Infra' 'database SE_OLS' 'PASS' 'exists'

    $sqlStage = Join-Path $ReportDir 'sql'
    New-Item -ItemType Directory -Force -Path $sqlStage | Out-Null
    $scripts = @(
        'VPAY01.sql',
        'dbo\Types\udt_TransactionIdLookup.sql',
        'dbo\Tables\OlsFile.sql',
        'dbo\Tables\SEWCPS_CPSTMF.sql',
        'dbo\Tables\TriggeredTranLog.sql',
        'dbo\Stored Procedures\usp_OlsFile_Insert.sql',
        'VPAY01\StoredProcedures\usp_ListClient_ByTransactionIds.sql',
        'dbo\Stored Procedures\sp_select_unacked_transactions_restriced.sql',
        'dbo\Stored Procedures\sp_update_ack_status.sql',
        'dbo\Stored Procedures\sp_update_send_status.sql')
    $i = 0
    $staged = @()
    foreach ($s in $scripts) {
        $src = Join-Path $SsdtDir $s
        if (-not (Test-Path $src)) { Add-Result 'Schema' $s 'WARN' 'script not found in repository'; continue }
        $i++
        $name = '{0:00}_{1}' -f $i, (Split-Path $s -Leaf)
        $text = Get-Content -Raw -Path $src
        [IO.File]::WriteAllText((Join-Path $sqlStage $name), $text, (New-Object System.Text.UTF8Encoding($false)))
        $staged += @{ Name = $name; Source = $s }
    }
    & docker exec ols-sqlserver rm -rf /tmp/olssql 2>&1 | Out-Null
    & docker cp "$sqlStage" ols-sqlserver:/tmp/olssql 2>&1 | Out-Null
    foreach ($st in $staged) {
        $out = & docker exec ols-sqlserver /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P $SaPassword -d SE_OLS -b -i "/tmp/olssql/$($st.Name)" 2>&1 | Out-String
        if ($LASTEXITCODE -eq 0) {
            Add-Result 'Schema' $st.Source 'PASS' 'applied'
        }
        elseif ($out -match 'already exists|There is already an object') {
            Add-Result 'Schema' $st.Source 'INFO' 'already present'
        }
        else {
            $required = $st.Source -notmatch '^dbo\\Stored Procedures\\sp_'
            $outcome = 'WARN'
            if ($required) { $outcome = 'FAIL' }
            Add-Result 'Schema' $st.Source $outcome (Get-Excerpt $out 400)
        }
    }
    $r = Invoke-SqlInContainer "IF NOT EXISTS (SELECT 1 FROM VPAY01.SEWCPS_CPSTMF WHERE TMTXID IN (1,2,3)) INSERT INTO VPAY01.SEWCPS_CPSTMF (TMCLIC,TMTXID,REPDATE,RRN) VALUES ('ABC',1,GETDATE(),1),('DEF',2,GETDATE(),2),('GHI',3,GETDATE(),3)" 'SE_OLS'
    if ($r.ExitCode -eq 0) { Add-Result 'Schema' 'test rows in VPAY01.SEWCPS_CPSTMF' 'PASS' 'transaction ids 1, 2, 3 map to ABC, DEF, GHI' }
    else { Add-Result 'Schema' 'test rows in VPAY01.SEWCPS_CPSTMF' 'FAIL' $r.Output }

    # ------------------------------------------------------------------ phase 5: local configuration
    $localRootFwd = $LocalRoot -replace '\\', '/'
    foreach ($sub in @('SRVFS\ols\AuthorizationFiles\Optum', 'SRVFS\ols\NonFinancialFiles\Optum', 'SRVFS\ols\PostedTransactionFiles\Optum', 'SRVFS\filetransfer\optum_ols')) {
        New-Item -ItemType Directory -Force -Path (Join-Path $LocalRoot $sub) | Out-Null
    }

    $keyPath = Join-Path $RepoRoot 'keys\id_rsa_localonly'
    if (-not (Test-Path $keyPath)) {
        if (Get-Command ssh-keygen -ErrorAction SilentlyContinue) {
            & ssh-keygen -q -t rsa -b 2048 -m PEM -f $keyPath -N '""' 2>&1 | Out-Null
            if (Test-Path $keyPath) { Add-Result 'Config' 'signing key' 'PASS' "throw-away RSA key generated at $keyPath (never commit it)" }
            else { Add-Result 'Config' 'signing key' 'FAIL' 'ssh-keygen did not produce keys/id_rsa_localonly' }
        }
        else {
            Add-Result 'Config' 'signing key' 'FAIL' 'keys/id_rsa_localonly missing and ssh-keygen not available; copy the key from y:/vcard/RSA_Keys/vpay-ols-processor/keys'
        }
    }
    else {
        Add-Result 'Config' 'signing key' 'INFO' "using existing $keyPath"
    }
    $keyPathFwd = $keyPath -replace '\\', '/'

    $fileSettings = [ordered]@{
        AuthorizationFileSettings      = [ordered]@{ WorkingDirectory = "$localRootFwd/SRVFS/ols/AuthorizationFiles"; OutputDirectory = "$localRootFwd/SRVFS/ols/AuthorizationFiles/Optum" }
        NonFinancialFileSettings       = [ordered]@{ WorkingDirectory = "$localRootFwd/SRVFS/ols/NonFinancialFiles"; OutputDirectory = "$localRootFwd/SRVFS/ols/NonFinancialFiles/Optum" }
        PostedTransactionsFileSettings = [ordered]@{ WorkingDirectory = "$localRootFwd/SRVFS/ols/PostedTransactionFiles"; OutputDirectory = "$localRootFwd/SRVFS/ols/PostedTransactionFiles/Optum" }
    }
    $apiSettings = [ordered]@{
        Logging           = [ordered]@{ GELF = [ordered]@{ Host = '127.0.0.1' } }
        OlsDatabase       = [ordered]@{ Hostname = "localhost,$SqlPort"; Username = 'sa'; Password = $SaPassword }
        OlsProcessorQueue = [ordered]@{ hostnames = @('localhost'); port = $RabbitPort }
    }
    foreach ($k in @($fileSettings.Keys)) { $apiSettings[$k] = $fileSettings[$k] }
    Write-LocalSettings $ApiDir $apiSettings

    $workerSettings = [ordered]@{
        Logging                        = [ordered]@{ GELF = [ordered]@{ Host = '127.0.0.1' } }
        OlsDatabase                    = [ordered]@{ Hostname = "localhost,$SqlPort"; Username = 'sa'; Password = $SaPassword }
        OlsProcessorQueue              = [ordered]@{ hostnames = @('localhost'); port = $RabbitPort }
        FileTransferServiceRabbitQueue = [ordered]@{ hostnames = @('localhost'); port = $RabbitPort }
        FileTransferServiceSettings    = [ordered]@{ FileTransferServiceFolderPath = "$localRootFwd/SRVFS/filetransfer"; FileTransferServiceFolderSubPath = 'optum_ols' }
        SigningServiceOptions          = [ordered]@{ PrivateKeyPath = $keyPathFwd }
    }
    foreach ($k in @($fileSettings.Keys)) { $workerSettings[$k] = $fileSettings[$k] }
    Write-LocalSettings $WorkerDir $workerSettings

    # ------------------------------------------------------------------ phase 6: start API and worker
    $env:ASPNETCORE_ENVIRONMENT = 'LocalDevelopment'
    $env:DOTNET_ENVIRONMENT = 'LocalDevelopment'
    $apiDll = Join-Path $ApiDir "bin\$Configuration\net10.0\VPay.Ols.Processor.Api.dll"
    $workerDll = Join-Path $WorkerDir "bin\$Configuration\net10.0\VPay.Ols.Processor.QueueConsumers.dll"

    $script:ApiProcess = Start-Process -FilePath 'dotnet' -ArgumentList @((Quote-Arg $apiDll), '--urls', $BaseUrl) -WorkingDirectory $ApiDir -PassThru -NoNewWindow `
        -RedirectStandardOutput (Join-Path $ReportDir 'api.out.log') -RedirectStandardError (Join-Path $ReportDir 'api.err.log')
    $apiUp = Wait-Until {
        if ($script:ApiProcess.HasExited) { return $true }
        $r = & curl.exe -s -o NUL -w '%{http_code}' "$BaseUrl/api/about"
        "$r".Trim() -eq '200'
    } 120 'API start-up'
    if ($script:ApiProcess.HasExited -or -not $apiUp) {
        $err = @(Get-Content (Join-Path $ReportDir 'api.out.log') -ErrorAction SilentlyContinue | Select-Object -Last 30) -join ' / '
        Add-Result 'Run' 'API start-up' 'FAIL' "did not answer on $BaseUrl | $err"
        throw 'api failed'
    }
    Add-Result 'Run' 'API start-up' 'PASS' "$BaseUrl (pid $($script:ApiProcess.Id))"

    $script:WorkerProcess = Start-Process -FilePath 'dotnet' -ArgumentList @((Quote-Arg $workerDll)) -WorkingDirectory $WorkerDir -PassThru -NoNewWindow `
        -RedirectStandardOutput (Join-Path $ReportDir 'worker.out.log') -RedirectStandardError (Join-Path $ReportDir 'worker.err.log')
    Start-Sleep -Seconds 20
    $workerLog = (Get-Content (Join-Path $ReportDir 'worker.out.log') -ErrorAction SilentlyContinue | Out-String)
    if ($script:WorkerProcess.HasExited) {
        $tail = @(($workerLog -split "`r?`n") | Select-Object -Last 20) -join ' / '
        Add-Result 'Run' 'worker start-up' 'FAIL' ("process exited with code {0} | {1}" -f $script:WorkerProcess.ExitCode, $tail)
        throw 'worker failed'
    }
    if ($workerLog -match 'Unhandled exception|TypeLoadException|MissingMethodException|BrokerUnreachable') {
        Add-Result 'Run' 'worker start-up' 'FAIL' 'exception in worker.out.log'
        throw 'worker failed'
    }
    Add-Result 'Run' 'worker start-up' 'PASS' "running (pid $($script:WorkerProcess.Id)); see worker.out.log"

    # ------------------------------------------------------------------ phase 7: endpoints
    $samples = Write-SampleFiles (Join-Path $ReportDir 'samples')

    $r = Invoke-Http '01-about' 'GET' '/api/about' @('-H', 'Accept: application/json')
    Test-Http 'GET /api/about' $r '200' 'buildName' | Out-Null
    $ct = Get-HeaderValue $r.Headers 'Content-Type'
    if ($ct -match 'application/json') { Add-Result 'Smoke' 'about content type' 'PASS' $ct } else { Add-Result 'Smoke' 'about content type' 'FAIL' "'$ct'" }
    $ver = Get-HeaderValue $r.Headers 'api-supported-versions'
    if ($ver -match '1\.0') { Add-Result 'Smoke' 'api-supported-versions header' 'PASS' $ver } else { Add-Result 'Smoke' 'api-supported-versions header' 'FAIL' "missing or unexpected: '$ver'" }
    Add-Result 'Smoke' 'about body' 'INFO' (Get-Excerpt $r.Body)

    $r = Invoke-Http '02-about-head' 'HEAD' '/api/about'
    Add-Result 'Smoke' 'HEAD /api/about' 'INFO' "status $($r.Status)"

    $r = Invoke-Http '03-health' 'GET' '/health'
    Test-Http 'GET /health (local SQL Server)' $r '200' | Out-Null
    Add-Result 'Smoke' 'health body' 'INFO' (Get-Excerpt $r.Body)

    $r = Invoke-Http '04-swagger-ui' 'GET' '/swagger/index.html'
    Test-Http 'GET /swagger/index.html' $r '200' 'swagger' | Out-Null

    $r = Invoke-Http '05-swagger-json' 'GET' '/swagger/v1.0/swagger.json'
    if (Test-Http 'GET /swagger/v1.0/swagger.json' $r '200' 'OLS Processor API') {
        foreach ($p in @('/api/about', '/api/files/ingest-authorizations', '/api/files/ingest-posted-transactions', '/api/files/ingest-non-financial')) {
            if ($r.Body -match [regex]::Escape("`"$p`"")) { Add-Result 'Smoke' "swagger documents $p" 'PASS' '' } else { Add-Result 'Smoke' "swagger documents $p" 'FAIL' 'path missing' }
        }
        $healthDocumented = 'no'
        if ($r.Body -match '"/health"') { $healthDocumented = 'yes' }
        Add-Result 'Smoke' 'swagger documents /health' 'INFO' $healthDocumented
    }

    $r = Invoke-Http '06-ingest-authorizations' 'POST' '/api/files/ingest-authorizations' @('-F', "file=@$($samples.Authorizations)")
    Test-Http 'POST /api/files/ingest-authorizations' $r '202' | Out-Null
    $ver = Get-HeaderValue $r.Headers 'api-supported-versions'
    if ($ver -match '1\.0') { Add-Result 'Smoke' 'ingest reports api-supported-versions' 'PASS' $ver } else { Add-Result 'Smoke' 'ingest reports api-supported-versions' 'FAIL' "'$ver'" }

    $r = Invoke-Http '07-ingest-posted-transactions' 'POST' '/api/files/ingest-posted-transactions' @('-F', "file=@$($samples.Posted)")
    Test-Http 'POST /api/files/ingest-posted-transactions' $r '202' | Out-Null

    $r = Invoke-Http '08-ingest-non-financial' 'POST' '/api/files/ingest-non-financial' @('-F', "file=@$($samples.NonFinancial)")
    Test-Http 'POST /api/files/ingest-non-financial' $r '202' | Out-Null

    $r = Invoke-Http '09-about-versioned' 'GET' '/api/about' @('-H', 'api-version: 1.0')
    Test-Http 'GET /api/about with api-version: 1.0' $r '200' 'buildName' | Out-Null

    $r = Invoke-Http '10-ingest-missing-file' 'POST' '/api/files/ingest-authorizations' @('-F', 'notTheFile=x')
    Test-Http 'POST without file field (model validation)' $r '400' | Out-Null
    Add-Result 'Smoke' 'missing-file content type' 'INFO' (Get-HeaderValue $r.Headers 'Content-Type')

    $r = Invoke-Http '11-ingest-wrong-content-type' 'POST' '/api/files/ingest-authorizations' @('-H', 'Content-Type: application/json', '--data', '{}')
    Test-Http 'POST with JSON body (inferred multipart Consumes)' $r '415' | Out-Null

    $r = Invoke-Http '12-not-found' 'GET' '/api/does-not-exist'
    Test-Http 'GET /api/does-not-exist' $r '404' | Out-Null
    Add-Result 'Smoke' '404 content type (Hellang ProblemDetails)' 'INFO' (Get-HeaderValue $r.Headers 'Content-Type')

    $r = Invoke-Http '13-method-not-allowed' 'DELETE' '/api/about'
    Test-Http 'DELETE /api/about' $r '405' | Out-Null

    # ------------------------------------------------------------------ phase 8: worker side effects
    $olsRoot = Join-Path $LocalRoot 'SRVFS\ols'
    $outputsFound = Wait-Until {
        @(Get-ChildItem -Path $olsRoot -Recurse -Filter '*_Optum_*_op_debit.TXT' -ErrorAction SilentlyContinue).Count -ge 3
    } 90 'three Optum output files'
    $outputs = @(Get-ChildItem -Path $olsRoot -Recurse -Filter '*_Optum_*_op_debit.TXT' -ErrorAction SilentlyContinue)
    if ($outputsFound) { Add-Result 'Worker' 'Optum output files' 'PASS' (($outputs | ForEach-Object { $_.FullName }) -join ' ; ') }
    else { Add-Result 'Worker' 'Optum output files' 'FAIL' "expected 3, found $($outputs.Count) under $olsRoot (see worker.out.log)" }

    $r = Invoke-SqlInContainer "SET NOCOUNT ON; SELECT COUNT(*) FROM dbo.OlsFile" 'SE_OLS'
    $rowCount = Get-FirstNumber $r.Output
    if ($r.ExitCode -eq 0 -and $rowCount -ge 3) { Add-Result 'Worker' 'rows in dbo.OlsFile' 'PASS' "count $rowCount" }
    else { Add-Result 'Worker' 'rows in dbo.OlsFile' 'FAIL' "count '$($r.Output)' (expected at least 3)" }

    $transferRoot = Join-Path $LocalRoot 'SRVFS\filetransfer'
    $transferred = @(Get-ChildItem -Path $transferRoot -Recurse -File -ErrorAction SilentlyContinue)
    if ($transferred.Count -ge 3) { Add-Result 'Worker' 'files copied to File Transfer folder' 'PASS' (($transferred | ForEach-Object { $_.FullName }) -join ' ; ') }
    else { Add-Result 'Worker' 'files copied to File Transfer folder' 'FAIL' "expected 3, found $($transferred.Count) under $transferRoot" }

    $q = & curl.exe -s -u rabbitmq:rabbitmq "http://localhost:$RabbitManagementPort/api/queues/Nacha/FileTransmit"
    try {
        $queue = "$q" | ConvertFrom-Json
        $messages = [int]$queue.messages
        if ($messages -ge 1) { Add-Result 'Worker' 'FileTransmit messages in vhost Nacha' 'PASS' "$messages message(s) waiting for the File Transfer Service (local stand-in)" }
        else { Add-Result 'Worker' 'FileTransmit messages in vhost Nacha' 'WARN' 'queue exists but is empty; check worker.out.log for the notification publisher' }
    }
    catch {
        Add-Result 'Worker' 'FileTransmit messages in vhost Nacha' 'WARN' "could not read the queue: $q"
    }

    $workerLog = (Get-Content (Join-Path $ReportDir 'worker.out.log') -ErrorAction SilentlyContinue | Out-String)
    $warnLines = @(($workerLog -split "`r?`n") | Where-Object { $_ -match 'did not match any known transaction|fail|error|exception' } | Select-Object -First 10)
    $review = 'no failure, error or exception lines'
    if ($warnLines.Count -gt 0) { $review = $warnLines -join ' / ' }
    Add-Result 'Worker' 'worker log review' 'INFO' $review
}
catch {
    Write-Log "stopped: $($_.Exception.Message)"
}
finally {
    if (-not $KeepRunning) {
        Stop-App $script:ApiProcess 'API'
        Stop-App $script:WorkerProcess 'worker'
    }
    else {
        if ($null -ne $script:ApiProcess) { Write-Log "API left running (-KeepRunning): pid $($script:ApiProcess.Id)" }
        if ($null -ne $script:WorkerProcess) { Write-Log "worker left running (-KeepRunning): pid $($script:WorkerProcess.Id)" }
    }
    if ($StopInfra) {
        Invoke-Logged 'docker' (@('compose') + $ComposeFiles + @('stop', 'ols-sqlserver', 'ols-processor-rabbitmq')) (Join-Path $ReportDir 'compose-stop.log') | Out-Null
    }
}

$failCount = Write-Summary
if ($failCount -gt 0) { exit 1 } else { exit 0 }
