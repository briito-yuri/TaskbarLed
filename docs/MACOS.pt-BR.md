# Instalação do Neon TSB Agents no macOS

[English](MACOS.md) · [README em português](../README.pt-BR.md)

## Apple Silicon (M1 ou posterior)

O download público é a versão **0.5.1**, para Macs com Apple Silicon e macOS 14 Sonoma ou posterior. O runtime .NET já vem no pacote.

1. [Baixe o ZIP para Apple Silicon](https://github.com/briito-yuri/TaskbarLed/releases/download/macos-v0.5.1/TaskbarLed-osx-arm64.zip).
2. Abra o ZIP no Finder para extrair o aplicativo.
3. Arraste **Neon Task Bar.app** para **Aplicativos** e abra por lá. A versão pública ainda usa esse nome.
4. Procure o ícone na barra de menus. O painel aparece quando há atividade detectada. Para deixá-lo visível mesmo sem atividade, selecione **Mostrar painel sem atividade** no menu do ícone.

O ZIP usa assinatura ad hoc. Se o macOS bloquear a abertura, consulte as [instruções da Apple para abrir um app de desenvolvedor que o macOS não consegue verificar](https://support.apple.com/pt-br/102445).

## Instalar pelo assistente

Os builds da branch também geram um instalador `.pkg`. Abra **TaskbarLed-osx-arm64.pkg** em um Mac com chip M ou **TaskbarLed-osx-x64.pkg** em um Mac Intel. Clique em **Continuar** e **Instalar** no assistente do macOS. Depois abra **Neon TSB Agents** em **Aplicativos**. O instalador inclui o app, os hooks e o runtime.

Para ativar o Claude Code sem usar o Terminal, abra o menu do Neon e clique em **Ativar integração com Claude Code…**. Reinicie as sessões do Claude Code para que carreguem a integração. A opção **Desativar integração com Claude Code…** remove os hooks instalados pelo Neon.

A prévia dos instaladores foi gerada a partir do commit `9fb170f` da branch `macos-port`. Estes pacotes de desenvolvimento ainda precisam de assinatura Developer ID Installer e notarização para distribuição pública sem a aprovação adicional do macOS.

[Baixar .pkg para Macs com chip M](https://github.com/briito-yuri/TaskbarLed/releases/download/macos-installer-9fb170f/TaskbarLed-osx-arm64.pkg) · [Baixar .pkg para Macs Intel](https://github.com/briito-yuri/TaskbarLed/releases/download/macos-installer-9fb170f/TaskbarLed-osx-x64.pkg)

## Mac Intel ou versão atual da branch

O pacote público acima é para Apple Silicon. Para usar o app em um **Mac Intel** ou testar a versão atual da branch com os temas mais recentes, gere o ZIP da arquitetura do seu Mac. Ambas as versões exigem macOS 14 ou posterior e incluem o runtime.

Com o SDK .NET indicado em `global.json` e as ferramentas de linha de comando da Apple instalados, abra o Terminal na raiz do repositório e execute o comando correspondente:

| Seu Mac | Comando | Arquivos gerados |
| --- | --- | --- |
| Chip Apple M1 ou posterior | `src/TaskbarLed.App.Avalonia/build-macos.sh osx-arm64` | `artifacts/TaskbarLed-osx-arm64/TaskbarLed-osx-arm64.zip` e `TaskbarLed-osx-arm64.pkg` |
| Processador Intel | `src/TaskbarLed.App.Avalonia/build-macos.sh osx-x64` | `artifacts/TaskbarLed-osx-x64/TaskbarLed-osx-x64.zip` e `TaskbarLed-osx-x64.pkg` |

Feche a versão anterior e abra o `.pkg` ou extraia o ZIP e mova **Neon TSB Agents.app** para **Aplicativos**. Abra o app pelo Finder. O build Apple Silicon roda nativamente em chips da série M.

## Ativar a integração com Claude Code

A detecção de processos e a integração com o Octo começam quando o app abre. Para receber eventos do Claude Code, rode o instalador de hooks depois de mover o app para **Aplicativos**:

```sh
# Para o ZIP público 0.5.1:
app="/Applications/Neon Task Bar.app"
# Para a versão compilada da branch, use:
# app="/Applications/Neon TSB Agents.app"
"$app/Contents/MacOS/TaskbarLed" --install-hooks
```

Reinicie as sessões abertas do Claude Code para carregar a configuração. Se mover ou renomear o app, execute o comando de novo com o caminho atualizado. Para desativar a integração, use `--uninstall-hooks` no mesmo executável.

Na versão atual da branch, clique com o botão direito no painel para abrir **Temas**, **Editar posição e tamanho…** ou **Encerrar**. [Galeria de temas](MACOS-THEMES.md) · [Guia técnico em inglês](MACOS.md)

## Desinstalar

1. Se você ativou a integração com o Claude Code, abra o menu do Neon e selecione **Desativar integração com Claude Code…**. Depois reinicie as sessões do Claude Code.
2. Escolha **Encerrar** no menu do Neon.
3. No Finder, abra **Aplicativos** e mova **Neon TSB Agents.app** para o Lixo. O macOS pode pedir a autorização de um administrador se o app foi instalado pelo `.pkg`.

As preferências ficam em `~/Library/Application Support/TaskbarLed` e são preservadas. A versão pública 0.5.1 aparece como **Neon Task Bar.app**.
