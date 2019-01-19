## Vorbereitung

### Datenbank

Bevor das Framework verwendet werden kann, muss eine Datenbanktabelle erstellt werden.
Das Framework geht von Tabellen mit mindestens einem Attribut aus, das mit dem PrimaryKeyAttribute als Primärschlüssel markiert wurde.
Diese Spalte muss als Identitätsspalte festgelegt werden, während alle restlichen Attribute der Tabelle sind frei wählbar.

### Entitätsklassen

Um die Daten einer erstellten Tabelle abfragen zu können, muss eine passende Entitätsklasse erstellt werden, wobei der Klassenname dem Tabellennamen entsprechen muss.
Jede Property, welche von der Datenbanktabelle gemappt werden soll, muss mit dem ColumnAttribut versehen werden.
Hier gilt auch wieder Einschränkung, dass die Properties dieselben Namen wie die Spalten in der Tabelle besitzen müssen.

## Daten abrufen

Zeilen einer Tabelle kann mit Hilfe der **DbService**-Klasse abgerufen werden.
Diese Klasse ist generisch und erwartet als Typparameter die Entitätsklasse, welche zu der entsprechenden Tabelle passt.
Mit der Methode **GetEntities** können die Objekte dann von der Tabelle abgerufen werden.

### QueryCondition

Um die **WHERE**-Klausel der Abfrage anzugeben, dient die Klasse **QueryCondition**.
Sie ist abstrakt und wird von diversen anderen Klassen geerbt.
Es ist beispielsweise mit der **ValueCompareCondition**-Klasse möglich, die Zeilen anhand einem Wertvergleich zu filtern.
Alternativ kann auch eine Expression vom Typ **Func<T,bool>** angegeben werden, wobei **T** der Entitätsklasse entspricht.

### QueryOptions

Die Klasse **QueryOptions** dient dazu, dem **DbService** diverse Optionen zu übergeben.
Z.B. kann angegeben werden, wie viele Zeilen maximal von der Datenbank zurückgegeben werden sollen.
Die Angabe von bis zu drei Spalten für die Sortierung kann auch über diese Klasse bewerkstelligt werden.

## Daten ändern

Daten können mithilfe der **InsertEntity**-, der **UpdateEntity**- und der **DeleteEntity**-Methoden verändert werden unter der Angabe einer Instanz der Entitätsklasse.

## Besonderheiten

### TableAttribute

Mit dem **TableAttribute** ist es möglich, die Entitätsklasse anders zu benennen, wie die zugehörige Tabelle.
Zusätzlich kann noch ein Schema angegeben werden, welches die Tabelle verwendet.
Bei der Verwendung des Standardschemas - **dbo** - kann dieses Feld ignoriert werden.

### ColumnAttribute

Das **ColumnAttribute** bietet, ähnlich wie das **TableAttribute**, die Möglichkeit, einen vom Property-namen abweichenden Spaltennamen zu verwenden.

### Views

Es gibt die Möglichkeit, Daten auch direkt von Views abzufragen.
Hierfür existiert ein eigenes Attribut, welches anstelle des **TableAttribute** verwendet werden soll.
Das **ViewAttribute** besitzt zusätzlich zum Namen der View noch Felder für die Angabe von Stored Procedures, die bei den Methoden zur Änderung von Zeilen verwendet werden.
Die beiden Stored Procedures, welche für Insert- und Update-Vorgänge verwendet werden, sollten für jedes Attribut der Entitätsklasse einen entsprechenden Parameter besitzen.
Die letzte Stored Procedure für Löschvorgänge hingegen sollte nur den Parameter für den Primärschlüssel besitzen.

### Transaktionen

Transaktionen werden mittels den Methoden **BeginTransaction** und **RemoveTransaction** begonnen und entfernt. Das Abbrechen bzw. das Anwenden der Transaktion muss direkt mit der zurückgelieferten **SqlTransaction** erledigt werden. Um Transaktionen über mehrere Instanzen der **DbService**-Klasse zu verwenden, akzeptiert die **BeginTransaction**-Methode optional eine **SqlTransaction**.
