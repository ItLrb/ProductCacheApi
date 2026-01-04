# Product Cache API

API REST desenvolvida em **.NET 8** com foco em boas práticas de backend, cache e observabilidade. Sistema completo de gerenciamento de produtos com cache Redis para alta performance.

## 🚀 Tecnologias

- **.NET 8** (ASP.NET Core)
- **Entity Framework Core 8.0** (ORM)
- **MySQL** (Banco de dados)
- **Redis** (Cache distribuído)
- **Docker & Docker Compose** (Containerização)
- **Serilog** (Logging estruturado)
- **Swagger/OpenAPI** (Documentação da API)

## 📌 Funcionalidades

- ✅ **CRUD completo de produtos**
- ✅ **Cache inteligente com Redis** (invalidação automática)
- ✅ **Validação de dados** com Data Annotations
- ✅ **Logs profissionais** (console e arquivo com rotação diária)
- ✅ **Tratamento global de erros** com middleware customizado
- ✅ **Respostas padronizadas** com DTOs
- ✅ **Degradação graciosa** (funciona mesmo sem Redis)
- ✅ **Documentação automática** com Swagger

## 🧱 Arquitetura

```
ProductCacheApi/
├── Controllers/          # Endpoints da API
├── DTOs/                # Data Transfer Objects (entrada/saída)
├── Entities/             # Entidades do domínio
├── DbContext/            # Contexto do Entity Framework
├── Cache/                # Serviço de cache (Redis)
├── Interfaces/           # Contratos de serviços
├── Middlewares/          # Middlewares customizados
├── Responses/            # Modelos de resposta padronizados
└── Migrations/           # Migrações do banco de dados
```

### Princípios de Design

- **Separação de responsabilidades**: Controllers, Services, DTOs e Entities separados
- **Inversão de dependência**: Uso de interfaces para desacoplamento
- **Validação em camadas**: DTOs com Data Annotations + ModelState validation
- **Cache desacoplado**: Interface ICacheService permite trocar implementação facilmente
- **Tratamento de erros centralizado**: ExceptionMiddleware para erros globais

