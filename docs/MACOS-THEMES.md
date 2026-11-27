# Temas do Neon TSB Agents no macOS

Capturas do painel renderizadas pelo Neon TSB Agents com duas sessões e quatro subagentes. Escolha um tema no menu **Tema** da barra.

[Instalação no Mac](MACOS.md) · [Voltar ao README](../README.pt-BR.md)

| Mapa de Linhas | Índice Editorial |
| --- | --- |
| ![Painel do Neon TSB Agents com o tema Mapa de Linhas](images/macos-themes/LineMap.png) | ![Painel do Neon TSB Agents com o tema Índice Editorial](images/macos-themes/EditorialIndex.png) |

| Janela Cobalto | Fósforo Violeta |
| --- | --- |
| ![Painel do Neon TSB Agents com o tema Janela Cobalto](images/macos-themes/CobaltWindow.png) | ![Painel do Neon TSB Agents com o tema Fósforo Violeta](images/macos-themes/VioletPhosphor.png) |

| LCD Oliva | Lista Clara |
| --- | --- |
| ![Painel do Neon TSB Agents com o tema LCD Oliva](images/macos-themes/OliveLcd.png) | ![Painel do Neon TSB Agents com o tema Lista Clara](images/macos-themes/ClearList.png) |

| Âmbar em árvore | Carvão compacto |
| --- | --- |
| ![Painel do Neon TSB Agents com o tema Âmbar em árvore](images/macos-themes/AmberTree.png) | ![Painel do Neon TSB Agents com o tema Carvão compacto](images/macos-themes/CompactCharcoal.png) |

| Âmbar com régua | Neon TSB Agents |
| --- | --- |
| ![Painel do Neon TSB Agents com o tema Âmbar com régua](images/macos-themes/AmberRail.png) | ![Painel do Neon TSB Agents com o tema Neon TSB Agents](images/macos-themes/ClaudeCode.png) |

| Modern Terminal | Matrix |
| --- | --- |
| ![Painel do Neon TSB Agents com o tema Modern Terminal](images/macos-themes/ModernTerminal.png) | ![Painel do Neon TSB Agents com o tema Matrix](images/macos-themes/Matrix.png) |

| Mono | Minimal |
| --- | --- |
| ![Painel do Neon TSB Agents com o tema Mono](images/macos-themes/Mono.png) | ![Painel do Neon TSB Agents com o tema Minimal](images/macos-themes/Minimal.png) |

| Glass | Paper |
| --- | --- |
| ![Painel do Neon TSB Agents com o tema Glass](images/macos-themes/Glass.png) | ![Painel do Neon TSB Agents com o tema Paper](images/macos-themes/Paper.png) |

| Liquid Glass |
| --- |
| ![Painel do Neon TSB Agents com o tema Liquid Glass](images/macos-themes/LiquidGlass.png) |

A captura de Liquid Glass mostra o gradiente da interface. No macOS 26, o tema acrescenta o material nativo do AppKit sobre o conteúdo atrás da janela.

## Atualizar as capturas

A partir da raiz do repositório, execute os testes de renderização para recriar as imagens.

~~~sh
TASKBARLED_THEME_GALLERY="$PWD/docs/images/macos-themes" dotnet test tests/TaskbarLed.App.Avalonia.Tests -c Release --filter FullyQualifiedName~ThemeGalleryTests
~~~
