# Design system inicial

## Direção

A interface deve parecer uma ferramenta profissional acolhedora, não uma rede social barulhenta. A hierarquia deve favorecer três ações: completar perfil, encontrar pessoas e enviar conexão.

## Tokens

| Papel | Valor |
|---|---|
| Ink / texto principal | `#162238` |
| Accent / ação | `#E96B43` |
| Fundo | `#F7F8FA` |
| Superfície | `#FFFFFF` |
| Texto secundário | `#4E5A6A` |
| Borda | `#E2E6EB` |
| Raio padrão | `8px` |
| Raio de cards | `12px` |
| Espaçamento base | múltiplos de `4px` |

## Componentes MVP

- **Botão primário:** ação única e clara (entrar, salvar perfil, conectar).
- **Botão secundário:** ações não destrutivas com borda.
- **Campo de filtro:** rótulo acessível, estado de foco visível e mensagens de erro abaixo.
- **Card de perfil:** nome, curso, instituição, intenção, competências e uma única ação de conexão.
- **Tag:** competência ou interesse; não deve ser usada como botão sem comportamento explícito.
- **Estado vazio:** orienta o próximo filtro ou convida a completar o perfil.

## Acessibilidade

Contraste mínimo WCAG AA, navegação completa por teclado, foco sempre visível, rótulos associados aos campos e textos que não dependem exclusivamente de cor. Não mostrar telefone/e-mail em cards públicos.
