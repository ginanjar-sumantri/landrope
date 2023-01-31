using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using landrope.common;
//using landrope.mcommon;

namespace landrope.mod2
{
	public class StepAttribute : Attribute
	{
		public int order { get; set; }
		public ProcessStep en_step { get; set; }
		public string name => en_step.ToString("g");
		public string descr => StepHelper.ProcessDescs[en_step];
		public string privs { get; set; }
  }

	public class MapFilterAttribute : Attribute
	{
		public Type typeClass { get; set; } = null;
		public string nameProp { get; set; } = null;
		public virtual bool Filter(Type typeClas, string nameProp)
			=> (this.typeClass == null || this.typeClass == typeClas) && (this.nameProp==null||this.nameProp==nameProp);
	}

	[AttributeUsage(AttributeTargets.Property,AllowMultiple =true)]
	public class BundleMapAttribute : MapFilterAttribute
	{
		public BundleMapAttribute(string keyDocType)
		{
			this.keyDocType = keyDocType;
		}
		public string keyDocType { get; set; }

	}

	[AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
	public class BundlePropMapAttribute : BundleMapAttribute
	{
		public BundlePropMapAttribute(string keyDocType,MetadataKey keyMeta, Dynamic.ValueType type)
			:base(keyDocType)
		{
			this.keyMeta = keyMeta;
			this.type = type;
		}
		public MetadataKey keyMeta { get; set; }
		public Dynamic.ValueType type { get; set; }
	}
}
