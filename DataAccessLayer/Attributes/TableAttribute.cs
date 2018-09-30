using System;

namespace RobinManzl.DataAccessLayer.Attributes
{

    /// <inheritdoc />
    /// <summary>
    /// Dieses Attribute kann verwendet werden, um einen vom Klassennamen abweichenden Tabellennamen zu definieren
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class TableAttribute : TableBaseAttribute
    {

        /// <inheritdoc />
        public TableAttribute(string name = null, string schema = null)
            : base(name, schema)
        {
        }

    }

}
