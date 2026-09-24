param(
    [string]$ProcessName = "ChatDND",
    [int]$Samples = 60
)

$process = Get-Process -Name $ProcessName -ErrorAction Stop
$values = for ($i = 0; $i -lt $Samples; $i++) {
    $process.Refresh()
    [pscustomobject]@{
        Time = Get-Date
        WorkingSetMB = [math]::Round($process.WorkingSet64 / 1MB, 2)
    }
    Start-Sleep -Seconds 1
}

$summary = $values | Measure-Object WorkingSetMB -Average -Maximum -Minimum
$values | Format-Table -AutoSize
$summary | Format-List
