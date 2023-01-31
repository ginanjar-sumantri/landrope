using flow.common;
using GenWorkflow;
using landrope.common;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using mongospace;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Extensions.DependencyInjection;

namespace landrope.mod3
{
	public class SLArange
	{
		public uint? from { get; set; }
		public uint? upto { get; set; }

		[BsonIgnore]
		public (uint v, char u) duration { get; set; }

		public Tuple<uint,char> _duration
		{
			get => new Tuple<uint, char>(duration.v,duration.u);
			set => duration = (value.Item1, value.Item2);
		}

		static char[] unitchars = {'d','D','w','W','m','M','y','Y'};

		public SLArange(uint? from, uint? upto, (uint v,char u) duration)
		{
			this.from = from;
			this.upto = upto;
			this.duration = duration;

			if (from == null && upto == null)
				throw new InvalidOperationException("SLA tidak valid: batas atas atau bawah harus ditetapkan");
			if (!unitchars.Contains(duration.u))
				throw new InvalidOperationException($"Satuan hari tidak valid, yang diperbolehkan: {unitchars}");
		}

		public static implicit operator SLArange((uint? from, uint? upto, (uint v, char u) duration) entry)
			=> new SLArange(entry.from,entry.upto,entry.duration);

		public uint Duration =>
			duration.u switch
			{
				'w' or 'W' => duration.v * 7,
				'm' or 'M' => duration.v * 30,
				'y' or 'Y' => duration.v * 365,
				_ => duration.v
			};

		internal bool IsIn(int count)
			=> count >= (from ?? uint.MinValue) && count <= (upto ?? uint.MaxValue);
	}

	[BsonDiscriminator("sla")]
	public class SLA
	{
		[JsonIgnore]
		public DocProcessStep _step { get; set; }

		public string step
		{
			get => _step.ToString("g");
			set { _step = Enum.TryParse<DocProcessStep>(value, out DocProcessStep stp) ? stp : (DocProcessStep)(-1); }
		}

		public string keyProject { get; set; } = null;
		public string keyDesa { get; set; } = null;

		public DateTime since { get; set; }
		public DateTime? until { get; set; }

		public SLArange[] ranges { get; set; } = new SLArange[0];


		public SLA()
		{
		}

		public SLA(string step, DateTime since, DateTime? until, SLArange[] ranges)
		{
			(this.step, this.since,this.until, this.ranges) = (step, since,until,ranges);
		}
		public SLA(string step, DateTime since, SLArange[] ranges)
		{
			(this.step, this.since, this.until, this.ranges) = (step, since, null, ranges);
		}
		public SLA(string step, DateTime since)
		{
			(this.step, this.since, this.until) = (step, since, null);
		}

		public SLA(DocProcessStep step, DateTime since, DateTime? until, SLArange[] ranges)
		{
			(this._step, this.since, this.until, this.ranges) = (step, since, until, ranges);
		}
		public SLA(DocProcessStep step, DateTime since, SLArange[] ranges)
		{
			(this._step, this.since, this.until, this.ranges) = (step, since, null, ranges);
		}
		public SLA(DocProcessStep step, DateTime since)
		{
			(this._step, this.since, this.until) = (step, since, null);
		}

		public static implicit operator SLA((string step, DateTime since, DateTime? until, SLArange[] ranges) entry)
			=> new SLA(entry.step, entry.since, entry.until, entry.ranges);
		public static implicit operator SLA((string step, DateTime since, SLArange[] ranges) entry)
			=> new SLA(entry.step, entry.since, entry.ranges);
		public static implicit operator SLA((string type, string step, string version, DateTime since) entry)
			=> new SLA(entry.step, entry.since);

		public static implicit operator SLA((DocProcessStep step, DateTime since, DateTime? until, SLArange[] ranges) entry)
			=> new SLA(entry.step, entry.since, entry.until, entry.ranges);
		public static implicit operator SLA((ToDoType type, DocProcessStep step, string version, DateTime since, SLArange[] ranges) entry)
			=> new SLA(entry.step, entry.since, entry.ranges);
		public static implicit operator SLA((ToDoType type, DocProcessStep step, string version, DateTime since) entry)
			=> new SLA(entry.step, entry.since);

		static List<SLA> _list = null;
		public static List<SLA> list
		{
			get
			{
				if (_list == null)
				{
					var context = ContextService.services.GetService<LandropePlusContext>();
					_list = context.GetCollections(new SLA(), "timings", "{}", "{_id:0}").ToList();
				}
				return _list;
			}
		}
	}
}
