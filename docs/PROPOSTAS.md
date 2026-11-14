# Propostas

Ideias levantadas durante os testes de uso real.

**Status:** P1, P2 e P3 foram implementadas em 2026-11-14 (ver *Decisões* no fim). Ainda falta validar no uso real. A ideia de luzes físicas continua em aberto.

---

## P1 — Encontrar o modo de edição

**Situação atual:** o modo de edição existe, mas fica escondido no menu do ícone da bandeja (botão direito → *Editar posição e tamanho*). No Windows 11 esse ícone costuma ficar dentro da seta `^` perto do relógio, então é fácil não achar.

**Proposta:**
- Botão direito **no próprio painel** abre o mesmo menu da bandeja.
- No primeiro uso, um aviso de uma linha no painel: *"Botão direito para editar"*, exibido uma única vez.
- Em modo edição: arrastar move o painel, arrastar a borda direita redimensiona, as setas do teclado fazem ajuste fino de 1 px, Enter salva e Esc cancela (hoje Esc também salva).

---

## P2 — Modos de posicionamento

Para quem não quer o painel dentro da barra de tarefas, ou usa barra com ocultação automática ou vertical.

| Modo | Onde fica | Arrastar | Uso típico |
|---|---|---|---|
| **Na barra** (atual) | Dentro da barra de tarefas | Só na horizontal | Barra com apps centralizados e lado esquerdo vazio |
| **Acima da barra** | Flutuando logo acima da barra, com 0,5 rem (8 px) de respiro | Na horizontal; a altura acompanha a barra | Quem não quer cobrir a barra |
| **Livre** | Qualquer ponto da tela | Livre, com ímã nas bordas | Barra oculta automaticamente, barra vertical, layouts próprios |

Detalhes:
- O respiro de 8 px é a distância entre a base do painel e o topo da barra. É configurável e escala com o DPI do monitor.
- No modo **Acima da barra** o painel cobre um pedaço da área útil. Ele continua escondido quando o Claude está visível e quando não há atividade (*Ocultar quando não houver atividade*), então só aparece quando tem algo a dizer.
- **Barra com ocultação automática:** no modo *Na barra* não há onde ancorar, então o painel cai automaticamente para *Acima da barra*, encostado na borda da tela.
- A posição é salva por modo. Trocar de modo e voltar devolve o painel ao lugar onde estava.

Mudança técnica: `PanelLayout` passa a receber o modo e devolver o retângulo de cada caso. É lógica pura e testável, como hoje. As configurações ganham `Placement` e `OffsetY`.

---

## P3 — Vídeo em tela cheia (YouTube, filmes, jogos)

**Situação atual:** em tela cheia o painel some de propósito, para não ficar por cima do vídeo. O efeito colateral é que você não vê quando o Claude termina ou quando precisa de você.

**Fato importante:** as notificações do próprio Windows também são silenciadas em tela cheia (regra automática do *Não perturbe*). Por isso o aviso do app do Claude provavelmente não aparece nesse cenário.

### Opções

| Opção | Como funciona | Intrusão | Funciona em |
|---|---|---|---|
| **A · Ocultar** (atual) | Nada aparece em tela cheia | Nenhuma | — |
| **B · Borda luminosa** | Uma linha fina (2–3 px) na borda inferior da tela pisca em verde quando termina e em âmbar quando precisa de você. Sem texto | Mínima | Vídeo no navegador e players em janela sem borda |
| **C · Aviso rápido** | Uma pílula pequena num canto, por ~4 s, só nos eventos importantes (*"API concluído em 12m47s"*, *"SITE aguardando você"*). Depois some | Baixa | Idem |
| **D · Mini painel fixo** | Versão compacta e semitransparente do painel num canto, enquanto houver atividade. Os cliques passam através dele | Média | Idem |

**Limitação técnica:** jogos em tela cheia *exclusiva* (DirectX exclusivo) não permitem nada por cima sem minimizar o jogo. Nesses casos a opção vira automaticamente *Ocultar*. Vídeos no navegador (YouTube, Netflix) usam tela cheia "sem borda" e aceitam B, C e D.

**Recomendação:** **B + C juntas como padrão.** A borda dá um sinal ambiente que não atrapalha o filme, e o aviso diz *o quê* aconteceu, só quando importa. D fica como opção para quem quer acompanhar o tempo todo.

### Ideia para o futuro (bom chamariz no GitHub)
Saídas externas, com **luzes físicas**: teclado RGB e fita LED (via OpenRGB), Philips Hue, ou um LED USB/Arduino. Isso entraria como um ponto de extensão `ISignalOutput`, ao lado do painel. Com ele dá pra ver o status até de longe da tela.

---

## Decisões (2026-11-14)
1. **P1 — aprovado:** menu pelo botão direito no próprio painel, aviso de primeiro uso, setas para ajuste fino, Enter salva e Esc cancela.
2. **P2 — aprovado:** os três modos, *Na barra* (padrão), *Acima da barra* (respiro de 8 px) e *Livre*, escolhidos no menu.
3. **P3 — aprovado:** em tela cheia, o padrão é **borda luminosa + aviso rápido**. As opções *Só aviso rápido*, *Mini painel fixo* e *Ocultar* ficam no menu. Em tela cheia exclusiva (DirectX), o painel apenas se oculta.
4. **Em aberto:** registrar `ISignalOutput` (luzes físicas) no roadmap do README.
