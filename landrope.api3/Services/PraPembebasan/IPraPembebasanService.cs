using landrope.common;
using System.Threading.Tasks;

namespace landrope.api3.Services.PraPembebasan
{
    public interface IPraPembebasanService
    {
        Task<PraPembebasanCore> Get(string key);
        Task<byte[]> GetDetailDocumentReportPdf(string key);
    }
}
