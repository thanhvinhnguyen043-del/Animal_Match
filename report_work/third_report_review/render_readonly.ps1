$ErrorActionPreference = 'Stop'
$wordApp = $null
$reviewDoc = $null
try {
 $wordApp = New-Object -ComObject Word.Application
 $wordApp.Visible = $false
 $wordApp.DisplayAlerts = 0
 $wordApp.AutomationSecurity = 3
 $reviewDoc = $wordApp.Documents.Open('C:\Users\ADMIN\Downloads\Bài Thực hành.docx', $false, $true)
 $reviewDoc.Repaginate()
 $pages = $reviewDoc.ComputeStatistics(2)
 $reviewDoc.ExportAsFixedFormat('C:\Users\ADMIN\Downloads\My project (1)\report_work\third_report_review\review.pdf',17)
 Write-Output "PAGES=$pages"
} finally {
 if ($reviewDoc -ne $null) { $reviewDoc.Close(0) }
 if ($wordApp -ne $null) { $wordApp.Quit() }
 if ($reviewDoc -ne $null) { [Runtime.InteropServices.Marshal]::ReleaseComObject($reviewDoc) | Out-Null }
 if ($wordApp -ne $null) { [Runtime.InteropServices.Marshal]::ReleaseComObject($wordApp) | Out-Null }
}
