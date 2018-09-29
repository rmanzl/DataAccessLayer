using System;

namespace RobinManzl.DataAccessLayer
{

    /// <inheritdoc />
    /// <summary>
    /// Stellt die Basis für das Table- und das View-Attribut dar
    /// </summary>
    public abstract class TableBaseAttribute : Attribute
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
        protected TableBaseAttribute(string name = null, string schema = null)
        {
            Name = name;
            Schema = schema;
        }

    }

}
