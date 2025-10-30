# BlyatOS 🧠💥

> Das Betriebssystem, das niemand wollte – aber trotzdem bootet.

---

## Was ist BlyatOS?

BlyatOS ist der verzweifelte Versuch, ein „richtiges“ Betriebssystem in **C#** zu schreiben, weil… warum eigentlich nicht?  
Gebaut auf dem legendären [Cosmos Framework](https://github.com/CosmosOS/Cosmos), das C# in etwas verwandelt, das tatsächlich auf echter Hardware startet (meistens).  

Ziel:  
Ein Mini-OS, das irgendwie Dateien lesen, Benutzer verwalten, VGA-Text anzeigen und *Bad Tetris* spielen kann – also alles, was ein moderner Kernel eben braucht.  

---

## Features (oder zumindest Dinge, die passieren)

- 🧩 **Bootet** – das allein ist schon Grund genug für Applaus.  
- 🧠 **Kernel.cs** – der Ort, an dem Träume und Exceptions kollidieren.  
- 🪵 **Eigenes „Dateisystem“** (mehr Konzept als Realität).  
- 👤 **User-Management** (mit so viel Sicherheit wie eine offene WG-Küche).  
- 🎮 **Bad Tetris** – für den Fall, dass du vergessen hast, warum du das Projekt gestartet hast.  
- 🧰 **VGACursorFix.cs** – denn manchmal ist der Cursor einfach woanders.  
- 💥 **GenericException.cs** – fängt Fehler, die du gar nicht wusstest, dass du sie hattest.  

---

## Aufbau des Projekts

```
BlyatOS/
├── Kernel.cs              # Herzstück, aka der magische Rauchgenerator
├── UserManagementApp.cs   # Benutzerverwaltung, fast wie Windows 95
├── BlyatgamesApp.cs       # Unterhaltung auf BIOS-Niveau
├── Library/
│   ├── Functions/
│   │   ├── BadTetris.cs
│   │   ├── BasicFunctions.cs
│   │   └── UserManagement.cs
│   ├── Helpers/
│   │   ├── ReadDisplay.cs
│   │   ├── PathHelpers.cs
│   │   └── GenericException.cs
│   ├── Configs/
│   │   └── UsersConfig.cs
│   └── Startupthings/
│       └── OnStartUp.cs
└── isoFiles/
    ├── Einleitung.txt     # Das Manifest des Wahnsinns
    └── kusche256.raw      # Vermutlich ein Artefakt aus einer besseren Zeit
```

---

## Wie man es **nicht** installiert

1. Öffne Visual Studio.  
2. Ignoriere alle Fehlermeldungen von Cosmos.  
3. Drücke **Build**.  
4. Warte.  
5. Frage dich, warum du das tust.  
6. Starte das ISO in VirtualBox oder VMware.  
7. Wenn du Text siehst: Gratuliere, du hast BlyatOS erfolgreich zum Leben erweckt.  
8. Wenn du nur schwarzen Bildschirm siehst: Das ist **minimalistische Kunst**.  

---

## Bekannte Probleme (aka Feature-Liste 2)

- VGA-Ausgabe lebt ihr eigenes Leben.  
- User-Configs vergessen manchmal, dass sie existieren.  
- Tetris ist … nennen wir’s **pädagogisch wertvoll**.  
- Cosmos-Builds funktionieren genau dann nicht, wenn du stolz bist.  
- Speicherverwaltung? Ja, irgendwann vielleicht.  

---

## Warum das alles?

Weil’s geht.  
Weil C# und Cosmos.  
Weil man irgendwann „BlyatOS“ gesagt hat und das Universum keine Wahl mehr hatte.  

---

## Mitmachen

Wenn du auch der Meinung bist, dass Betriebssysteme zu stabil geworden sind:  
1. Forke das Projekt.  
2. Schreib irgendwas rein.  
3. Wenn’s bootet, ist es ein Feature.  
4. Wenn nicht – willkommen im Team.  

---

## Lizenz

Frei nach dem Motto:  
> „Mach, was du willst, aber beschwer dich nicht, wenn’s raucht.“  

Vermutlich **MIT License**, aber lies lieber die Datei – oder frag dein Gewissen.

---

## Danksagung

- Dem **Cosmos-Team**, das’s überhaupt möglich macht, C# im BIOS-Modus zu verwenden.  
- Meinem Geduldsfaden.  
- Kaffee.  
- Und natürlich: **dem Blyat selbst**, ohne das dieses Projekt nie so heißen dürfte.

---

> *BlyatOS – Es lebt. Manchmal.*