## 📋 Pré-requisitos

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [MySQL](https://www.mysql.com/downloads/) ou [Docker](https://www.docker.com/)
- [Redis](https://redis.io/download) ou Docker
- [Git](https://git-scm.com/)

## 🛠️ Instalação e Configuração

### 1. Clone o repositório

```bash
git clone https://github.com/seu-usuario/ProductCacheApi.git
cd ProductCacheApi
```

### 2. Configure o banco de dados

Edite o arquivo `appsettings.Development.json` com suas credenciais do MySQL:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "server=localhost;database=productCachedb;user=seu_usuario;password=sua_senha"
  },
  "Redis": {
    "Connection": "localhost:6379"
  }
}
```

### 3. Execute as migrações

```bash
dotnet ef database update
```

Ou se preferir criar uma nova migração:

```bash
dotnet ef migrations add NomeDaMigracao
dotnet ef database update
```

### 4. Inicie o Redis (opcional, mas recomendado)

#### Opção A: Docker Compose

```bash
docker compose up -d
```

#### Opção B: Redis local

```bash
# Windows (com Chocolatey)
choco install redis-64

# Linux
sudo apt-get install redis-server

# macOS
brew install redis
```

**Nota**: A aplicação funciona mesmo sem Redis, mas o cache não estará disponível.

## 🚀 Como Executar

### Desenvolvimento

```bash
# Restaurar dependências
dotnet restore

# Compilar
dotnet build

# Executar
dotnet run
```

A aplicação estará disponível em:
- **API**: `http://localhost:5149`
- **Swagger UI**: `http://localhost:5149/swagger`

### Produção

```bash
# Publicar
dotnet publish -c Release -o ./publish

# Executar
cd publish
dotnet ProductCacheApi.dll
```

## 📚 Endpoints da API

### Base URL
```
http://localhost:5149/api/ProductControllers
```

### Endpoints Disponíveis

| Método | Endpoint | Descrição | Status de Sucesso |
|--------|----------|-----------|-------------------|
| `GET` | `/api/ProductControllers` | Lista todos os produtos | 200 |
| `GET` | `/api/ProductControllers/{id}` | Busca produto por ID | 200 |
| `POST` | `/api/ProductControllers` | Cria um novo produto | 201 |
| `PUT` | `/api/ProductControllers/{id}` | Atualiza um produto | 204 |
| `DELETE` | `/api/ProductControllers/{id}` | Deleta um produto | 204 |

### Exemplos de Uso

#### 1. Listar todos os produtos

```bash
GET /api/ProductControllers
```

**Resposta:**
```json
{
  "source": "cache",
  "data": [
    {
      "id": 1,
      "name": "Produto Exemplo",
      "price": 99.99,
      "stock": 50,
      "createdAt": "2026-01-04T00:00:00"
    }
  ]
}
```

#### 2. Buscar produto por ID

```bash
GET /api/ProductControllers/1
```

**Resposta:**
```json
{
  "source": "database",
  "data": {
    "id": 1,
    "name": "Produto Exemplo",
    "price": 99.99,
    "stock": 50,
    "createdAt": "2026-01-04T00:00:00"
  }
}
```

#### 3. Criar produto

```bash
POST /api/ProductControllers
Content-Type: application/json

{
  "name": "Novo Produto",
  "price": 149.99,
  "stock": 100
}
```

**Resposta:**
```json
{
  "success": true,
  "message": "Created product successfully",
  "data": {
    "id": 2,
    "name": "Novo Produto",
    "price": 149.99,
    "stock": 100
  }
}
```

#### 4. Atualizar produto

```bash
PUT /api/ProductControllers/1
Content-Type: application/json

{
  "name": "Produto Atualizado",
  "price": 199.99,
  "stock": 75
}
```

**Resposta:** `204 No Content`

#### 5. Deletar produto

```bash
DELETE /api/ProductControllers/1
```

**Resposta:** `204 No Content`

### Validações

A API valida automaticamente os dados de entrada:

- **Name**: Obrigatório (string não vazia)
- **Price**: Obrigatório, mínimo 0.01
- **Stock**: Obrigatório, mínimo 0

**Exemplo de erro de validação:**
```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "Name": ["The Name field is required."],
    "Price": ["The field Price must be between 0,01 and ..."]
  }
}
```

## 🔧 Configuração

### Variáveis de Ambiente

Para produção, use variáveis de ambiente ou User Secrets:

```bash
# User Secrets (desenvolvimento)
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "server=localhost;database=productCachedb;user=root;password=root"
dotnet user-secrets set "Redis:Connection" "localhost:6379"
```

### appsettings.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": ""
  },
  "Redis": {
    "Connection": ""
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

## 📊 Cache

O sistema utiliza Redis para cache com as seguintes características:

- **Cache de lista**: Chave `products:all` (TTL: 5 minutos)
- **Cache individual**: Chave `product:{id}` (TTL: 5 minutos)
- **Invalidação automática**: Cache é invalidado em Create, Update e Delete
- **Degradação graciosa**: Se Redis não estiver disponível, a aplicação continua funcionando normalmente

## 📝 Logs

Os logs são gerados automaticamente pelo Serilog:

- **Console**: Logs em tempo real no console
- **Arquivo**: Logs salvos em `Logs/log-YYYYMMDD.txt` (rotação diária)
- **Níveis**: Information, Warning, Error

Exemplo de log:
```
2026-01-04 00:00:00 [INF] Creating product {"Name":"Produto","Price":99.99,"Stock":50}
2026-01-04 00:00:01 [INF] Product with ID 1 was updated successfully
```

## 🐳 Docker

### Docker Compose

O arquivo `compose.yaml` inclui apenas o Redis. Para um setup completo com MySQL:

```yaml
services:
  mysql:
    image: mysql:8.0
    container_name: productcache_mysql
    environment:
      MYSQL_ROOT_PASSWORD: root
      MYSQL_DATABASE: productCachedb
    ports:
      - "3306:3306"
    volumes:
      - mysql_data:/var/lib/mysql

  redis:
    image: redis:7
    container_name: productcache_redis
    ports:
      - "6379:6379"

volumes:
  mysql_data:
```

### Dockerfile

O projeto inclui um `Dockerfile` para containerização da aplicação:

```bash
docker build -t productcacheapi .
docker run -p 8080:8080 productcacheapi
```

## 🧪 Testes

Para testar a API, você pode usar:

- **Swagger UI**: `http://localhost:5149/swagger`
- **Postman**: Importe a coleção (se disponível)
- **cURL**: Exemplos nos endpoints acima
- **HTTP Client**: Arquivo `ProductCacheApi.http` (se configurado)

## 🛡️ Segurança

### Boas Práticas Implementadas

- ✅ Validação de entrada com Data Annotations
- ✅ DTOs para isolar entidades do contrato da API
- ✅ Tratamento de erros sem expor detalhes sensíveis
- ✅ Logs estruturados para auditoria

### Recomendações para Produção

- [ ] Adicionar autenticação/autorização (JWT, OAuth, etc.)
- [ ] Implementar HTTPS
- [ ] Adicionar rate limiting
- [ ] Configurar CORS adequadamente
- [ ] Usar variáveis de ambiente para secrets
- [ ] Implementar health checks
- [ ] Adicionar monitoramento (Application Insights, etc.)

## 📦 Estrutura do Projeto

```
ProductCacheApi/
├── Controllers/
│   └── ProductControllers.cs      # Endpoints da API
├── DTOs/
│   ├── CreateProductDto.cs          # DTO para criação
│   ├── UpdateProductDto.cs          # DTO para atualização
│   └── ProductDto.cs                # DTO para resposta
├── Entities/
│   └── Product.cs                   # Entidade do domínio
├── DbContext/
│   └── AppDbContext.cs              # Contexto do EF Core
├── Cache/
│   └── RedisCacheService.cs         # Implementação do cache
├── Interfaces/
│   └── ICacheService.cs             # Interface do cache
├── Middlewares/
│   └── ExceptionMiddleware.cs      # Tratamento global de erros
├── Responses/
│   └── ApiResponse.cs               # Modelo de resposta padronizado
├── Migrations/                       # Migrações do banco
├── Logs/                            # Arquivos de log
├── Program.cs                       # Configuração da aplicação
├── appsettings.json                  # Configurações
└── appsettings.Development.json     # Configurações de desenvolvimento
```

## 📄 Licença

Este projeto está sob a licença MIT. Veja o arquivo `LICENSE` para mais detalhes.

## 👤 Autor

**Italo**
- GitHub: [@ItLrb](https://github.com/ItLrb)
