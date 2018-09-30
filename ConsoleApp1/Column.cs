using RobinManzl.DataAccessLayer;
using RobinManzl.DataAccessLayer.Attributes;

namespace ConsoleApp1
{

    [Table(Schema = "mdl", Name = "Column")]
    internal class Column : IEntity
    {

        [Column]
        public int Id { get; set; }

        [Column]
        public string Name { get; set; }

        [Column]
        public int Test { get; set; }

    }

}
