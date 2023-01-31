using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using CadLoader;
using GeomHelper;
using landrope.mod;
using MongoDB.Bson;
using MongoDB.Driver;
using mongospace;

namespace mapup
{
	public partial class Form1 : Form
	{
		public Form1()
		{
			InitializeComponent();
		}

		LandropeContext context;
		processor proc;

		private void button1_Click(object sender, EventArgs e)
		{
			var res = openDlg.ShowDialog();
			if (res != DialogResult.OK)
				return;
			var fname = openDlg.FileName;
			textBox1.Text = fname;
		}

		private void button2_Click(object sender, EventArgs e)
		{
			Close();
		}

		public static LandropeContext CreateContext()
		{
			var url = ConfigurationManager.ConnectionStrings["mongodb"].ConnectionString.Replace("+", "&");
			var database = ConfigurationManager.AppSettings["database"];
			return new LandropeContext(url, database);
		}

		private void Form1_Load(object sender, EventArgs e)
		{
			context = CreateContext();

			CollectProjects();
			proc = new processor(context);
		}

		private void CollectProjects()
		{
			//var projects = context.Projects("{}").ToList()
			var projects = context.GetCollections(new Project(), "project_only", "{}").ToList()
										.Select(p => new ComboItem{ value = p.key, caption = p.identity }).ToArray();

			comboBox1.Items.Clear();
			comboBox1.Items.AddRange(projects);
		}

		private void CollectVillages()
		{
			var pk = ((ComboItem)comboBox1.SelectedItem)?.value;
			if (pk == null)
				return;

			var villages = context.GetCollections(new { village = new Village() }, "villages", $"{{'project.key':'{pk}'}}","{project:0}").ToList()
										.Select(p => new ComboItem{ value = p.village.key, caption = p.village.identity }).ToArray();

			comboBox2.Items.Clear();
			comboBox2.Items.AddRange(villages);
		}

		private void button4_Click(object sender, EventArgs e)
		{
			CollectProjects();
		}

		private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
		{
			CollectVillages();
		}

		private void button5_Click(object sender, EventArgs e)
		{
			CollectVillages();
		}

		private void button3_Click(object sender, EventArgs e)
		{
			var vk = ((ComboItem)comboBox2.SelectedItem)?.value;
			if (vk == null)
				return;
			if (string.IsNullOrWhiteSpace(textBox1.Text))
				return;
			var fname = textBox1.Text.Trim();

			var res = proc.Process(fname, vk);
			if (res == null)
				res = "DONE";
			MessageBox.Show(res);
		}

	}

	internal class ComboItem
	{
		public string value;
		public string caption;

		public override string ToString() => caption;
	}
}
