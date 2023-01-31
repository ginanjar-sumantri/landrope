using landrope.common;
using landrope.mod2;
using Microsoft.AspNetCore.Http;
using MongoDB.Driver.Core.Operations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json.Serialization;

namespace landrope.mod3
{
	public class DocExist
	{
		//public bool included { get; set; }
		public RegisteredDoc Doc { get; set; }

		public DocExistCore ToCore()
		{
			var core = new DocExistCore();
			core.Doc, core.existas, core.included) = (Doc.ToCore(), existas, included);
			return core;
		}

		public DocExist FromCore(DocExistCore core)
		{
			(Doc, existas, included) = ((RegisteredDoc)Doc, core.existas, core.included);
			return this;
		}

		public static explicit operator DocExist(DocExistCore core)
		{
			var doc = new DocExist();
			(doc.Doc, doc.existas, doc.included) = ((RegisteredDoc)core.Doc, core.existas, core.included);
			return doc;
		}

		public DocExistView ToView()
		{
			var view = new DocExistView();
			(view.keyDocType, view.included, view.docType, view.props, view.Asli,view.Copy,view.Legalisir,view.Salinan,view.Soft_Copy) = 
				(Doc.keyDocType,included, Doc.docType.identifier, Doc.props,
				existas.First(x => x.ex == Existence.Asli).cnt,
				existas.First(x => x.ex == Existence.Copy).cnt,
				existas.First(x => x.ex == Existence.Legalisir).cnt,
				existas.First(x => x.ex == Existence.Salinan).cnt,
				existas.First(x => x.ex == Existence.Soft_Copy).cnt>1 );
			return view;
		}

	}
}
