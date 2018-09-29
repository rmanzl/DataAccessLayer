
namespace RobinManzl.DataAccessLayer
{
    
    /// <summary>
    /// Schnittstelle, welche die Basis jeder Entitätsklasse darstellt
    /// </summary>
    public interface IEntity
    {

        /// <summary>
        /// Der Primärschlüssel der Tabelle
        /// </summary>
        int Id { get; set; }

    }

}
