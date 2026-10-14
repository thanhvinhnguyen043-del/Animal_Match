$ErrorActionPreference = 'Stop'
$friendWord = $null
$friendDoc = $null
try {
    $friendWord = New-Object -ComObject Word.Application
    $friendWord.Visible = $false
    $friendWord.DisplayAlerts = 0
    $friendWord.AutomationSecurity = 3
    $friendDoc = $friendWord.Documents.Open('C:\Users\ADMIN\Downloads\Bài thực hành An toàn Game.docx', $false, $true)
    $friendDoc.Repaginate()
    $friendPages = $friendDoc.ComputeStatistics(2)
    $friendDoc.ExportAsFixedFormat('C:\Users\ADMIN\Downloads\My project (1)\report_work\friend_review\word_actual.pdf', 17)
    Write-Output "READ_ONLY_RENDER pages=$friendPages"
} finally {
    if ($friendDoc -ne $null) { $friendDoc.Close(0); [Runtime.InteropServices.Marshal]::ReleaseComObject($friendDoc) | Out-Null }
    if ($friendWord -ne $null) { $friendWord.Quit(); [Runtime.InteropServices.Marshal]::ReleaseComObject($friendWord) | Out-Null }
}
