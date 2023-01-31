using auth.mod;
using GeomHelper;
using landrope.mod;
using landrope.mod.shared;
using landrope.mod2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CadLoader
{
    public class PTSKIMapper
    {
        protected LandropeContext context;
        protected ExtLandropeContext contextex;
        protected string ptskkey;
        protected Shapes areas = new Shapes();

        public PTSKIMapper(LandropeContext context, ExtLandropeContext contextex, string key)
        {
            this.context = context;
            this.contextex = contextex;
            ptskkey = key;
            CheckExistence();
        }

        public PTSKIMapper(LandropeContext context, ExtLandropeContext contextex, PTSK ptsk)
        {
            this.context = context;
            this.contextex = contextex;
            ptskkey = ptsk.key;
        }

        public bool PutUTMs(UtmPoint[][] Utms, int Zone, bool south)
        {
            //MyTracer.PushProc(MethodBase.GetCurrentMethod().Name, "PU");
            try
            {

                areas.Clear();
                areas.AddRange(Utms.Select(u => new Shape { coordinates = UtmConv.UTM2LatLon(u, Zone, south).ToList() }));

                return true;
            }
            finally
            {

            }
        }

        public bool Store(PTSK ptsk)
        {
            try
            {
                var ent = contextex.ptsk.FirstOrDefault(x => x.key == ptsk.key);

                if (ent != null)
                {
                    //ent.SetAreas(this.areas);
                    ent.SetAreasEncode(this.areas);
                    contextex.ptsk.Update(ent);
                }
                else
                {
                    ent = ptsk;
                    //ent.SetAreas(this.areas);
                    ent.SetAreasEncode(this.areas);
                    contextex.ptsk.Insert(ent);
                }

                contextex.SaveChanges();

                return true;
            }
            catch (Exception ex)
            {

                return false;
            }
            finally
            {

            }
        }

        protected void CheckExistence()
        {
            var map = contextex.ptsk.FirstOrDefault(p => p.key == ptskkey);
            if (map == null)
                throw new Exception("Invalid Land's key given");
        }
    }
}
