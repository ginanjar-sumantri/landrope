using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using auth.mod;
using landrope.common;
using mongospace;

namespace landrope.mod2
{

    [Entity("category", "categories_new")]
    public class Category : entity
    {
        public Category()
        {

        }

        public StatusCategory bebas { get; set; }
        public int segment { get; set; }
        public ushort? value { get; set; }
        public string desc { get; set; }
        public string shortDesc { get; set; }


        public void FromCore(CategoryCore core)
        {
            (segment, bebas, desc, shortDesc) =
                             (core.segment, core.bebas, core.desc, core.shortDesc);
        }

        public CategoryView toView()
        {
            var view = new CategoryView();

            (view.key, view.bebas, view.segment, view.value, view.desc, view.shortDesc) = (key, bebas, segment, value, desc, shortDesc);

            return view;
        }
    }

    public class category //For Categories
    {
        public category(user user)
        {
            keyCreator = user.key;
        }
        public DateTime tanggal { get; set; }
        public string[] keyCategory { get; set; }
        public string keyCreator { get; set; }

        public CategoriesView toView(Persil persil, ExtLandropeContext context, List<Category> allCat)
        {
            string cat = string.Empty;
            var view = new CategoriesView();

            (var project, var desa) = context.GetVillage(persil.basic.current?.keyDesa);

            var ncats = keyCategory.Join(allCat, k => k, c => c.key, (k, c) => c).ToArray();
            var cats = string.Join(" ", ncats.Select(c => c.desc).ToArray());
            var shcats = string.Join(" ", ncats.Select(c => c.shortDesc).ToArray());

            (view.keyPersil, view.Project, view.Desa, view.keyCategories, view.Category, view.shortCategory,
                    view.NamaPemilik, view.AlasHak, view.luasSurat) =
                (persil.key, project?.identity, desa?.identity, keyCategory, cats, shcats,
                    persil.basic.current?.surat.nama, persil.basic.current?.surat.nomor, persil.basic.current?.luasSurat);

            return view;
        }
    }

    public class StateHistories
    {
        public StateHistories(user user)
        {
            keyCreator = user.key;
        }
        public StatusBidang en_state { get; set; }
        public DateTime date { get; set; }
        public string keyCreator { get; set; }
    }
}
