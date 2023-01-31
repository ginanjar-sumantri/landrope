using auth.mod;
using binaland;
using landrope.common;
using landrope.mod;
using landrope.mod.shared;
using MongoDB.Bson.Serialization.Attributes;
using mongospace;
using System;
using System.Collections.Generic;
using System.Text;

namespace landrope.mod2
{
    [Entity("ptsk", "masterdatas")]
    public class PTSK : Company
    {
        public DateTime? terbit { get; set; }
        public string nomor { get; set; }
        public byte[] careas { get; set; }
        public void SetAreasEncode(Shapes value)
        {
            careas = landbase.encode(value);
        }

        public PTSKView toView()
        {
            var view = new PTSKView();
            (view.key, view.identifier, view.code, view.terbit, view.nomor, view.careas) =
                (key, identifier, code, terbit, nomor, careas);

            return view;
        }
        public void FromCore(PTSKCore core)
        {
            (identifier, code, terbit, nomor) = (core.identifier, core.code, core.terbit, core.nomor);
        }

        public void FromCore(string identifier, string code, DateTime? terbit, string nomor)
        {
            (this.identifier, this.code, this.terbit, this.nomor) = (identifier, code, terbit, nomor);
        }
    }

    public class skhistory
    {
        public string keyPTSK { get; set; }
        public DateTime tanggal { get; set; }
        public string keyCreator { get; set; }

        public SKHistoriesView toView(Persil persil, ExtLandropeContext context)
        {
            var view = new SKHistoriesView();
            (var project, var desa) = context.GetVillage(persil.basic.current.keyDesa);
            var ptskdesc = context.ptsk.FirstOrDefault(p => p.key == keyPTSK);

            (view.keyPersil, view.Project, view.Desa, view.keyPTSK, view.PTSK, view.NamaPemilik, view.AlasHak, view.luasSurat) =
                (persil.key, project.identity, desa.identity, keyPTSK, ptskdesc?.identifier,
                persil.basic.current.surat.nama, persil.basic.current.surat.nomor, persil.basic.current.luasSurat);

            return view;
        }

    }


}
