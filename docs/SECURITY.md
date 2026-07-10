# Segurança

## Medidas já previstas no scaffold
- JWT para autenticação de API
- abstração para hashing de senha
- armazenamento de segredo JWT em configuração
- auditoria de login e registro
- ownership por userId
- DTOs separados de entidades
- concurrency token em entidades críticas

## Ajustes obrigatórios antes de produção
- substituir hasher placeholder por Argon2id ou ASP.NET Core Identity
- implementar refresh token rotativo persistido com hash e reuse detection
- adicionar MFA TOTP e recovery codes
- configurar TLS, HSTS e rate limiting
- integrar vault de segredos
- revisar política de logs e mascaramento
