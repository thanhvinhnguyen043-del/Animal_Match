$ErrorActionPreference = 'Stop'
$reportPath = 'C:\Users\ADMIN\Downloads\My project (1)\Báo cáo dịch ngược SandSort cập nhật luồng thắng.docx'
$renderRoot = 'C:\Users\ADMIN\Downloads\My project (1)\report_work\win_flow_update\render'
New-Item -ItemType Directory -Path $renderRoot -Force | Out-Null
$pdfPath = Join-Path $renderRoot 'word_actual.pdf'
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
    $reportDoc.Repaginate()
    foreach ($toc in $reportDoc.TablesOfContents) { $toc.UpdatePageNumbers() }
    $reportDoc.Save()
    $pages = $reportDoc.ComputeStatistics(2)
    $reportDoc.ExportAsFixedFormat($pdfPath, 17)
    $positions = @()
    foreach ($p in $reportDoc.Paragraphs) {
        $text = $p.Range.Text.Trim()
        if ($text -match '^Hình \d+\.' -or $text -match '^7\.[78]\.' -or $text -match '^9\. Kết luận') {
            $positions += [pscustomobject]@{ text=$text; page=$p.Range.Information(3) }
        }
    }
    [pscustomobject]@{pages=$pages;positions=$positions} | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $renderRoot 'layout.json') -Encoding UTF8
    Write-Output "WORD_RENDER_OK pages=$pages"
} finally {
    if ($reportDoc -ne $null) { $reportDoc.Close(0) }
    if ($wordApp -ne $null) { $wordApp.Quit() }
    if ($reportDoc -ne $null) { [Runtime.InteropServices.Marshal]::ReleaseComObject($reportDoc) | Out-Null }
    if ($wordApp -ne $null) { [Runtime.InteropServices.Marshal]::ReleaseComObject($wordApp) | Out-Null }
}
