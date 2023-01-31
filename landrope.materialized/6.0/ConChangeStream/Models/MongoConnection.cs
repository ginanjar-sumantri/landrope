using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConChangeStream.Models;
internal class MongoConnection
{
    public string protocol { get; set; }
    public string encode { get; set; }
    public string server { get; set; }
    public string uid { get; set; }
    public string pwd { get; set; }
    public string ssl { get; set; }
    public string database { get; set; }
    public string replica { get; set; }
}
