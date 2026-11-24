# Testes e CI

O projeto `GetCNPJ.Tests` usa xUnit v2 e VSTest, com referência ao projeto da biblioteca. Executa os mesmos três testes em `net48`, `net481`, `net8.0` e `net9.0`. `netstandard2.0` é validado por compilação: é um contrato de APIs, sem runtime próprio.

Os testes exercitam o cliente público com apenas BrasilAPI habilitada: resposta HTTP de sucesso e mapeamento básico, HTTP 503 registrado como falha do provedor e seleção de provedor inexistente sem requisição HTTP. `StubHttpMessageHandler` intercepta todo HTTP; não há consulta real, credenciais ou dados de clientes. O CNPJ e a razão social são sintéticos e não representam evidência de cadastro existente.

Em Windows, usar SDK com suporte a .NET 9, runtimes .NET 8 e 9 e .NET Framework 4.8.1. Os reference assemblies de .NET Framework devem estar disponíveis pelos targeting packs ou pelo mecanismo de restauração do SDK. Como .NET Framework 4.8.1 substitui 4.8, os testes de ambos os targets usam o runtime instalado; não comprovam execução em uma máquina com apenas 4.8.

```powershell
dotnet restore GetCNPJ.sln
dotnet build GetCNPJ.sln -c Release
dotnet test GetCNPJ.sln -c Release --no-build --settings tests/test.runsettings --logger trx --results-directory TestResults
```

O arquivo `test.runsettings` faz a execução falhar quando nenhum teste é encontrado. Os resultados TRX ficam em `TestResults/`, ignorado pelo Git. O `NuGet.Config` da raiz limita a restauração à fonte pública NuGet.org, evitando dependência de fontes privadas da máquina.

O workflow `.github/workflows/main.yml` executa restore, build de todos os targets e testes em Windows, nos pushes das branches configuradas, em pull requests para `main`/`develop` e por acionamento manual. Não contém empacotamento, publicação ou uso de segredo NuGet. A biblioteca não gera pacote automaticamente no build, e o projeto de testes não é empacotável.

## Runtime .NET 9 local nesta validação

Nesta máquina, o runtime .NET 9 foi instalado somente em `artifacts/dotnet9`, ignorado pelo Git. Para repetir o teste desse target sem instalação global:

```powershell
$runtimeHost = Join-Path $PWD 'artifacts/dotnet9/dotnet.exe'
dotnet test GetCNPJ.sln -c Release --no-build -f net9.0 --settings tests/test.runsettings --logger trx --results-directory TestResults -- "RunConfiguration.DotNetHostPath=$runtimeHost"
```

Para `net48`, `net481` e `net8.0`, usar o comando normal com `-f <target>`. O CI instala os SDKs/runtimes 8 e 9 com `setup-dotnet`, seleciona o SDK 9 por um global.json temporário e executa a solução inteira.

Referências oficiais: [xUnit e VSTest](https://xunit.net/docs/nuget-packages-v3), [setup-dotnet](https://github.com/actions/setup-dotnet), [checkout](https://github.com/actions/checkout).
