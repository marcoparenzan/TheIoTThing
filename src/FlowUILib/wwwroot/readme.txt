*ai*

# Flow Editor - Prompt

Genera una applicazione **single-file vanilla HTML** con **Tailwind CSS** (via CDN) che implementi un **editor visuale di flow/diagrammi a blocchi**.

## Architettura

- **Un unico file `index.html`** con HTML, CSS inline/Tailwind e JavaScript vanilla (no framework, no build tools)
- Tailwind CSS caricato via CDN (`<script src="https://cdn.tailwindcss.com">`)
- Tutto il codice JS in un tag `<script>` nel file
- Classi CSS Tailwind per tutto il layout e lo styling

## Layout dell'interfaccia

L'interfaccia è divisa in:

1. **Toolbar superiore**: contiene i pulsanti Nuovo, Apri, Salva, Zoom In, Zoom Out, Zoom Reset
2. **Pannello laterale sinistro (Palette)**: contiene i blocchi trascinabili organizzati per categoria:
   - **Blocchi di Ingresso** (colore verde): rappresentano input di dati, sorgenti, sensori
   - **Blocchi di Elaborazione** (colore blu): rappresentano trasformazioni, calcoli, logica
   - **Blocchi di Uscita** (colore arancione/rosso): rappresentano output, destinazioni, attuatori
3. **Area di lavoro centrale (Canvas)**: area con griglia di sfondo dove si posizionano e collegano i blocchi
4. **Pannello laterale destro (Proprietà)**: mostra e permette di modificare le proprietà del blocco selezionato

## Funzionalità dei Blocchi

Ogni blocco ha:
- **id**: identificatore univoco generato automaticamente
- **type**: tipo del blocco (`input`, `process`, `output`)
- **name**: nome visualizzato sul blocco (editabile)
- **x, y**: posizione sul canvas
- **config**: oggetto JSON libero per configurazione personalizzata
- **color**: colore assegnato in base al tipo (personalizzabile)

### Tipi di blocco nella palette:

**Ingresso (verdi):**
- Data Source
- API Input
- File Reader
- Sensor
- Manual Input

**Elaborazione (blu):**
- Transform
- Filter
- Aggregate
- Condition (If/Else)
- Script

**Uscita (arancioni/rossi):**
- Data Output
- API Output
- File Writer
- Display
- Notification

## Interazioni richieste

### Drag & Drop
- Trascinare un blocco dalla palette al canvas per aggiungerlo
- Trascinare un blocco esistente sul canvas per spostarlo
- Il blocco si aggancia alla griglia (snap-to-grid)

### Selezione e modifica
- Click su un blocco per selezionarlo (evidenziato con bordo)
- Il pannello proprietà mostra: nome, tipo, colore, configurazione JSON
- Si può rinominare il blocco dal pannello proprietà
- Si può modificare il JSON di configurazione con un textarea
- Tasto Canc o pulsante per rimuovere il blocco selezionato

### Connessioni tra blocchi
- Ogni blocco ha un **punto di connessione in uscita** (destra) e uno **in ingresso** (sinistra)
- Trascinare da un punto di uscita a un punto di ingresso per creare una connessione (linea/freccia)
- Le connessioni sono visualizzate come curve Bezier su un layer SVG
- Click su una connessione per selezionarla, Canc per rimuoverla

### Zoom e Griglia
- Zoom In / Zoom Out con pulsanti e rotella del mouse (Ctrl+Scroll)
- La griglia di sfondo si scala con lo zoom
- Visualizzazione del livello di zoom corrente (es. "100%")
- Zoom Reset per tornare al 100%
- Pan dell'area di lavoro con click destro + trascinamento o middle mouse

### File Operations
- **Nuovo**: pulisce il canvas, chiede conferma se ci sono modifiche non salvate
- **Apri**: apre un file JSON dal filesystem locale (input file), carica blocchi e connessioni
- **Salva**: esporta tutto il flow come file JSON scaricabile

### Formato JSON del flow:
```json
{
  "name": "My Flow",
  "version": "1.0",
  "blocks": [
    {
      "id": "block_1",
      "type": "input",
      "name": "Data Source",
      "x": 100,
      "y": 200,
      "config": { "source": "database", "query": "SELECT * FROM users" },
      "color": "#22c55e"
    }
  ],
  "connections": [
    {
      "id": "conn_1",
      "from": "block_1",
      "to": "block_2"
    }
  ]
}
```

## Stile visivo

- Design moderno, scuro (dark theme) con sfondo `bg-gray-900`
- Griglia punteggiata o a linee sottili sul canvas
- Blocchi con bordi arrotondati, ombra, colore di sfondo in base al tipo
- Icone semplici (emoji o SVG inline) per ogni tipo di blocco
- Font mono per l'editor JSON
- Transizioni e animazioni fluide
- Cursore appropriato per ogni azione (grab, pointer, crosshair)

## Vincoli tecnici

- **Zero dipendenze** oltre Tailwind CSS CDN
- Compatibile con browser moderni (Chrome, Firefox, Edge)
- Responsive: funziona su schermi >= 1024px
- Tutto in un singolo file HTML
- Le connessioni sono renderizzate su un elemento SVG sovrapposto al canvas
- Performance: supportare almeno 50 blocchi senza rallentamenti
