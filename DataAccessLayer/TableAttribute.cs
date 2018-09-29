using System;

namespace DataAccessLayer
{

    /// <inheritdoc />
    /// <summary>
    /// Dieses Attribute kann verwendet werden, um einen vom Klassennamen abweichenden Tabellennamen zu definieren
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class TableAttribute : Attribute
    {

        /// <summary>
        /// Gibt das verwendete Schema der Tabelle an, falls das Standardschema (dbo) verwendet wurde, kann dieses Attribute auch auf NULL gesetzt werden
        /// </summary>
        public string Schema;

        /// <summary>
        /// Gibt den physikalischen Tabellennamen an
        /// </summary>
        public string Name;

        /// <inheritdoc />
        /// <summary>
        /// Standardkonstruktor, welcher alle Attribute setzt
        /// </summary>
        /// <param name="name">
        /// Wert für den Tabellennamen
        /// </param>
        /// <param name="schema">
        /// Schema der Tabelle
        /// </param>
        public TableAttribute(string name = null, string schema = null)
        {
            Name = name;
            Schema = schema;
        }

    }

}
