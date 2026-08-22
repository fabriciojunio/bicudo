# Monta o jogo, roda os testes e gera o executavel.
# Uso:  .\montar.ps1            (monta e testa)
#       .\montar.ps1 -Build     (monta, testa e gera o .exe)
#       .\montar.ps1 -SoTestes  (so roda os testes)

param(
    [switch]$Build,
    [switch]$SoTestes
)

$ErrorActionPreference = "Stop"

$unity = "C:\Program Files\Unity\Hub\Editor\2022.3.62f3\Editor\Unity.exe"
$projeto = $PSScriptRoot
$logs = Join-Path $projeto "Logs"

if (-not (Test-Path $unity)) {
    Write-Host "Unity 2022.3.62f3 nao encontrado em:" -ForegroundColor Red
    Write-Host "  $unity"
    Write-Host "Ajuste a variavel `$unity no topo deste script."
    exit 1
}
if (-not (Test-Path $logs)) { New-Item -ItemType Directory $logs | Out-Null }

function Executar($titulo, $argumentos, $arquivoLog) {
    Write-Host ""
    Write-Host "=== $titulo ===" -ForegroundColor Cyan
    $caminhoLog = Join-Path $logs $arquivoLog
    $p = Start-Process -FilePath $unity -ArgumentList $argumentos -PassThru -Wait -NoNewWindow
    Write-Host "  codigo de saida: $($p.ExitCode)   log: Logs\$arquivoLog"
    return $p.ExitCode
}

$falhou = $false

if (-not $SoTestes) {
    $args1 = @(
        "-batchmode", "-quit", "-nographics",
        "-projectPath", "`"$projeto`"",
        "-executeMethod", "BicudoBuild.MontarTudo",
        "-logFile", "`"$logs\montar.log`""
    )
    if ((Executar "Montando o jogo" $args1 "montar.log") -ne 0) {
        Write-Host "Falhou ao montar. Veja Logs\montar.log" -ForegroundColor Red
        exit 1
    }
}

# --- testes ---
$argsEdit = @(
    "-batchmode", "-nographics",
    "-projectPath", "`"$projeto`"",
    "-runTests", "-testPlatform", "EditMode",
    "-testResults", "`"$logs\resultado-editmode.xml`"",
    "-logFile", "`"$logs\testes-editmode.log`""
)
if ((Executar "Testes EditMode" $argsEdit "testes-editmode.log") -ne 0) { $falhou = $true }

# Sem "-nographics": os testes de PlayMode carregam as cenas de verdade e
# alguns chamam Camera.Render para guardar captura de tela. Sem contexto
# grafico o mono derruba o processo no meio da suite, e o resultado parece
# falha de teste quando e falha de ambiente.
$argsPlay = @(
    "-batchmode",
    "-projectPath", "`"$projeto`"",
    "-runTests", "-testPlatform", "PlayMode",
    "-testResults", "`"$logs\resultado-playmode.xml`"",
    "-logFile", "`"$logs\testes-playmode.log`""
)
if ((Executar "Testes PlayMode" $argsPlay "testes-playmode.log") -ne 0) { $falhou = $true }

# --- resumo dos resultados ---
Write-Host ""
Write-Host "=== Resumo ===" -ForegroundColor Cyan
foreach ($nome in @("editmode", "playmode")) {
    $xml = Join-Path $logs "resultado-$nome.xml"
    if (Test-Path $xml) {
        [xml]$r = Get-Content $xml
        $t = $r.'test-run'
        Write-Host ("  {0,-9} total={1}  passou={2}  falhou={3}  pulou={4}" -f `
            $nome, $t.total, $t.passed, $t.failed, $t.skipped)

        if ([int]$t.failed -gt 0) {
            $r.SelectNodes("//test-case[@result='Failed']") | ForEach-Object {
                Write-Host ("     X {0}" -f $_.fullname) -ForegroundColor Red
            }
        }
    } else {
        Write-Host "  $nome : sem arquivo de resultado" -ForegroundColor Yellow
    }
}

if ($Build -and -not $falhou) {
    $argsBuild = @(
        "-batchmode", "-quit", "-nographics",
        "-projectPath", "`"$projeto`"",
        "-executeMethod", "BicudoBuild.GerarExecutavel",
        "-logFile", "`"$logs\build.log`""
    )
    if ((Executar "Gerando executavel" $argsBuild "build.log") -eq 0) {
        Write-Host ""
        Write-Host "Pronto: Build/Bicudo.exe" -ForegroundColor Green
    }
}

if ($falhou) {
    Write-Host ""
    Write-Host "Alguns testes falharam. Veja os logs em Logs\." -ForegroundColor Yellow
    exit 1
}

Write-Host ""
Write-Host "Tudo certo." -ForegroundColor Green
