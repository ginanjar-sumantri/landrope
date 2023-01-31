using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using ExcelDataReader;
//using Google.Api;
using landrope.common;
using MongoDB.Driver;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders.BinaryEncoders;

namespace landrope.mod
{
	public static class AreaLoader
	{
		internal enum ColumnName
		{
			PROKEY = 0,
			PROJECT = 1,
			VILKEY = 2,
			DESA = 3,
			BEBAS = 4,
			B_NIB1 = 5,
			B_NIB2 = 6,
			B_NIB3 = 7,
			B_NIB4 = 8,
			B_NONIB = 9,
			DEAL = 10,
			BB_SHM = 11,
			BB_NONIB = 12,
			KAMPUNG = 13
		}

		internal static Dictionary<string, (int bebas, int hibah, int PBT)> coltypes = new Dictionary<string, (int bebas, int hibah, int PBT)>
		{
			{"PROKEY",(0,0,0)},
			{"VILKEY",(0,0,0)},
			{"BEBAS",(4,1,0)},
			{"B_H",(4,2,2)},
			{"B_H_NN",(4,2,1)},
			{"T_H",(3,2,1)},
			{"TRANS",(3,1,0)},
			{"BB_S",(2,1,0)},
			{"BB_H",(2,2,1)},
			{"KAMPUNG",(1,0,0)}
		};

		public static string Load(LandropeContext context, string filepath, string sheetname)
		{
			//var conf = new ExcelReaderConfiguration();
			var strm = new FileStream(filepath, FileMode.Open, FileAccess.Read);
			var xreader = ExcelReaderFactory.CreateReader(strm);
			try
			{
				//var conf = new ExcelDataSetConfiguration { FilterSheet = }
				var res = xreader.AsDataSet();
				var dt = res.Tables.Cast<DataTable>().FirstOrDefault(d => d.TableName == sheetname);
				var ses = context.db.Client.StartSession();
				ses.StartTransaction();
				try
				{
					Project currproj = null;
					dt.Rows.Cast<DataRow>().ToList().ForEach(d =>
					{
						var data = d.ItemArray.Select(dd => (dd?.Equals(DBNull.Value) ?? true) ? null : dd).ToArray();
						var prokey = data[(int)ColumnName.PROKEY] as string;
						if (currproj != null && currproj.key != prokey)
						{
							context.db.GetCollection<Project>("maps").ReplaceOne($"{{key:'{currproj.key}'}}", currproj);
							currproj = null;
						}
						if (currproj == null)
							currproj = context.GetCollections(new Project(), "maps", $"{{'key':'{prokey}'}}").FirstOrDefault();
						if (currproj != null)
						{
							var vilkey = data[(int)ColumnName.VILKEY] as string;
							var village = currproj.villages.FirstOrDefault(v => v.key == vilkey);
							if (village != null)
							{
								if (village.StatusAreas == null)
									village.StatusAreas = new double[Enum.GetValues(typeof(LandStatus)).Length].ToList();
								SetArea(village, LandStatus.Sudah_Bebas__murni, data, ColumnName.BEBAS, false);
								SetArea(village, LandStatus.Belum_Bebas__Sertifikat, data, ColumnName.BB_SHM, false);
								SetArea(village, LandStatus.Belum_Bebas__Sertifikat, data, ColumnName.DEAL, true);
								SetArea(village, LandStatus.Hibah__PBT_sudah_terbit, data, ColumnName.B_NIB1, false);
								SetArea(village, LandStatus.Hibah__PBT_sudah_terbit, data, ColumnName.B_NIB2, true);
								SetArea(village, LandStatus.Hibah__PBT_sudah_terbit, data, ColumnName.B_NIB3, true);
								SetArea(village, LandStatus.Hibah__PBT_sudah_terbit, data, ColumnName.B_NIB4, true);
								SetArea(village, LandStatus.Hibah__PBT_belum_terbit, data, ColumnName.BB_NONIB, false);
								//SetArea(village, LandStatus.Sudah_bebas__PBT_belum_terbit, data, ColumnName.B_NONIB, false);
								SetArea(village, LandStatus.Kampung, data, ColumnName.KAMPUNG, false);
							}
						}
					});
					if (currproj != null)
						context.db.GetCollection<Project>("maps").ReplaceOne($"{{key:'{currproj.key}'}}", currproj);
					var projs = context.GetCollections(new Project(), "maps", "{}").ToList();
					projs.SelectMany(p => p.villages).ToList().ForEach(v => v.DistributeAreas(context));
					ses.CommitTransaction();
				}
				catch (Exception ex)
				{
					ses.AbortTransaction();
					return ex.Message;
				}
			}
			finally
			{
				xreader.Close();
				strm.Close();
			}
			return null;

			void SetArea(Village village, LandStatus status, object[] array, ColumnName col, bool accum = false)
			{
				var obj = array[(int)col];
				var value = (((double?)obj) ?? 0) * 1e4;
				if (accum)
					village.StatusAreas[(int)status] += value;
				else
					village.StatusAreas[(int)status] = value;
			}
		}

