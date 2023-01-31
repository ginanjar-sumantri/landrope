using auth.mod;
using mongospace;
using System;
using System.Collections.Generic;
using System.Text;

namespace landrope.mod2
{
    [Entity("PersilCategories", "persil_categories")]
    public class PersilCategories : entity
    {
        public string keyPersil { get; set; }
        public category[] categories1 { get; set; } = new category[0];
        public category[] categories2 { get; set; } = new category[0];
        public category[] categories3 { get; set; } = new category[0];
    }
}