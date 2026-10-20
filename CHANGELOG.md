# Changelog

Formato baseado em [Keep a Changelog](https://keepachangelog.com/pt-BR/1.1.0/).
Versionamento semântico.

## [1.0.0] - 2026-10-20

### Added

- Criação de pedido com preço e vendedor buscados no catalog no momento da compra.
- Listagem do comprador, consulta por id (dono ou admin), listagem administrativa
  com filtros de status e vendedor, e cancelamento pelo comprador.
- Outbox transacional: evento gravado junto com a mudança de estado e publicado
  no Kafka por um worker.
- Consumidor idempotente de `stock.reserved` e `stock.rejected` com a saga
  pending, confirmed, paid ou cancelled.
- Pagamento simulado com taxa de aprovação configurável.
- JWT RS256 validado pelo JWKS do auth, sem segredo compartilhado.
- `GET /health` com MySQL e Kafka.
- Testes com SQLite em memória e API inteira em memória.
- Dockerfile multi-stage e CI com testes e `dotnet format`.
