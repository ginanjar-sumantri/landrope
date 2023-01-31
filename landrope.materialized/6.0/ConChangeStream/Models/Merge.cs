namespace ConChangeStream.Models;
public class Merge
{
    public string db {get;set;}
    public string view {get;set; }
    public string destination {get;set;}
    public string key {get;set;}
    public bool specific {get;set;}
    public string urlendpoint {get;set; }
}