using System;
using System.Collections.Generic;
using System.Data;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;
using ExcelDataReader;
using landrope.common;
using landrope.mod;
using landrope.mod2;
using MongoDB.Bson;
using MongoDB.Driver;
using mongospace;
using Newtonsoft.Json;

namespace landrope.import
{
  public static class DataLoader
  {
    public static string Load(LandropeContext context, ExtLandropeContext contextEx, string filepath, string sheetname, 
                              bool update, double marker, bool checkonly=false)
    {
      var strm = new FileStream(filepath, FileMode.Open, FileAccess.Read);
      var xreader = ExcelReaderFactory.CreateReader(strm);
      try
      {
        var res = xreader.AsDataSet();
        var dt = res.Tables.Cast<DataTable>().FirstOrDefault(d => d.TableName == sheetname);
        var sesx = contextEx.db.Client.StartSession();
        sesx.StartTransaction();
        var ses = context.db.Client.StartSession();
        ses.StartTransaction();
        try
        {
          var rcols = dt.Rows.Cast<DataRow>().FirstOrDefault(); // get first row
          if (rcols == null)
            throw new Exception("This worksheet contains no rows");
          var cols = rcols.ItemArray.Select(c => (c?.Equals(DBNull.Value) ?? true) ? null : c).ToArray(); //col names from first row
                                                                                                          //var indices = Enumerable.Range(1, cols.Length - 1).ToList(); // col numbers for enumeration
          var xcols = cols.Select((c, i) => (c, i)).Where(x => x.c != null).ToList();
          var discriminators = xcols.Where(x => x.c.Equals("basic.proses") || x.c.Equals("basic.jenis"))
            .Select(x => new KeyValuePair<string, int>(x.c.ToString().Replace("basic.", ""), x.i))
            .ToDictionary(x => x.Key, x => x.Value);
          if (discriminators.Count != 2)
            return "Invalid first row values of the workshet";

          var arrrows = dt.Rows.Cast<DataRow>().Select(x => x.ItemArray).ToArray();
          var rows = arrrows.Where(x => x[0] != null && !DBNull.Value.Equals(x[0]) && x[0].Equals(marker))
                                  .Select((cells, i) => (cells, i)).ToList();

          Console.WriteLine("");
          if (update)
            Console.WriteLine($"\"info\", \"rownum\", \"key\", \"proses\",\"jenis\",\"keyProject\",\"keyDesa\",\"tahap\",\"noPeta\",\"luasSurat\",\"luasDibayar\",\"satuan\",\"total\",\"group\",\"namasurat\"");
          foreach (var x in rows)
          {
            Console.Write($"importing row #{x.i}...");
            var data = x.cells.Select((dd, i) => (dd: (dd?.Equals(DBNull.Value) ?? true) ? null : dd, i))
              .Where(x => x.dd != null)
              .Join(xcols, dx => dx.i, cx => cx.i, (dx, cx) => (cx.c, d: dx.dd, dx.i))
              .ToArray();

            var ojenis = x.cells[discriminators["jenis"]];
            var oproses = x.cells[discriminators["proses"]];

            string jenis = (ojenis == null || DBNull.Value.Equals(ojenis)) ? null : ojenis.ToString();
            string proses = (oproses == null || DBNull.Value.Equals(oproses)) ? null : oproses.ToString();
            if (jenis == null && proses == null)
              throw new Exception($"Invalid document configuration (proses+jenis alas hak) on row {x.i}");

            var prok = Enum.TryParse(proses, out JenisProses en_proses);
            var jnok = Enum.TryParse(jenis, out JenisAlasHak en_jenis);

            if (!prok && !jnok)
              throw new Exception($"Invalid document configuration (proses+jenis alas hak) on row {x.i}");

            Persil persil = (en_proses, en_jenis) switch
            {
              (JenisProses.hibah, _) => new PersilHibah(),
              (_, JenisAlasHak.khusus) => new PersilHibah(),
              (_, JenisAlasHak.girik) => new PersilGirik(),
              (_, JenisAlasHak.shm) => new PersilSHM(),
              (_, JenisAlasHak.shp) => new PersilSHP(),
              (_, JenisAlasHak.hgb) => new PersilHGB(),
              (JenisProses.overlap, JenisAlasHak.unknown) => new PersilHGB (),
              (_, _) => null
            };
            if (persil == null)
              throw new Exception($"Invalid document configuration (proses+jenis alas hak) on row {x.i}");
            if (en_proses==JenisProses.overlap)
              persil.basic = new ValidatableShell<PersilBasic> { 
                current = new PersilBasic 
                { 
                  en_proses = JenisProses.overlap 
                } 
              };

            var dict = DataExtensions.MakeDict(persil.GetType());

            var paybatches = new[] { "%sudahBayar", "%sisaBayar" };
            var paydatas = data.Where(x => paybatches.Contains(x.c.ToString())).ToArray();

            var datax = data.Where(x => !paybatches.Contains(x.c.ToString()) && x.c.ToString() != "$key")
                        .Select(x => GetCombo(dict, x)).Where(x => x.props != null && x.mainprop != null).ToList();

            var keydata = data.FirstOrDefault(x => x.c.ToString() == "$key");
            //datax.ForEach(xx =>
            foreach (var xx in datax)
            {
              var mainobj = dict[xx.mainprop];
              var type = xx.props.Last().PropertyType;
              if (typeof(Nullable).IsAssignableFrom(type) && type.GenericTypeArguments.Any())
                type = type.GenericTypeArguments[0];
              var preprops = xx.props.SkipLast(1).ToArray();
              var obj = GetValue(preprops, mainobj);
              PutValue(xx.props, obj, xx.d, type, xx.findkey);
            }//);
            if (paydatas.Any())
            {
              var basic = dict["basic"] as PersilBasic;
              var total = basic.total;
              var sudah = paydatas.FirstOrDefault(x => x.c.ToString() == "%sudahBayar");
              var sisa = paydatas.FirstOrDefault(x => x.c.ToString() == "%sisaBayar");
              double dsudah = sudah.d != null ? (double)sudah.d : 0;
              double dsisa = sisa.d != null ? (double)sisa.d : 0;
              if (dsudah > 0d)
              {
                if ((total.HasValue && dsudah < total.Value) || dsisa > 0)
                {
                  var dp = (GroupDP)dict["gDP"];
                  dp.utama = new List<mod2.Rfp> { new mod2.Rfp { jumlah = dsudah } };
                }
                else
                {
                  var lunas = (GroupPelunasan)dict["gpelunasan"];
                  lunas.utama = new mod2.Rfp { jumlah = dsudah };
                }
              }
            }
            var OK = DataExtensions.SaveDict(dict, persil, update, checkonly, keydata.d?.ToString(), contextEx, x.i);
            if (!OK)
            {
              Console.ForegroundColor = ConsoleColor.Red;
              Console.Beep();
              Console.WriteLine("Failed");
              Console.ResetColor();
            }
            else
              Console.WriteLine("Okay");
          }
          //);
          contextEx.SaveChanges();
          ses.CommitTransaction();
          sesx.CommitTransaction();
        }
        catch (Exception)
        {
          ses.AbortTransaction();
          sesx.AbortTransaction();
          throw;
        }
      }
      catch (Exception ex)
      {
        var st = ex.Message;
        for (Exception exx = ex.InnerException; exx != null;exx=exx.InnerException)
          st += $"->{exx.Message}";
        return st;
      }
      finally
      {
        xreader.Close();
        strm.Close();
      }
      return null;

      object GetValue(PropertyInfo[] props, object data)
      {
        object val = data;
        foreach (PropertyInfo prop in props)
        {
          if (val == null)
            return null;
          val = prop.GetValue(val);
        }
        return val;
      }

      (object d, PropertyInfo[] props, string mainprop, string findkey, int i) GetCombo(
                                        Dictionary<string, ValidatableItem> dict, (object c, object d, int i) x)
      {
        var failvalue = (x.d, props: (PropertyInfo[])null, mainprop: (string)null, findkey: (string)null, x.i);
        var expropname = "?";
        try
        {
          string propname = x.c?.ToString();
          expropname = "1000;" + propname;
          if (string.IsNullOrWhiteSpace(propname))
            return failvalue;
          var parts = propname.Split('.');

          var mainprop = parts[0];
          expropname = "2000;" + mainprop;
          var mainobj = dict[mainprop];
          if (mainobj == null)
            throw new InvalidDataException("Inconsistence in persil type definition vs data...");

          var maintype = mainobj.GetType();
          propname = string.Join(".", parts.Skip(1));
          expropname = "3000;" + propname;
          var px = mainobj.GetPropInfo(propname, true);
          return (x.d, px.props, mainprop, px.findkey, x.i);
        }
        catch (Exception ex)
        {
          Console.WriteLine($"Error at {expropname} : {ex.Message}");
          return failvalue;
        }
      }

      void PutValue(PropertyInfo[] props, object obj, object value, Type type, string findkey)
      {
        var meth = typeof(JsonConvert).GetMethods().Where(m => m.Name == "DeserializeObject")
                    .FirstOrDefault(m => m.IsGenericMethod);
        if (meth == null)
          throw new Exception("Invalid library referemced, unable to found DeserializeObject<T> methos");
        meth = meth.MakeGenericMethod(type);
        var json = JsonConvert.SerializeObject(value);
        var newvalue = meth.Invoke(null, new object[] { json });
        if (!string.IsNullOrEmpty(findkey))
          newvalue = FindKey(context, contextEx, newvalue.ToString(), findkey, obj);
        var prop = props.Last();
        prop.SetValue(obj, newvalue);
      }
    }

