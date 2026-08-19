# Autenticação e convites

## Fluxo do participante

1. Um administrador cria um convite para um e-mail específico.
2. A API retorna o token uma única vez; somente o hash é guardado no PostgreSQL.
3. O participante envia token, nome e senha para `/api/auth/register`.
4. A API valida validade e uso do convite, cria usuário e associação na mesma transação e marca o convite como usado.
5. Registro e login retornam JWT com duração padrão de 60 minutos.

## Autorização

O JWT identifica somente o usuário. O acesso a dados de uma comunidade exige consulta à tabela `communities_memberships`. Isso evita que uma associação removida continue válida até o vencimento do token.

Administradores podem criar convites. Membros podem acessar o diretório. Nenhuma dessas permissões deve ser inferida de dados enviados pelo frontend.

## Configuração obrigatória

`Jwt__Key` deve conter um segredo aleatório de pelo menos 32 bytes e ser fornecido por variável de ambiente na VPS. O repositório não contém uma chave funcional.

O primeiro administrador é uma operação de implantação: criar comunidade, usuário inicial com hash de senha válido e associação com papel `admin`. Esse bootstrap será automatizado antes do primeiro deploy.

## Pendências antes da publicação

- limitação de tentativas de login e criação de convites;
- recuperação de senha;
- confirmação de e-mail;
- refresh token com rotação e revogação;
- trilha de auditoria administrativa;
- integração com provedor de e-mail;
- política de senha e termos de privacidade revisados.
