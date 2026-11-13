# Size on VDI

Piccola applicazione Windows in italiano: spazio totale, usato e libero del volume del profilo e i 20 file più grandi, con dimensione in MB e percorso completo. Nessuna finestra PowerShell, nessuna cancellazione, nessun invio di dati. Scansione automatica all'apertura, aggiornamento manuale, interruzione e apertura della cartella del file selezionato.

## Ottenere l'EXE con GitHub

1. Crea un repository GitHub e carica il contenuto di questa cartella, inclusa la cartella nascosta `.github`.
2. Apri **Actions → Build Windows EXE**. La compilazione parte a ogni push; puoi anche usare **Run workflow**.
3. Apri un'esecuzione completata e scarica **SizeOnVdi-windows-x64** dalla sezione **Artifacts** (devi essere autenticato su GitHub).
4. Estrai lo ZIP e avvia `SizeOnVdi.exe` su Windows x64. Non occorre installare .NET o Python. L'EXE include il runtime, quindi pesa più di un semplice script.

Gli artifact restano disponibili per 30 giorni; riesegui il workflow per rigenerarli. Non vengono create release automaticamente. Il programma non è firmato: Windows o le policy aziendali possono chiedere conferma o impedirne l'avvio; in quel caso rivolgersi all'IT.

## Cosa misura

- Cerca ricorsivamente nella cartella del profilo dell'utente corrente, inclusi file nascosti e di sistema accessibili. Legge le dimensioni, non il contenuto dei file.
- Mostra i primi 20 in ordine decrescente, mantenendo solo 20 risultati in memoria.
- Cerca prima il volume etichettato `Profile-<nome utente>`, anche se montato senza lettera. Se non esiste, usa il volume che contiene la cartella del profilo e lo segnala chiaramente nella finestra. Questo ripiego è un'aggiunta rispetto al comando originale.
- Usa dimensioni logiche: come PowerShell, MB = 1.048.576 byte e GB = 1.073.741.824 byte. Compressione, file sparsi e segnaposto cloud possono occupare fisicamente uno spazio diverso.
- Lo spazio libero riguarda il volume, non necessariamente la quota personale o lo spazio del disco fisico che ospita il VHD della VDI.
- File o cartelle non accessibili vengono saltati. Non segue junction e collegamenti a cartelle, per evitare cicli e percorsi esterni al profilo. Il contatore dei saltati include questi casi e non stima quanti file contengano le cartelle escluse.
- I valori sono una fotografia durante l'analisi: i file possono cambiare nel frattempo. Lo spazio del volume viene letto all'inizio di ogni analisi.

## Sviluppo

Richiede .NET SDK 10. Interfaccia Windows Forms; nessun pacchetto NuGet di terze parti.

```sh
dotnet run --project tests/SizeOnVdi.Checks -c Release
dotnet build src/SizeOnVdi/SizeOnVdi.csproj -c Release
dotnet publish src/SizeOnVdi/SizeOnVdi.csproj -c Release -r win-x64 --self-contained true -o publish -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:DebugType=None -p:DebugSymbols=false
```

La compilazione è possibile anche da macOS con SDK e accesso ai pacchetti Microsoft; l'interfaccia e le API dei volumi devono essere provate su Windows. GitHub Actions esegue i controlli dello scanner e la compilazione su Windows.

Verifica manuale su una VDI: confronta i valori con `Get-Volume`, controlla un profilo con etichetta e uno senza, verifica percorsi lunghi, interruzione e apertura cartella. Le differenze nei risultati rispetto a PowerShell possono dipendere da permessi, collegamenti e file modificati durante la scansione.