    internal static string FindKey(LandropeContext context, ExtLandropeContext contextExt,
                            string value, string keyword, object obj)
    {
      switch (keyword)
      {
        case "keyProject": return FindProjectKey(context, value);
        case "keyDesa": return FindDesaKey(context, value, obj);
        case "keyNotaris": return FindNotarisKey(contextExt, value);
        case "keyPenampung": return FindPenampungKey(contextExt, value);
        case "keyPTSK": return FindPTSKKey(contextExt, value);
        case "keyCompany": return FindCompanyKey(contextExt, value);
      }
      return null;
    }

    internal static string FindNotarisKey(ExtLandropeContext contextExt, string value)
    {
      var notarists = contextExt.notarists.Query(c => c.invalid != true)
                      .Select(c => new { c.key, identifier = c.identifier.ToLower().Replace(" ", "") });
      var doc = notarists.FirstOrDefault(c => c.identifier == value.ToLower().Replace(" ", ""));
      if (doc == null)
      {
        var p = new Notaris { key = MongoEntity.MakeKey, identifier = value };
        contextExt.notarists.Insert(p);
        contextExt.SaveChanges();
        return p.key;
      }
      return doc.key;
    }

    internal static string FindCompanyKey(ExtLandropeContext contextExt, string value)
    {
      var companies = contextExt.companies.Query(c => c.invalid != true)
                      .Select(c => new { c.key, identifier = c.identifier.ToLower().Replace(" ", "") });
      var doc = companies.FirstOrDefault(c => c.identifier == value.ToLower().Replace(" ", ""));
      if (doc == null)
      {
        var p = new Company { key = MongoEntity.MakeKey, identifier = value, status = StatusPT.penampung };
        contextExt.companies.Insert(p);
        contextExt.SaveChanges();
        return p.key;
      }
      return doc.key;
    }

