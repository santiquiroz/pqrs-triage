param(
  [string]$Servidor = "llama-server.exe",
  [string]$Modelo = "$env:USERPROFILE\.ollama\models\blobs\sha256-e8a3dc8b810a7cf4baa097263a71b0953b250620175ed72d93d113fc3ecb8d45",
  [int]$Puerto = 8091
)
# El GGUF es el que Ollama bajó con: ollama pull hf.co/bartowski/togethercomputer_Tev1-4B-experimental-GGUF:Q4_K_M
# Se sirve aparte de Ollama porque OLLAMA_CONTEXT_LENGTH=262144 impide cargarlo junto a otro modelo.
$log = Join-Path $PSScriptRoot "tev1.log"
Start-Process -FilePath $Servidor -WorkingDirectory (Split-Path $Servidor) -WindowStyle Hidden `
  -ArgumentList @('-m', $Modelo, '--host', '127.0.0.1', '--port', $Puerto, '-c', '16384', '-np', '4', '-ngl', '99', '--jinja', '--alias', 'tev1-4b') `
  -RedirectStandardError $log -RedirectStandardOutput "$log.out"
Write-Output "Tev1 arrancando en http://127.0.0.1:$Puerto/v1 (log: $log)"
