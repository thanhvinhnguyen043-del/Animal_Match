$ErrorActionPreference='Stop'
$source='C:\Users\ADMIN\Downloads\Báo cáo an toàn Game.docx'
$out='C:\Users\ADMIN\Downloads\My project (1)\report_work\friend_report_review\friend_report.pdf'
$before=(Get-FileHash -LiteralPath $source -Algorithm SHA256).Hash
$app=$null
$doc=$null
try {
 $app=New-Object -ComObject Word.Application
 $app.Visible=$false
 $app.DisplayAlerts=0
 $app.AutomationSecurity=3
 $doc=$app.Documents.Open($source,$false,$true)
 $doc.Repaginate()
 $pages=$doc.ComputeStatistics(2)
 $doc.ExportAsFixedFormat($out,17)
 Write-Output "READ_ONLY_RENDER_OK pages=$pages"
} finally {
 if($null -ne $doc){$doc.Close(0)}
 if($null -ne $app){$app.Quit()}
 if($null -ne $doc){[Runtime.InteropServices.Marshal]::ReleaseComObject($doc)|Out-Null}
 if($null -ne $app){[Runtime.InteropServices.Marshal]::ReleaseComObject($app)|Out-Null}
}
$after=(Get-FileHash -LiteralPath $source -Algorithm SHA256).Hash
if($before -ne $after){throw 'Source changed'}