    internal static string FindPTSKKey(ExtLandropeContext contextExt, string value)
    {
      var companies = contextExt.companies.Query(c => c.invalid != true && c.status == StatusPT.pembeli)
                      .Select(c => new { c.key, identifier = c.identifier.ToLower().Replace(" ", "") });
      var doc = companies.FirstOrDefault(c => c.identifier == value.ToLower().Replace(" ", ""));
      if (doc == null)
      {
        var p = new Company { key = MongoEntity.MakeKey, identifier = value, status = StatusPT.pembeli };
        contextExt.companies.Insert(p);
        contextExt.SaveChanges();
        return p.key;
      }
      return doc.key;
    }

    internal static string FindPenampungKey(ExtLandropeContext contextExt, string value)
    {
      var companies = contextExt.companies.Query(c => c.invalid != true && c.status == StatusPT.penampung)
                      .Select(c => new { c.key, identifier = c.identifier.ToLower().Replace(" ", "") });
      var doc = companies.FirstOrDefault(c => c.identifier == value.ToLower().Replace(" ", ""));
      if (doc == null)
      {
        var p = new Company { key = MongoEntity.MakeKey, identifier = value, status = StatusPT.penampung };
        contextExt.companies.Insert(p);
        contextExt.SaveChanges();
        return p.key;
      }
      return doc.key;
    }

    internal static string FindProjectKey(LandropeContext context, string value)
    {
      Project p = context.GetCollections(new Project(), "maps", $"{{identity:/{rgxmode(value)}/i}}").ToList().FirstOrDefault();
      if (p == null)
      {
        p = new Project { key = MongoEntity.MakeKey, identity = value };
        context.db.GetCollection<Project>("maps").InsertOne(p);
      }
      return p.key;
    }

