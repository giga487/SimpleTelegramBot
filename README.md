# SimpleTelegramBot

Applicazione ASP.NET Core/Blazor Server per inviare messaggi a un canale Telegram tramite REST API e pagine server-rendered.

## Configurazione

Modificare `appsettings.json`:

```json
{
  "Telegram": {
    "BotToken": "TOKEN_DEL_BOT",
    "ChatId": "@nome_canale_o_chat_id"
  },
  "RequestMemory": {
    "MaxEntries": 500
  }
}
```

Per ora il secret del bot è letto dal classico `appsettings.json`, come richiesto.

I log Serilog sono scritti sia su console sia nella cartella `Logs/`, con limite di circa 100 MB per file e massimo 30 file mantenuti.

## REST API

- `POST /api/telegram/messages`

  Body:

  ```json
  {
    "caller": "nome-chiamante",
    "message": "testo del messaggio"
  }
  ```

- `GET /api/telegram/requests?take=20&caller=nome-chiamante`

  Restituisce le ultime N richieste in memoria, raggruppate per `Caller`. Il filtro `caller` è opzionale.

- `DELETE /api/telegram/requests`

  Svuota la memoria accumulata.

## Pagine

- `/send`: form statico che invia tramite REST API, senza usare eventi/circuito Blazor Server.
- `/requests`: riepilogo della memoria con filtro per `Caller`, numero massimo di richieste e pulsante di cancellazione.

## CORS

Le pagine incluse chiamano le API dalla stessa origin, quindi CORS non serve. Se in futuro un client browser esterno dovesse chiamare direttamente le API, configurare:

```json
{
  "ApiCors": {
    "AllowedOrigins": [ "https://client.example" ]
  }
}
```

## Esecuzione

```powershell
dotnet run
```

## User secrets in sviluppo

In ambiente `Development` è preferibile mettere i dati sensibili nei .NET user secrets:

```powershell
dotnet user-secrets set "Telegram:BotToken" "TOKEN_DEL_BOT"
dotnet user-secrets set "Telegram:ChatId" "@nome_del_canale"
```

`Telegram:ApiBaseUrl` non deve contenere il token: deve restare `https://api.telegram.org`.
Se lo hai impostato per errore nei secrets, rimuovilo:

```powershell
dotnet user-secrets remove "Telegram:ApiBaseUrl"
```