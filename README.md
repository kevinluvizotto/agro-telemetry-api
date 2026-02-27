agro-telemetry-api

API de Telemetria do projeto AgroSolutions IoT (FIAP Tech Challenge – Fase 5).

Responsável por:
- Receber leituras de sensores (umidade/temperatura/chuva)
- Persistir e disponibilizar histórico de leituras
- Publicar eventos no RabbitMQ via MassTransit para processamento assíncrono (ex.: geração de alertas)

STACK
- .NET (Minimal API / ASP.NET)
- Azure SQL
- RabbitMQ + MassTransit
- JWT Bearer Authentication
- Swagger/OpenAPI

FLUXO (ALTO NÍVEL)
1) Portal/cliente envia leitura -> Telemetry API (POST /telemetry/readings)
2) Telemetry API salva e publica evento (ex.: sensor-reading-received)
3) Alerts API consome e gera alertas quando necessário

PRINCIPAIS ENDPOINTS (EXEMPLO)
- GET /health
- GET /telemetry/readings?plotId=...&take=200
- POST /telemetry/readings

CONFIGURAÇÃO (ENV VARS)
- ConnectionStrings__Default
- Jwt__Issuer
- Jwt__Audience
- Jwt__Key
- Rabbit__Host (ex.: rabbitmq.agro.svc.cluster.local)
- Rabbit__User
- Rabbit__Pass

RODAR LOCALMENTE (PRECISA DO RABBITMQ)
Subir RabbitMQ local:
docker run -d --name rabbitmq \
  -p 5672:5672 -p 15672:15672 \
  -e RABBITMQ_DEFAULT_USER=agro \
  -e RABBITMQ_DEFAULT_PASS=agropass \
  rabbitmq:3.13-management

Depois rode a API:
dotnet restore
dotnet run

TESTE RÁPIDO (COM JWT)
TOKEN="(cole o token aqui)"
curl -s -X POST "http://localhost:8082/telemetry/readings" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "plotId":"00000000-0000-0000-0000-000000000000",
    "timestamp":"2026-02-27T00:00:00Z",
    "soilMoisture":25.0,
    "temperatureC":29.1,
    "precipitationMm":0
  }'

OBSERVAÇÕES PARA AKS/INGRESS
Quando publicado atrás do Ingress, costuma ser acessado via:
- /telemetry/...