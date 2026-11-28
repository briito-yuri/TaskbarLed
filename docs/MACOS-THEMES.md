# Temas do TaskbarLed no macOS

Capturas dos 17 temas do painel renderizadas pelo TaskbarLed com duas sessões e quatro subagentes. Escolha um tema no menu **Temas** da barra.

[Instalação no Mac](MACOS.pt-BR.md) · [Voltar ao README](../README.pt-BR.md)

## Temas compartilhados

| Mapa de Linhas | Liquid Glass |
| --- | --- |
| [![Mapa de Linhas](images/macos-themes/LineMap.png)](images/macos-themes/LineMap.png) | [![Liquid Glass](images/macos-themes/LiquidGlass.png)](images/macos-themes/LiquidGlass.png) |

| Índice Editorial | Janela Cobalto |
| --- | --- |
| [![Índice Editorial](images/macos-themes/EditorialIndex.png)](images/macos-themes/EditorialIndex.png) | [![Janela Cobalto](images/macos-themes/CobaltWindow.png)](images/macos-themes/CobaltWindow.png) |

| Fósforo Violeta | LCD Oliva |
| --- | --- |
| [![Fósforo Violeta](images/macos-themes/VioletPhosphor.png)](images/macos-themes/VioletPhosphor.png) | [![LCD Oliva](images/macos-themes/OliveLcd.png)](images/macos-themes/OliveLcd.png) |

| Âmbar em árvore | Carvão compacto |
| --- | --- |
| [![Âmbar em árvore](images/macos-themes/AmberTree.png)](images/macos-themes/AmberTree.png) | [![Carvão compacto](images/macos-themes/CompactCharcoal.png)](images/macos-themes/CompactCharcoal.png) |

| Âmbar com régua | Lista Clara |
| --- | --- |
| [![Âmbar com régua](images/macos-themes/AmberRail.png)](images/macos-themes/AmberRail.png) | [![Lista Clara](images/macos-themes/ClearList.png)](images/macos-themes/ClearList.png) |

A captura da galeria mostra o gradiente do painel. No macOS 26, o vidro nativo do AppKit deixa passar a cor do que está atrás da janela. A linha concluída recebe um pulso neutro e desaparece após dez segundos.

### Editor de posição, tamanho e opacidade

![Editor do TaskbarLed no macOS 26 com Liquid Glass, modo Manual e opacidade em 81%](images/macos-themes/LiquidGlassEditor.png)

Captura do app empacotado no macOS 26.5.2. A linha concluída usa dados de teste.

## Outros temas incluídos

| Claude Code | Modern Terminal |
| --- | --- |
| [![Claude Code](images/macos-themes/ClaudeCode.png)](images/macos-themes/ClaudeCode.png) | [![Modern Terminal](images/macos-themes/ModernTerminal.png)](images/macos-themes/ModernTerminal.png) |

| Matrix | Mono |
| --- | --- |
| [![Matrix](images/macos-themes/Matrix.png)](images/macos-themes/Matrix.png) | [![Mono](images/macos-themes/Mono.png)](images/macos-themes/Mono.png) |

| Minimal | Glass |
| --- | --- |
| [![Minimal](images/macos-themes/Minimal.png)](images/macos-themes/Minimal.png) | [![Glass](images/macos-themes/Glass.png)](images/macos-themes/Glass.png) |

| Paper |
| --- |
| [![Paper](images/macos-themes/Paper.png)](images/macos-themes/Paper.png) |

## Atualizar as capturas

A partir da raiz do repositório, execute os testes de renderização para recriar as imagens.

~~~sh
TASKBARLED_THEME_GALLERY="$PWD/docs/images/macos-themes" dotnet test tests/TaskbarLed.App.Avalonia.Tests -c Release --filter FullyQualifiedName~ThemeGalleryTests
~~~
