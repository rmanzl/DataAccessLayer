using System;

namespace RobinManzl.DataAccessLayer.Exceptions
{

    /// <inheritdoc />
    /// <summary>
    /// Exception, welche auftritt, wenn eine Entitätsklasse ohne Primärschlüsselattribute angegeben wurde
    /// </summary>
    public class InvalidEntityClassException : Exception
    {
        
        internal InvalidEntityClassException(string message)
            : base(message)
        {
        }

    }

}
