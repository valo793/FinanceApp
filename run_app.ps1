# Script de inicializacao do FinanceApp
# Este script inicia a API em segundo plano, aguarda a inicializacao e roda o app desktop.
# Ao fechar o app desktop, a API tambem e encerrada.

$WorkspaceDir = "c:\Users\Pichau\Desktop\FinanceApp"
Set-Location -Path $WorkspaceDir

# Finaliza qualquer instancia anterior travada
Get-Process -Name "FinanceApp.Api", "FinanceApp.Desktop" -ErrorAction SilentlyContinue | Stop-Process -Force

# Caminhos dos executaveis compilados
$ApiExe = Join-Path $WorkspaceDir "src\FinanceApp.Api\bin\Debug\net10.0\FinanceApp.Api.exe"
$DesktopExe = Join-Path $WorkspaceDir "src\FinanceApp.Desktop\bin\Debug\net10.0-windows10.0.19041.0\win-x64\FinanceApp.Desktop.exe"

# 1. Iniciar a API
if (Test-Path $ApiExe) {
    Write-Host "Iniciando API via binario..."
    $ApiProcess = Start-Process $ApiExe -ArgumentList "--urls http://localhost:5000" -PassThru -WindowStyle Hidden -WorkingDirectory (Split-Path $ApiExe)
} else {
    Write-Host "Iniciando API via dotnet run..."
    $ApiProcess = Start-Process dotnet -ArgumentList "run --project src\FinanceApp.Api\FinanceApp.Api.csproj --urls http://localhost:5000" -PassThru -WindowStyle Hidden
}

# 2. Aguardar a API ficar online
Write-Host "Aguardando inicializacao da API..."
$maxRetries = 20
$retryCount = 0
$apiReady = $false
while ($retryCount -lt $maxRetries -and -not $apiReady) {
    try {
        $response = Invoke-WebRequest -Uri "http://localhost:5000/api/v1/accounts" -Method Get -TimeoutSec 1 -ErrorAction Stop
        $apiReady = $true
    } catch {
        if ($_.Exception.Response) {
            $apiReady = $true
        } else {
            if ($ApiProcess.HasExited) {
                break
            }
            Start-Sleep -Milliseconds 500
            $retryCount++
        }
    }
}

if (-not $apiReady) {
    Add-Type -AssemblyName System.Windows.Forms
    [System.Windows.Forms.MessageBox]::Show("A API nao iniciou a tempo. Por favor, certifique-se de que o banco de dados PostgreSQL esta rodando.", "FinanceApp - Erro", "OK", "Error")
    $ApiProcess | Stop-Process -Force -ErrorAction SilentlyContinue
    exit
}

# 3. Iniciar o App Desktop
if (Test-Path $DesktopExe) {
    Write-Host "Iniciando App Desktop via binario..."
    $DesktopProcess = Start-Process $DesktopExe -PassThru -WorkingDirectory (Split-Path $DesktopExe)
} else {
    Write-Host "Iniciando App Desktop via dotnet run..."
    $DesktopProcess = Start-Process dotnet -ArgumentList "run --project src\FinanceApp.Desktop\FinanceApp.Desktop.csproj" -PassThru
}

# 4. Aguardar o usuario fechar o App Desktop
$DesktopProcess.WaitForExit()

# 5. Encerrar a API ao fechar o app
Write-Host "Encerrando a API do FinanceApp..."
$ApiProcess | Stop-Process -Force -ErrorAction SilentlyContinue