		public static string Load2(LandropeContext context, string filepath, string sheetname)
		{
			//var conf = new ExcelReaderConfiguration();
			var strm = new FileStream(filepath, FileMode.Open, FileAccess.Read);
			var xreader = ExcelReaderFactory.CreateReader(strm);
			try
			{
				//var conf = new ExcelDataSetConfiguration { FilterSheet = }
				var res = xreader.AsDataSet();
				var dt = res.Tables.Cast<DataTable>().FirstOrDefault(d => d.TableName == sheetname);
				var ses = context.db.Client.StartSession();
				ses.StartTransaction();
				try
				{
					Project currproj = null;
					var rcols = dt.Rows.Cast<DataRow>().FirstOrDefault(); // get first row
					if (rcols == null)
						throw new Exception("This worksheet contains no rows");
					var cols = rcols.ItemArray.Select(c => (c?.Equals(DBNull.Value) ?? true) ? null : c).ToArray(); //col names from first row
					var indices = Enumerable.Range(1, cols.Length - 1).ToList(); // col numbers for enumeration

					var statlen = Enum.GetNames(typeof(LandStatus)).Length;

					// selecting rows with first cell is not empty, then walk through about it
					dt.Rows.Cast<DataRow>().ToList().Where(d => !(d[0]?.Equals(DBNull.Value) ?? true)).ToList()
						.ForEach(d =>
						{
							var prokey = "";
							var vilkey = "";
							var lvalues = new List<((int bebas, int hibah, int PBT) key, double value)>();

							var data = d.ItemArray.Select(dd => (dd?.Equals(DBNull.Value) ?? true) ? null : dd).ToArray();
							indices.ForEach(idx =>
							{
								var col = cols[idx];
								if (col != null)
								{
									if (col.Equals("PROKEY"))
										prokey = data[idx] as string;
									else if (col.Equals("VILKEY"))
										vilkey = data[idx] as string;
									else
									{
										(int bebas, int hibah, int PBT) key;
										if (coltypes.TryGetValue(col.ToString(), out key))
										{
											var value = (double?)data[idx];
											lvalues.Add((key, value ?? 0));
										}
									}
								}
							});
/*
			{"BEBAS",(4,1,0)},
			{"B_H",(4,2,2)},
			{"B_H_NN",(4,2,1)},
			{"T_H",(3,2,1)},
			{"TRANS",(3,1,0)},
			{"BB_S",(2,1,0)},
			{"BB_H",(2,2,1)},
			{"KAMPUNG",(1,0,0)}
*/
							var ValueByStatus = new double[statlen];
							lvalues.ForEach(v =>
							{
								LandStatus ls = LandStatus.Tanpa_status;
								switch (v.key.bebas)
								{
									case 1: ls = LandStatus.Kampung; break;
									case 2:
										if (v.key.hibah == 1)
											ls = LandStatus.Belum_Bebas__Sertifikat;
										else
											ls = LandStatus.Hibah__PBT_belum_terbit;
										break;
									case 3:
										if (v.key.hibah == 1)
											ls = LandStatus.Transisi_murni;
										else
											ls = LandStatus.Transisi_hibah;
										break;
									case 4:
										if (v.key.hibah == 1)
											ls = LandStatus.Sudah_Bebas__murni;
										else
										{
											if (v.key.PBT == 1)
												ls = LandStatus.Hibah__PBT_belum_terbit;
											else
												ls = LandStatus.Hibah__PBT_sudah_terbit;
										}
										break;
								}
								ValueByStatus[(int)ls] += v.value * 10000;
							});
							if (currproj != null && currproj.key != prokey)
							{
								context.db.GetCollection<Project>("maps").ReplaceOne($"{{key:'{currproj.key}'}}", currproj);
								currproj = null;
							}
							if (currproj == null)
								currproj = context.GetCollections(new Project(), "maps", $"{{'key':'{prokey}'}}").FirstOrDefault();
							if (currproj != null)
							{
								var village = currproj.villages.FirstOrDefault(v => v.key == vilkey);
								if (village != null)
									village.StatusAreas = ValueByStatus.ToList();
							}
						});

					// stores the last project in loop					
					if (currproj != null)
						context.db.GetCollection<Project>("maps").ReplaceOne($"{{key:'{currproj.key}'}}", currproj);

					// reload all projects for distributing the villages' area
					var projs = context.GetCollections(new Project(), "maps", "{}").ToList();
					projs.SelectMany(p => p.villages).ToList().ForEach(v => v.DistributeAreas(context));
					ses.CommitTransaction();
				}
				catch (Exception ex)
				{
					ses.AbortTransaction();
					return ex.Message;
				}
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
