using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace landrope.scheduler.Enums
{
    public enum ApiService
    {
        Api1 = 1,
        Api2 = 2,
        Api3 = 3
    }

    public static partial class EnumHelpers
    {
        public static int GetPort(this ApiService val)
            => val switch
            {
                ApiService.Api1 => 7878,
                ApiService.Api2 => 7879,
                ApiService.Api3 => 7880
            };
    }
}
