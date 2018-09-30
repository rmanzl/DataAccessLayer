using System;

namespace RobinManzl.DataAccessLayer.Attributes
{
    
    /// <inheritdoc />
    /// <summary>
    /// Attribut, welches für die Kennzeichnung von Properties genutzt werden kann, die Teil der Tabellendefinition sind
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class ColumnAttribute : Attribute
    {

        /// <summary>
        /// Falls der Datenbankattributname vom Namen der zugehörigen Property abweicht, kann dieser mittels dieses Feldes angegeben werden
        /// </summary>
        public string Name;

        /// <inheritdoc />
        /// <summary>
        /// Standardkonstruktor, der den Namen der Spalte angibt
        /// </summary>
        /// <param name="name">
        /// Der Name der Spalte
        /// </param>
        public ColumnAttribute(string name = null)
        {
            Name = name;
        }

    }

}
