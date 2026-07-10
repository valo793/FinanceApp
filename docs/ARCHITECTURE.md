# Arquitetura

## Macro
Cliente desktop WinUI 3 -> API ASP.NET Core -> Application/Domain/Infrastructure -> PostgreSQL

## Decisões principais
- cliente sem segredos
- backend como mediador de regras críticas
- domínio financeiro centralizado
- auditoria obrigatória para eventos críticos
- recorrência materializada por worker

## Módulos já esboçados
- autenticação
- contas
- transações
- dashboards
- recorrência
- investimentos base

## Próximos blocos
- sessões e refresh token rotativo persistido
- MFA TOTP
- categorias e income sources
- projections
- notifications
- snapshots de dashboard
