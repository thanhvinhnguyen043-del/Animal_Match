$ErrorActionPreference = 'Stop'
$reportPath = 'C:\Users\ADMIN\Downloads\My project (1)\Bao cao dich nguoc SandSort bo sung day du.docx'
$pdfPath = 'C:\Users\ADMIN\Downloads\My project (1)\report_work\continuous_render\word_actual.pdf'
$qaPath = 'C:\Users\ADMIN\Downloads\My project (1)\report_work\continuous_render\word_layout.json'
$wordApp = $null
$reportDoc = $null
try {
    $wordApp = New-Object -ComObject Word.Application
    $wordApp.Visible = $false
    $wordApp.DisplayAlerts = 0
    $wordApp.AutomationSecurity = 3
    $reportDoc = $wordApp.Documents.Open($reportPath, $false, $false)
    $reportDoc.Fields.Update() | Out-Null
    foreach ($toc in $reportDoc.TablesOfContents) {
        $toc.Update()
        $toc.Range.Font.Size = 10
        $toc.Range.ParagraphFormat.SpaceBefore = 0
        $toc.Range.ParagraphFormat.SpaceAfter = 0
        $toc.Range.ParagraphFormat.LineSpacingRule = 4
        $toc.Range.ParagraphFormat.LineSpacing = 12
    }
    $tocTitle = $reportDoc.Content.Duplicate
    if ($tocTitle.Find.Execute('MỤC LỤC')) {
        $tocTitle.Font.Size = 16
        $tocTitle.Font.Bold = 1
    }
    $reportDoc.Repaginate()
    foreach ($toc in $reportDoc.TablesOfContents) { $toc.UpdatePageNumbers() }
    $reportDoc.Save()
    $pages = $reportDoc.ComputeStatistics(2)
    $reportDoc.ExportAsFixedFormat($pdfPath, 17)
    [pscustomobject]@{pages=$pages} | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $qaPath -Encoding UTF8
    Write-Output "WORD_RENDER_OK pages=$pages"
} finally {
    if ($reportDoc -ne $null) { $reportDoc.Close(0) }
    if ($wordApp -ne $null) { $wordApp.Quit() }
    if ($reportDoc -ne $null) { [Runtime.InteropServices.Marshal]::ReleaseComObject($reportDoc) | Out-Null }
    if ($wordApp -ne $null) { [Runtime.InteropServices.Marshal]::ReleaseComObject($wordApp) | Out-Null }
}


