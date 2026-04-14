# Руководство по развёртыванию DeveloptmentProcessesHITS (GoogleClassroom Backend)

> ASP.NET Core 9.0 REST API + PostgreSQL  
> Три варианта деплоя: **Docker Compose** (рекомендуется), **Docker** (ручной), **без Docker** (systemd)

---

## Содержание

1. [Требования к серверу](#1-требования-к-серверу)
2. [Подготовка сервера (Ubuntu 22.04)](#2-подготовка-сервера-ubuntu-2204)
3. [Установка зависимостей](#3-установка-зависимостей)
4. [Получение исходного кода](#4-получение-исходного-кода)
5. [Конфигурация приложения](#5-конфигурация-приложения)
6. [Вариант A — Docker Compose (рекомендуется)](#6-вариант-a--docker-compose-рекомендуется)
7. [Вариант B — Docker (ручной запуск)](#7-вариант-b--docker-ручной-запуск)
8. [Вариант C — без Docker (systemd-сервис)](#8-вариант-c--без-docker-systemd-сервис)
9. [Настройка Nginx как обратного прокси](#9-настройка-nginx-как-обратного-прокси)
10. [HTTPS через Let's Encrypt](#10-https-через-lets-encrypt)
11. [Проверка работоспособности](#11-проверка-работоспособности)
12. [Миграции базы данных](#12-миграции-базы-данных)
13. [Логи и мониторинг](#13-логи-и-мониторинг)
14. [Обновление приложения](#14-обновление-приложения)
15. [CI/CD через GitHub Actions](#15-cicd-через-github-actions)
16. [Устранение типичных ошибок](#16-устранение-типичных-ошибок)

---

## 1. Требования к серверу

| Параметр | Минимум | Рекомендуется |
|---|---|---|
| ОС | Ubuntu 22.04 LTS | Ubuntu 22.04 LTS |
| CPU | 1 vCPU | 2 vCPU |
| RAM | 1 GB | 2 GB |
| Диск | 10 GB | 20 GB SSD |
| Открытые порты | 22 (SSH), 80 (HTTP), 443 (HTTPS) | + 5432 (если нужен внешний доступ к БД) |

> **Важно:** порт 5432 (PostgreSQL) **не нужно** открывать публично — БД будет доступна только внутри сервера/Docker-сети.

---

## 2. Подготовка сервера (Ubuntu 22.04)

### 2.1 Первый вход на сервер

```bash
ssh root@<IP_АДРЕС_СЕРВЕРА>
```

### 2.2 Обновление системы

```bash
apt update && apt upgrade -y
```

### 2.3 Создание пользователя для приложения (безопасность)

```bash
adduser appuser
usermod -aG sudo appuser
# Переключаемся на нового пользователя
su - appuser
```

### 2.4 Настройка файрвола

```bash
sudo ufw allow OpenSSH
sudo ufw allow 80/tcp
sudo ufw allow 443/tcp
sudo ufw enable
sudo ufw status
```

---

## 3. Установка зависимостей

### 3.1 Установка Git

```bash
sudo apt install git -y
git --version   # должно вывести git version 2.x.x
```

### 3.2 Установка Docker и Docker Compose (для вариантов A и B)

```bash
# Добавляем официальный репозиторий Docker
sudo apt install ca-certificates curl gnupg -y
sudo install -m 0755 -d /etc/apt/keyrings
curl -fsSL https://download.docker.com/linux/ubuntu/gpg | \
  sudo gpg --dearmor -o /etc/apt/keyrings/docker.gpg
sudo chmod a+r /etc/apt/keyrings/docker.gpg

echo \
  "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/docker.gpg] \
  https://download.docker.com/linux/ubuntu \
  $(. /etc/os-release && echo "$VERSION_CODENAME") stable" | \
  sudo tee /etc/apt/sources.list.d/docker.list > /dev/null

# Устанавливаем Docker
sudo apt update
sudo apt install docker-ce docker-ce-cli containerd.io docker-buildx-plugin docker-compose-plugin -y

# Разрешаем запуск Docker без sudo
sudo usermod -aG docker $USER
newgrp docker

# Проверяем
docker --version          # Docker version 24.x.x
docker compose version    # Docker Compose version v2.x.x
```

### 3.3 Установка .NET 9.0 SDK (только для варианта C — без Docker)

```bash
# Добавляем репозиторий Microsoft
wget https://packages.microsoft.com/config/ubuntu/22.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
rm packages-microsoft-prod.deb

# Устанавливаем .NET 9.0 SDK
sudo apt update
sudo apt install dotnet-sdk-9.0 -y

# Проверяем
dotnet --version   # должно вывести 9.0.x
```

### 3.4 Установка PostgreSQL (только для варианта C — без Docker)

```bash
sudo apt install postgresql postgresql-contrib -y
sudo systemctl enable postgresql
sudo systemctl start postgresql
sudo systemctl status postgresql   # должно быть active (running)
```

---

## 4. Получение исходного кода

```bash
# Создаём папку для приложения
sudo mkdir -p /opt/gcapp
sudo chown $USER:$USER /opt/gcapp
cd /opt/gcapp

# Клонируем репозиторий
git clone https://github.com/russkoepoleexperimentov/DeveloptmentProcessesHITS.git .

# Переключаемся на нужную ветку (обычно main для production)
git checkout main

# Убеждаемся, что код актуален
git log --oneline -5
```

---

## 5. Конфигурация приложения

### 5.1 Создание конфигурации Production

Приложение загружает настройки из `appsettings.json`, а затем перекрывает их файлом `appsettings.Production.json`.  
**Никогда не редактируй `appsettings.json` напрямую** — создай отдельный файл для production.

```bash
cd /opt/gcapp/GoogleClassroom

# Создаём production-конфиг
nano appsettings.Production.json
```

Вставь следующее содержимое, заменив значения в угловых скобках `< >`:

```json
{
    "ConnectionStrings": {
        "DefaultConnection": "host=localhost;port=5432;username=gcuser;password=<ТВОЙ_ПАРОЛЬ_БД>;database=GcDb"
    },
    "FileStorage": {
        "Path": "/opt/gcapp/uploads"
    },
    "Jwt": {
        "Access": {
            "Secret": "<МИНИМУМ_32_СИМВОЛА_СЛУЧАЙНАЯ_СТРОКА>",
            "LifetimeMinutes": 40,
            "Issuer": "gcapp-production",
            "Audience": "gcapp-clients"
        },
        "Refresh": {
            "Secret": "<ЕЩЁ_ОДНА_СЛУЧАЙНАЯ_СТРОКА_МИНИМУМ_32_СИМВОЛА>",
            "LifetimeDays": 7
        }
    },
    "Logging": {
        "LogLevel": {
            "Default": "Warning",
            "Microsoft": "Warning",
            "Microsoft.EntityFrameworkCore": "Warning"
        }
    },
    "Urls": "http://0.0.0.0:8080"
}
```

> **Как сгенерировать случайную строку для JWT Secret:**
> ```bash
> openssl rand -base64 48
> ```
> Запусти команду дважды — получишь две разные строки для Access.Secret и Refresh.Secret.

### 5.2 Создание папки для загруженных файлов

```bash
mkdir -p /opt/gcapp/uploads
chmod 755 /opt/gcapp/uploads
```

---

## 6. Вариант A — Docker Compose (рекомендуется)

Это самый простой и надёжный способ. Docker Compose поднимает одновременно приложение и базу данных.

### 6.1 Создание docker-compose.yml

```bash
cd /opt/gcapp
nano docker-compose.yml
```

Вставь содержимое:

```yaml
version: "3.9"

services:
  db:
    image: postgres:16-alpine
    container_name: gcapp_db
    restart: unless-stopped
    environment:
      POSTGRES_USER: gcuser
      POSTGRES_PASSWORD: <ТВОЙ_ПАРОЛЬ_БД>     # совпадает с паролем в appsettings.Production.json
      POSTGRES_DB: GcDb
    volumes:
      - postgres_data:/var/lib/postgresql/data
    networks:
      - gcapp_net
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U gcuser -d GcDb"]
      interval: 10s
      timeout: 5s
      retries: 5

  app:
    build:
      context: ./GoogleClassroom
      dockerfile: Dockerfile
    container_name: gcapp_api
    restart: unless-stopped
    depends_on:
      db:
        condition: service_healthy
    environment:
      ASPNETCORE_ENVIRONMENT: Production
      ConnectionStrings__DefaultConnection: "host=db;port=5432;username=gcuser;password=<ТВОЙ_ПАРОЛЬ_БД>;database=GcDb"
      Jwt__Access__Secret: "<ТВОЙ_JWT_ACCESS_SECRET>"
      Jwt__Refresh__Secret: "<ТВОЙ_JWT_REFRESH_SECRET>"
      FileStorage__Path: /app/uploads
    volumes:
      - uploads_data:/app/uploads
    ports:
      - "127.0.0.1:8080:8080"    # доступен только локально — Nginx проксирует снаружи
    networks:
      - gcapp_net

volumes:
  postgres_data:
  uploads_data:

networks:
  gcapp_net:
    driver: bridge
```

> **Важно:** замени все `<ТВОЙ_ПАРОЛЬ_БД>`, `<ТВОЙ_JWT_ACCESS_SECRET>`, `<ТВОЙ_JWT_REFRESH_SECRET>` на реальные значения, **одинаковые** с теми, что указал в `appsettings.Production.json`.

### 6.2 Запуск

```bash
cd /opt/gcapp

# Собираем образ и запускаем все контейнеры в фоне
docker compose up --build -d

# Смотрим логи (Ctrl+C для выхода)
docker compose logs -f

# Проверяем статус
docker compose ps
```

Ожидаемый вывод `docker compose ps`:
```
NAME           IMAGE              COMMAND                  STATUS          PORTS
gcapp_api      gcapp-app          "dotnet GoogleClassr…"   Up 2 minutes    127.0.0.1:8080->8080/tcp
gcapp_db       postgres:16-alpine "docker-entrypoint.s…"   Up 2 minutes    5432/tcp
```

### 6.3 Остановка и перезапуск

```bash
docker compose stop          # остановить (данные сохраняются)
docker compose start         # запустить снова
docker compose restart       # перезапустить
docker compose down          # остановить и удалить контейнеры (данные НЕ удаляются — они в volumes)
docker compose down -v       # ⚠️ удалить контейнеры И все данные БД
```

---

## 7. Вариант B — Docker (ручной запуск)

Используй этот вариант, если PostgreSQL уже установлен на сервере отдельно.

### 7.1 Настройка PostgreSQL (если не установлен)

```bash
sudo systemctl start postgresql

# Создаём пользователя и базу данных
sudo -u postgres psql <<EOF
CREATE USER gcuser WITH PASSWORD '<ТВОЙ_ПАРОЛЬ_БД>';
CREATE DATABASE "GcDb" OWNER gcuser;
GRANT ALL PRIVILEGES ON DATABASE "GcDb" TO gcuser;
EOF
```

### 7.2 Сборка Docker-образа

```bash
cd /opt/gcapp/GoogleClassroom
docker build -t gcapp:latest .
```

Сборка займёт 2–5 минут при первом запуске.

### 7.3 Запуск контейнера приложения

```bash
docker run -d \
  --name gcapp_api \
  --restart unless-stopped \
  -p 127.0.0.1:8080:8080 \
  -v /opt/gcapp/uploads:/app/uploads \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -e "ConnectionStrings__DefaultConnection=host=host.docker.internal;port=5432;username=gcuser;password=<ТВОЙ_ПАРОЛЬ_БД>;database=GcDb" \
  -e "Jwt__Access__Secret=<ТВОЙ_JWT_ACCESS_SECRET>" \
  -e "Jwt__Refresh__Secret=<ТВОЙ_JWT_REFRESH_SECRET>" \
  --add-host=host.docker.internal:host-gateway \
  gcapp:latest
```

### 7.4 Проверка

```bash
docker ps
docker logs gcapp_api -f
```

---

## 8. Вариант C — без Docker (systemd-сервис)

Используй этот вариант, если Docker недоступен.

### 8.1 Настройка PostgreSQL

```bash
sudo -u postgres psql <<EOF
CREATE USER gcuser WITH PASSWORD '<ТВОЙ_ПАРОЛЬ_БД>';
CREATE DATABASE "GcDb" OWNER gcuser;
GRANT ALL PRIVILEGES ON DATABASE "GcDb" TO gcuser;
EOF
```

### 8.2 Сборка и публикация приложения

```bash
cd /opt/gcapp

# Собираем и публикуем в папку /opt/gcapp/publish
dotnet publish ./GoogleClassroom/GoogleClassroom.csproj \
  -c Release \
  -o /opt/gcapp/publish \
  --runtime linux-x64 \
  --self-contained false

# Проверяем, что файлы появились
ls /opt/gcapp/publish
# должен быть файл GoogleClassroom.dll
```

### 8.3 Создание systemd-сервиса

```bash
sudo nano /etc/systemd/system/gcapp.service
```

Вставь содержимое:

```ini
[Unit]
Description=GoogleClassroom ASP.NET Core API
After=network.target postgresql.service
Requires=postgresql.service

[Service]
Type=simple
User=appuser
WorkingDirectory=/opt/gcapp/publish
ExecStart=/usr/bin/dotnet /opt/gcapp/publish/GoogleClassroom.dll
Restart=on-failure
RestartSec=10
KillSignal=SIGINT
SyslogIdentifier=gcapp

# Переменные окружения
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=ASPNETCORE_URLS=http://0.0.0.0:8080
Environment="ConnectionStrings__DefaultConnection=host=localhost;port=5432;username=gcuser;password=<ТВОЙ_ПАРОЛЬ_БД>;database=GcDb"
Environment="Jwt__Access__Secret=<ТВОЙ_JWT_ACCESS_SECRET>"
Environment="Jwt__Refresh__Secret=<ТВОЙ_JWT_REFRESH_SECRET>"
Environment="FileStorage__Path=/opt/gcapp/uploads"

[Install]
WantedBy=multi-user.target
```

### 8.4 Запуск сервиса

```bash
# Права на папку
sudo chown -R appuser:appuser /opt/gcapp/publish
sudo chown -R appuser:appuser /opt/gcapp/uploads

# Активируем и запускаем
sudo systemctl daemon-reload
sudo systemctl enable gcapp
sudo systemctl start gcapp

# Проверяем статус
sudo systemctl status gcapp
```

Ожидаемый вывод:
```
● gcapp.service - GoogleClassroom ASP.NET Core API
     Loaded: loaded (/etc/systemd/system/gcapp.service; enabled)
     Active: active (running) since ...
```

---

## 9. Настройка Nginx как обратного прокси

Nginx принимает входящие HTTP/HTTPS запросы и передаёт их приложению на порт 8080.

### 9.1 Установка Nginx

```bash
sudo apt install nginx -y
sudo systemctl enable nginx
sudo systemctl start nginx
```

### 9.2 Создание конфигурации сайта

```bash
sudo nano /etc/nginx/sites-available/gcapp
```

Вставь конфигурацию (замени `<ТВОЙ_ДОМЕН_ИЛИ_IP>` на реальный домен или IP-адрес):

```nginx
server {
    listen 80;
    server_name <ТВОЙ_ДОМЕН_ИЛИ_IP>;

    # Лимит размера загружаемых файлов
    client_max_body_size 50M;

    # Заголовки безопасности
    add_header X-Frame-Options "SAMEORIGIN";
    add_header X-Content-Type-Options "nosniff";

    location / {
        proxy_pass         http://127.0.0.1:8080;
        proxy_http_version 1.1;
        proxy_set_header   Upgrade $http_upgrade;
        proxy_set_header   Connection keep-alive;
        proxy_set_header   Host $host;
        proxy_set_header   X-Real-IP $remote_addr;
        proxy_set_header   X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header   X-Forwarded-Proto $scheme;
        proxy_cache_bypass $http_upgrade;
        proxy_read_timeout 300s;
        proxy_connect_timeout 75s;
    }

    # Swagger UI — доступен только из локальной сети (опционально)
    # location /swagger {
    #     allow 10.0.0.0/8;
    #     allow 192.168.0.0/16;
    #     deny all;
    #     proxy_pass http://127.0.0.1:8080;
    # }
}
```

### 9.3 Активация конфигурации

```bash
# Создаём символическую ссылку
sudo ln -s /etc/nginx/sites-available/gcapp /etc/nginx/sites-enabled/gcapp

# Удаляем дефолтный сайт (если мешает)
sudo rm -f /etc/nginx/sites-enabled/default

# Проверяем синтаксис
sudo nginx -t
# должно вывести: syntax is ok / test is successful

# Перезагружаем Nginx
sudo systemctl reload nginx
```

---

## 10. HTTPS через Let's Encrypt

> Требование: у тебя должен быть **настоящий домен** (не IP-адрес), и DNS уже должен указывать на сервер.

### 10.1 Установка Certbot

```bash
sudo apt install certbot python3-certbot-nginx -y
```

### 10.2 Получение сертификата

```bash
sudo certbot --nginx -d <ТВОЙ_ДОМЕН>
# Например: sudo certbot --nginx -d api.myproject.ru
```

Certbot задаст несколько вопросов:
- Email для уведомлений об истечении сертификата
- Согласие с условиями сервиса
- **Редирект HTTP → HTTPS** — выбери вариант "2" (Redirect) — это рекомендуется

После этого Certbot сам обновит конфигурацию Nginx и добавит блок `443 ssl`.

### 10.3 Проверка автообновления

Сертификат действует 90 дней. Certbot настраивает автообновление через systemd:

```bash
# Проверяем, что автообновление работает
sudo certbot renew --dry-run
```

---

## 11. Проверка работоспособности

### 11.1 Проверка API

```bash
# Простой ping через curl (вернёт 404 — это нормально, значит API работает)
curl -i http://localhost:8080/

# Проверка через домен/IP
curl -i http://<ТВОЙ_ДОМЕН>/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"test@test.com","password":"wrongpass"}'
# Ожидаем ответ 400/401, а не 502 или "connection refused"
```

### 11.2 Swagger UI

Открой в браузере:

```
http://<ТВОЙ_ДОМЕН>/swagger
```

Должна открыться интерактивная документация API. Если открывается — приложение работает корректно.

### 11.3 Проверка базы данных (вариант Docker Compose)

```bash
# Подключаемся к контейнеру БД
docker exec -it gcapp_db psql -U gcuser -d GcDb

# Смотрим список таблиц (должны появиться после первого запуска)
\dt
# Ожидаем увидеть: AspNetUsers, AspNetRoles, Courses, Teams и т.д.

\q   # выход
```

---

## 12. Миграции базы данных

Приложение применяет все ожидающие миграции **автоматически при каждом старте** — это реализовано в `Program.cs`:

```csharp
if (context.Database.GetPendingMigrations().Any())
    context.Database.Migrate();
```

Тем не менее, бывают ситуации, когда нужно применить миграции вручную.

### 12.1 Ручное применение миграций (вариант C — без Docker)

```bash
cd /opt/gcapp

dotnet ef database update \
  --project GoogleClassroom/GoogleClassroom.csproj \
  --connection "host=localhost;port=5432;username=gcuser;password=<ПАРОЛЬ>;database=GcDb"
```

### 12.2 Проверка статуса миграций

```bash
dotnet ef migrations list \
  --project GoogleClassroom/GoogleClassroom.csproj
```

### 12.3 Откат миграции (при необходимости)

```bash
# Откат к конкретной миграции (указываем имя миграции БЕЗ даты)
dotnet ef database update <ИМЯ_МИГРАЦИИ> \
  --project GoogleClassroom/GoogleClassroom.csproj
```

---

## 13. Логи и мониторинг

### Вариант A/B — Docker

```bash
# Логи приложения в реальном времени
docker logs gcapp_api -f

# Последние 100 строк логов
docker logs gcapp_api --tail=100

# Логи с временными метками
docker logs gcapp_api -f --timestamps

# Логи PostgreSQL
docker logs gcapp_db -f
```

### Вариант C — systemd

```bash
# Логи приложения в реальном времени
sudo journalctl -u gcapp -f

# Логи за сегодня
sudo journalctl -u gcapp --since today

# Логи за последний час
sudo journalctl -u gcapp --since "1 hour ago"
```

### Nginx логи

```bash
# Логи доступа (все входящие запросы)
sudo tail -f /var/log/nginx/access.log

# Логи ошибок
sudo tail -f /var/log/nginx/error.log
```

---

## 14. Обновление приложения

### Вариант A — Docker Compose

```bash
cd /opt/gcapp

# Получаем новый код
git pull origin main

# Пересобираем образ и перезапускаем (с нулевым даунтаймом данных)
docker compose up --build -d

# Проверяем
docker compose ps
docker compose logs app -f
```

### Вариант C — systemd

```bash
cd /opt/gcapp

# Получаем новый код
git pull origin main

# Пересобираем
dotnet publish ./GoogleClassroom/GoogleClassroom.csproj \
  -c Release \
  -o /opt/gcapp/publish \
  --runtime linux-x64 \
  --self-contained false

# Перезапускаем сервис
sudo systemctl restart gcapp
sudo systemctl status gcapp
```

---

## 15. CI/CD через GitHub Actions

Эта конфигурация автоматически деплоит приложение на сервер при каждом push в ветку `main`.

### 15.1 Добавление SSH-ключа сервера в GitHub Secrets

На **локальной машине** или **сервере** сгенерируй SSH-ключ для деплоя:

```bash
ssh-keygen -t ed25519 -C "github-actions-deploy" -f ~/.ssh/deploy_key -N ""
cat ~/.ssh/deploy_key.pub    # публичный ключ
cat ~/.ssh/deploy_key        # приватный ключ
```

**На сервере** добавь публичный ключ в список авторизованных:

```bash
echo "<СОДЕРЖИМОЕ_deploy_key.pub>" >> ~/.ssh/authorized_keys
chmod 600 ~/.ssh/authorized_keys
```

**В репозитории GitHub** перейди: `Settings` → `Secrets and variables` → `Actions` → `New repository secret`

Добавь следующие секреты:

| Имя секрета | Значение |
|---|---|
| `SERVER_HOST` | IP-адрес или домен сервера |
| `SERVER_USER` | имя пользователя (например, `appuser`) |
| `SERVER_SSH_KEY` | содержимое файла `~/.ssh/deploy_key` (приватный ключ) |
| `SERVER_PORT` | `22` (или другой SSH-порт) |

### 15.2 Создание файла workflow

Создай файл в репозитории: `.github/workflows/deploy.yml`

```yaml
name: Deploy to Production

on:
  push:
    branches: [ main ]

jobs:
  deploy:
    runs-on: ubuntu-latest
    steps:
      - name: Checkout code
        uses: actions/checkout@v4

      - name: Deploy via SSH
        uses: appleboy/ssh-action@v1.0.3
        with:
          host: ${{ secrets.SERVER_HOST }}
          username: ${{ secrets.SERVER_USER }}
          key: ${{ secrets.SERVER_SSH_KEY }}
          port: ${{ secrets.SERVER_PORT }}
          script: |
            cd /opt/gcapp
            git pull origin main
            docker compose up --build -d
            docker compose ps
```

### 15.3 Дополнительно — запуск тестов перед деплоем

```yaml
name: CI/CD Pipeline

on:
  push:
    branches: [ main ]

jobs:
  test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '9.0.x'

      - name: Restore dependencies
        run: dotnet restore

      - name: Run unit tests
        run: dotnet test Tests/Tests.csproj --no-restore --verbosity normal

  deploy:
    needs: test       # деплой только после успешных тестов
    runs-on: ubuntu-latest
    steps:
      - name: Deploy via SSH
        uses: appleboy/ssh-action@v1.0.3
        with:
          host: ${{ secrets.SERVER_HOST }}
          username: ${{ secrets.SERVER_USER }}
          key: ${{ secrets.SERVER_SSH_KEY }}
          port: ${{ secrets.SERVER_PORT }}
          script: |
            cd /opt/gcapp
            git pull origin main
            docker compose up --build -d
```

---

## 16. Устранение типичных ошибок

### Ошибка: `connection refused` на порту 8080

```bash
# Проверяем, запущено ли приложение
docker compose ps              # вариант A/B
sudo systemctl status gcapp    # вариант C

# Смотрим логи на наличие ошибок
docker compose logs app
```

### Ошибка: `Failed to connect to database`

```bash
# Проверяем, запущен ли PostgreSQL
docker compose ps              # контейнер db должен быть Up
# или
sudo systemctl status postgresql

# Проверяем строку подключения
docker exec gcapp_api env | grep ConnectionStrings
```

### Ошибка: `502 Bad Gateway` в Nginx

```bash
# Nginx работает, но приложение не отвечает на 8080
curl http://127.0.0.1:8080/swagger/index.html
# Если ошибка — проблема в приложении, смотри его логи
docker compose logs app --tail=50
```

### Ошибка при запуске: `Invalid JWT secret`

Убедись, что JWT секрет содержит **минимум 32 символа** (256 бит). Слишком короткий ключ вызовет исключение при старте.

```bash
# Генерация надёжного ключа
openssl rand -base64 48
```

### Ошибка: `Permission denied` для папки uploads

```bash
# Вариант C (systemd)
sudo chown -R appuser:appuser /opt/gcapp/uploads
sudo chmod 755 /opt/gcapp/uploads

# Вариант A/B (Docker) — volume управляется Docker, обычно не требуется
docker exec gcapp_api ls -la /app/uploads
```

### Ошибка при миграции: `relation already exists`

Это значит, что часть таблиц уже создана. Приложение применяет только **ожидающие** миграции, поэтому повторного запуска бояться не стоит. Если ошибка критическая:

```bash
# Смотрим лог миграций в БД
docker exec gcapp_db psql -U gcuser -d GcDb -c \
  "SELECT migration_id, product_version FROM \"__EFMigrationsHistory\" ORDER BY migration_id;"
```

---

## Быстрый старт (краткая шпаргалка)

```bash
# 1. Клонируем репозиторий
git clone https://github.com/russkoepoleexperimentov/DeveloptmentProcessesHITS.git /opt/gcapp
cd /opt/gcapp

# 2. Создаём docker-compose.yml (см. раздел 6.1) и заполняем пароли

# 3. Запускаем
docker compose up --build -d

# 4. Устанавливаем Nginx и настраиваем прокси (см. раздел 9)

# 5. Получаем HTTPS сертификат (если есть домен)
sudo certbot --nginx -d <ДОМЕН>

# 6. Проверяем
curl http://localhost:8080/swagger/index.html
```

---

*Документ подготовлен для проекта [DeveloptmentProcessesHITS](https://github.com/russkoepoleexperimentov/DeveloptmentProcessesHITS)*
