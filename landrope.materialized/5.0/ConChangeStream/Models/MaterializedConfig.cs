namespace ConChangeStream.Models
{
    public class MaterializedConfig
    {
        public string database {get; set;}
        public string collection {get; set;}
        public Merge[] merges {get; set;}
    }
}