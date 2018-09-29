using System;

namespace RobinManzl.DataAccessLayer
{

    /// <inheritdoc />
    /// <summary>
    /// Dieses Attribute kann verwendet werden, um einen vom Klassennamen abweichenden Viewnamen zu definieren
    /// Zusätzlich kann mithilfe dieses Attributes angegeben werden, mittels welcher Stored Procedures die CRUD-Vorgänge durchgeführt werden können
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class ViewAttribute : TableBaseAttribute
    {

        /// <summary>
        /// Gibt das verwendete Schema der Stored Procedures an, falls das Standardschema (dbo) verwendet wurde, kann dieses Attribute auch auf NULL gesetzt werden
        /// </summary>
        public string ProcedureSchema;

        /// <summary>
        /// Gibt den Namen der Stored Procedure an, welche für die Anlage einer Zeile in der View verwendet werden soll
        /// </summary>
        public string InsertProcedure;

        /// <summary>
        /// Gibt den Namen der Stored Procedure an, welche für die Änderung einer Zeile in der View verwendet werden soll
        /// </summary>
        public string UpdateProcedure;

        /// <summary>
        /// Gibt den Namen der Stored Procedure an, welche für das Löschen einer Zeile in der View verwendet werden soll
        /// </summary>
        public string DeleteProcedure;

        /// <inheritdoc />
        public ViewAttribute(string name = null, string schema = null)
        : base(name, schema)
        {
        }

    }

}
