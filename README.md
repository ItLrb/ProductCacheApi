# Product Cache API

API REST desenvolvida em **.NET 8** com foco em boas práticas de backend, cache e observabilidade.

## 🚀 Tecnologias
- .NET 8 (ASP.NET Core) & Entity Framework Core
- MySQL
- Redis
- Docker & Docker Compose
- Serilog

## 📌 Funcionalidades
- CRUD de produtos
- Cache de produtos com Redis
- Invalidação automática de cache
- Logs profissionais (console e arquivo)
- Tratamento global de erros
- Respostas padronizadas com DTOs

## 🧱 Arquitetura
- Controllers
- DTOs (entrada e saída)
- Middleware global de exceptions
- Cache desacoplado (Redis)
- Entidades isoladas do contrato da API

## 🐳 Como rodar o projeto (Docker)

```bash
docker compose up -d