using MongoDB.Driver.GeoJsonObjectModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace landrope.mod
{
	public class SphPoint : GeoJsonCoordinates
	{
		public double lon { get; set; }
		public double lat { get; set; }
		public override ReadOnlyCollection<double> Values => new ReadOnlyCollection<double>(new[]{ lon,lat}.ToList());
	}
}
