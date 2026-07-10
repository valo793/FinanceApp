# FinanceApp

Scaffold inicial de uma plataforma de finanças pessoais desktop para Windows com backend ASP.NET Core, PostgreSQL, worker de recorrência e cliente WinUI 3.

## Estrutura
- `src/FinanceApp.Api`: API principal
- `src/FinanceApp.Application`: casos de uso, DTOs e contratos
- `src/FinanceApp.Domain`: entidades e regras de domínio
- `src/FinanceApp.Infrastructure`: persistência, segurança e integrações internas
- `src/FinanceApp.Contracts`: contratos compartilhados
- `src/FinanceApp.Worker`: jobs de recorrência e notificações
- `src/FinanceApp.Desktop`: cliente WinUI 3
- `tests`: testes unitários e de integração
- `docs`: arquitetura, segurança e próximos passos
- `deploy`: infraestrutura local de desenvolvimento

## Base técnica
- .NET 10 LTS
- WinUI 3
- ASP.NET Core Web API
- PostgreSQL
- EF Core
- Serilog
- OpenTelemetry
- JWT + refresh token rotativo
- Background worker para recorrência

## Estado deste scaffold
Este pacote entrega:
- estrutura de solução
- entidades centrais do domínio
- DbContext e mapeamentos principais
- endpoints base de autenticação, contas, transações e dashboards
- worker de recorrência
- cliente desktop com shell, login e dashboard inicial
- documentação técnica inicial
- Docker Compose para PostgreSQL

## Como evoluir
1. Ajustar secrets e certificados por ambiente.
2. Implementar migrations reais e pipeline CI/CD.
3. Completar fluxos de autenticação, MFA, recuperação de senha e auditoria fina.
4. Implementar os módulos faltantes: categorias, investimentos, projeções, notificações e settings.
5. Conectar a UI WinUI aos endpoints reais e expandir o design system.
