using System;

namespace RobinManzl.DataAccessLayer.Attributes
{

    /// <inheritdoc />
    /// <summary>
    /// Attribut, welches für die Kennzeichnung der Primärschlüssel-Attributes einer Entitätsklasse genutzt werden kann
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class PrimaryKeyAttribute : Attribute
    {
    }

}
