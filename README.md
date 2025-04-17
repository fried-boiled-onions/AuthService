# Messenger Backend

Этот репозиторий содержит backend для мессенджера. Основная задача — обработка бизнес-логики, взаимодействие с базой данных и предоставление REST и WebSocket API для фронтенда.

## Содержание
- [Стек технологий](#стек-технологий)
- [Установка и запуск](#установка-и-запуск)
- [Конфигурация](#конфигурация)
- [Тестирование](#тестирование)
- [Контакты](#контакты)

---

## Стек технологий
- **Язык программирования:** C#
- **Фреймворк:** ASP.NET Core
- **Авторизация:** JWT
- **Message Broker:** RabbitMQ
- **База данных:** PostgreSQL (хранится в [отдельном репозитории](https://github.com/fried-boiled-onions/messenger-DB))
- **Хранение файлов:** S3-compatible storage

---

## Установка и запуск
### 1. Клонирование репозитория
```bash
git clone https://github.com/fried-boiled-onions/messenger-backend.git
cd messenger-backend
```

### 2. Настройка переменных окружения
Создайте файл `.env` в корне проекта со следующими переменными:
```env
JWT_SECRET=your_jwt_secret
DB_CONNECTION=Host=localhost;Port=5432;Database=messenger;Username=your_user;Password=your_password
RABBITMQ_CONNECTION=amqp://guest:guest@localhost:5672/
S3_ENDPOINT=http://localhost:9000
S3_ACCESS_KEY=your_access_key
S3_SECRET_KEY=your_secret_key
```

### 3. Установка зависимостей
```bash
dotnet restore
```

### 4. Запуск сервера
```bash
dotnet run
```

Сервер будет запущен на `http://localhost:5000`.

---

## Конфигурация
### База данных
Для работы backend необходима база данных. Схемы и миграции находятся в [репозитории базы данных](https://github.com/fried-boiled-onions/messenger-DB.git). Убедитесь, что база данных запущена и настроена.

### RabbitMQ
Убедитесь, что RabbitMQ запущен и доступен по адресу, указанному в переменных окружения.

### S3-compatible storage
Для хранения файлов используйте любой совместимый с S3 сервис, например, MinIO.

---

## Тестирование
Для запуска тестов выполните:
```bash
dotnet test
```