    internal static string FindDesaKey(LandropeContext context, string value, object obj)
    {
      var prop = obj.GetType().GetProperty("keyProject");
      if (prop == null)
        throw new InvalidDataException("Desa key can not be determined without project key");
      var keyproj = prop.GetValue(obj);
      if (keyproj == null)
        throw new InvalidDataException("Desa key can not be determined whle the project key is not defined");
      Project p = context.GetCollections(new Project(), "maps", $"{{key:'{keyproj}'}}").ToList().FirstOrDefault();
      if (p == null)
        throw new InvalidDataException("Desa key can not be determined while project key is not valid");

      Village vil = p.villages.FirstOrDefault(v => v.identity.ToLower().Replace(" ", "") == value.ToLower().Replace(" ", ""));
      if (vil == null)
      {
        vil = new Village { key = MongoEntity.MakeKey, identity = value };
        p.villages.Add(vil);
        context.db.GetCollection<Project>("maps").ReplaceOne($"{{key:'{p.key}'}}", p);
      }
      return vil.key;
    }

    static string rgxmode(string value) => new Regex(@"\s+").Replace(value, @"\s?").Replace("(", @"\(").Replace(")", @"\)");

    public static string LoadNoPeta(ExtLandropeContext contextEx, string filepath, string sheetname)
    {
      var strm = new FileStream(filepath, FileMode.Open, FileAccess.Read);
      var xreader = ExcelReaderFactory.CreateReader(strm);
      try
      {
        var res = xreader.AsDataSet();
        var dt = res.Tables.Cast<DataTable>().FirstOrDefault(d => d.TableName == sheetname);
        //var sesx = contextEx.db.Client.StartSession();
        //sesx.StartTransaction();
        try
        {
          var rcols = dt.Rows.Cast<DataRow>().FirstOrDefault(); // get first row
          if (rcols == null)
            throw new Exception("This worksheet contains no rows");
          var cols = rcols.ItemArray.Select(c => (c?.Equals(DBNull.Value) ?? true) ? null : c).ToArray(); //col names from first row
                                                                                                          //var indices = Enumerable.Range(1, cols.Length - 1).ToList(); // col numbers for enumeration
          var xcols = cols.Select((c, i) => (c, i)).Where(x => x.c != null).ToList();
          var discriminators = xcols.Where(x => x.c.Equals("key") || x.c.Equals("noPeta")).ToDictionary(x=>x.c, x=>x.i);
          if (discriminators.Count != 2)
            return "Invalid first row values of the workshet";

          var arrrows = dt.Rows.Cast<DataRow>().Select(x => x.ItemArray).ToArray();
          var rows = arrrows.Where(x => x[0] != null && !DBNull.Value.Equals(x[0]) && x[0].Equals(1d))
                                  .Select((cells, i) => (cells, i)).ToList();

          foreach (var x in rows)
          {
            Console.Write($"updating row #{x.i}...");

            var key = x.cells[discriminators["key"]] as string;
            var nopeta = x.cells[discriminators["noPeta"]];
            var persil = contextEx.persils.FirstOrDefault(p => p.key == key);
            if (persil == null)
              continue;
            persil.basic.current.noPeta = nopeta as string;
            persil.basic.entries.ForEach(e => {
              if (e.item != null)
                e.item.noPeta = nopeta as string;
            });
            contextEx.persils.Update(persil);
            //switch (persil)
            //{
            //  case PersilGirik pg: contextEx.persilGiriks.Update(pg); break;
            //  case PersilHibah pg: contextEx.persilHibahs.Update(pg); break;
            //  case PersilSHM pg: contextEx.persilSHMs.Update(pg); break;
            //  case PersilSHP pg: contextEx.persilSHPs.Update(pg); break;
            //  case PersilHGB pg: contextEx.persilHGBs.Update(pg); break;
            //}
          }
          //);
          contextEx.SaveChanges();
          //sesx.CommitTransaction();
        }
        catch (Exception)
        {
          //sesx.AbortTransaction();
          throw;
        }
      }
      catch (Exception ex)
      {
        var st = ex.Message;
        for (Exception exx = ex.InnerException; exx != null; exx = exx.InnerException)
          st += $"->{exx.Message}";
        return st;
      }
      finally
      {
        xreader.Close();
        strm.Close();
      }
      return null;
    }
  }
}
