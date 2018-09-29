
namespace DataAccessLayer.Query
{

    public class QueryOptions
    {

        public int? MaxRowCount { get; set; }

        public string OrderByColumn1 { get; set; }

        public string OrderByColumn2 { get; set; }

        public string OrderByColumn3 { get; set; }

        public bool OrderDescending { get; set; }

    }

}
