# Instalar o Neon TSB Agents no Mac

Escolha o instalador para o seu Mac e abra o arquivo baixado. No assistente do macOS, clique em **Continuar** e **Instalar**. Depois abra **Neon TSB Agents** em **Aplicativos**.

| Seu Mac | Baixar instalador |
| --- | --- |
| Chip Apple M1 ou posterior | [TaskbarLed-osx-arm64.pkg](https://github.com/briito-yuri/TaskbarLed/releases/download/macos-installer-9fb170f/TaskbarLed-osx-arm64.pkg) |
| Processador Intel | [TaskbarLed-osx-x64.pkg](https://github.com/briito-yuri/TaskbarLed/releases/download/macos-installer-9fb170f/TaskbarLed-osx-x64.pkg) |

Ambos exigem macOS 14 ou posterior e incluem o runtime. São instaladores de prévia do commit `9fb170f`, sem assinatura Developer ID Installer ou notarização.

Se aparecer o aviso de que a Apple não pôde verificar o instalador, tente abri-lo e, depois de conferir o SHA-256 do download, selecione **Abrir Mesmo Assim** em **Ajustes do Sistema → Privacidade e Segurança**. [Orientação da Apple](https://support.apple.com/pt-br/102445)

## Conferir o download

No Terminal, rode `shasum -a 256` seguido do caminho do arquivo `.pkg`. Compare o resultado com o SHA-256 correspondente abaixo.

| Arquivo | SHA-256 |
| --- | --- |
| `TaskbarLed-osx-arm64.pkg` | `d56c8fd2fb0e2df020f5add6f7c4a45fb64b39e3abdc4ee95e78fe801b1d1708` |
| `TaskbarLed-osx-x64.pkg` | `0479c7917500339ec14d6f5409f07a89ea179efc3b5d7a70072c4eda7b8b2c56` |

O SHA-256 confirma que o arquivo corresponde ao publicado. A revisão aberta permite auditar o código ou compilar localmente. A verificação do Gatekeeper para distribuição pública exige assinatura e notarização. [Código da prévia](https://github.com/briito-yuri/TaskbarLed/tree/9fb170fef2738ff9cad6a1874f014e5df45c1467) · [Como compilar](docs/MACOS.pt-BR.md#mac-intel-ou-versão-atual-da-branch)

[Guia em português](docs/MACOS.pt-BR.md) · [English guide](docs/MACOS.md)
