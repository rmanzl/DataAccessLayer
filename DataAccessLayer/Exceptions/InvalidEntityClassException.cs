using System;

namespace RobinManzl.DataAccessLayer.Exceptions
{

    /// <inheritdoc />
    /// <summary>
    /// Exception, welche auftritt, wenn eine Entitätsklasse ohne Primärschlüsselattribute angegeben wurde
    /// </summary>
    public class InvalidEntityClassException : Exception
    {
        
        internal InvalidEntityClassException()
        : base("Specified entity class is not in the expected format. Make sure the class contains one property marked with a PrimaryKeyAttribute.")
        {
        }

    }

}
