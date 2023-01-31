using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Text;

namespace landrope.common
{
	public class CategoryCore
	{
		public string key { get; set; }
		public StatusCategory bebas { get; set; }
		public int segment { get; set; }
		public string desc { get; set; }
		public string shortDesc { get; set; }
	}


	public class CategoryView
	{
		public string key { get; set; }
		public StatusCategory bebas { get; set; }
		public int segment { get; set; }
		public ushort? value { get; set; }
		public string desc { get; set; }
		public string shortDesc { get; set; }
	}

	public class CategoriesCore
    {
		public DateTime? tanggal { get; set; }
		public string[] keyCategory { get; set; }
		public string[] keyPersil { get; set; }
	}

	public class CategoriesView
	{
		public string keyPersil { get; set; }
		public string Project { get; set; }
		public string Desa { get; set; }
		public string[] keyCategories { get; set; }
		public string Category { get; set; }
		public string shortCategory { get; set; }
		public string NamaPemilik { get; set; }
		public string AlasHak { get; set; }
		public double? luasSurat { get; set; }
	}

	public class CategoriesFollowUpView
    {
        public string keyFollowUp { get; set; }
        public string nomor { get; set; }
		public string manager { get; set; }
        public string sales { get; set; }
        public string[]	keyCategories { get; set; }
        public string categories { get; set; }
        public double? luas { get; set; }
    }

	public class CategoriesFollowUpCore
	{
        public string keyFollowUp { get; set; }
        public string[] keyCategory { get; set; }
	}

	public class CategoryL
	{
		public string keyCategory { get; set;}
		public string desc { get; set;}
	}
}
