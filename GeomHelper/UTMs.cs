using geo.shared;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;

namespace GeomHelper
{
	public class UtmPoint
	{
		public double x=0;
		public double y=0;
	}
	public class Utm : UtmPoint
	{
		public int zone;
		public char? band;
		public bool? southHemi;

		public Utm(double x, double y, int zone, char? band = null, bool? southHemi = null)
		{
			this.x = x;
			this.y = y;
			this.zone = zone;
			this.band = band;
			this.southHemi = southHemi;
		}
	}

	public static class UtmConv
	{
		static double pi = Math.PI;
		static double pirad = Math.PI/180d;

		/* Ellipsoid model constants (actual values here are for WGS84) */
		static double sm_a = 6378137.0;
		static double sm_b = 6356752.314;
		static double sm_EccSquared = 6.69437999013e-03;

		static double UTMScaleFactor = 0.9996;
		static double n, np2, np3, np4, np5, np6, np7, np8;
		static double alpha, beta, gamma, delta, epsilon;
		static double alpha_, beta_, gamma_, delta_, epsilon_;
		static double ep2;

		static UtmConv()
		{
			n = (sm_a - sm_b) / (sm_a + sm_b);
			np2 = n * n;
			np3 = np2 * n;
			np4 = np2 * np2;
			np5 = np3 * np2;

			/* Precalculate alpha */
			alpha = ((sm_a + sm_b) / 2) * (1 + (np2 / 4) + (np4 / 64));
			alpha_ = ((sm_a + sm_b) / 2) * (1 + (np2 / 4) + (np4 / 64));

			/* Precalculate beta */
			beta = (-3 * n / 2) + (9 * np3 / 16) + (-3 * np5 / 32);
			beta_ = (3 * n / 2) + (-27 * np3 / 32) + (269 * np5 / 512);

			/* Precalculate gamma */
			gamma = (15 * np2 / 16) + (-15 * np4 / 32);
			gamma_ = (21 * np2 / 16) + (-55 * np4 / 32);

			/* Precalculate delta */
			delta = (-35 * np3 / 48) + (105 * np5 / 256);
			delta_ = (151 * np3 / 96) + (-417 * np5 / 128);

			/* Precalculate epsilon */
			epsilon = (315 * np4 / 512);
			epsilon_ = (1097 * np4 / 512);

			ep2 = (sm_a * sm_a - sm_b * sm_b) / (sm_b * sm_b);
		}

		/*
    * DegToRad
    *
    * Converts degrees to radians.
    *
    */
		static double DegToRad(double deg) => deg * pirad;



		/*
    * RadToDeg
    *
    * Converts radians to degrees.
    *
    */
		static double RadToDeg(double rad) => rad / pirad;



		/*
    * ArcLengthOfMeridian
    *
    * Computes the ellipsoidal distance from the equator to a point at a
    * given latitude.
    *
    * Reference: Hoffmann-Wellenhof, B., Lichtenegger, H., and Collins, J.,
    * GPS: Theory and Practice, 3rd ed.  New York: Springer-Verlag Wien, 1994.
    *
    * Inputs:
    *     phi - Latitude of the point, in radians.
    *
    * Globals:
    *     sm_a - Ellipsoid model major axis.
    *     sm_b - Ellipsoid model minor axis.
    *
    * Returns:
    *     The ellipsoidal distance of the point from the equator, in meters.
    *
    */
		static double ArcLengthOfMeridian(double phi) =>
			alpha * (phi + (beta * Math.Sin(2.0 * phi)) + (gamma * Math.Sin(4.0 * phi)) +
												(delta * Math.Sin(6.0 * phi)) + (epsilon * Math.Sin(8.0 * phi)));

		/*
    * UTMCentralMeridian
    *
    * Determines the central meridian for the given UTM zone.
    *
    * Inputs:
    *     zone - An integer value designating the UTM zone, range [1,60].
    *
    * Returns:
    *   The central meridian for the given UTM zone, in radians, or zero
    *   if the UTM zone parameter is outside the range [1,60].
    *   Range of the central meridian is the radian equivalent of [-177,+177].
    *
    */
		public static double UTMCentralMeridian(int zone) => DegToRad(-183.0 + (zone * 6d));

		/*
    * FootpointLatitude
    *
    * Computes the footpoint latitude for use in converting transverse
    * Mercator coordinates to ellipsoidal coordinates.
    *
    * Reference: Hoffmann-Wellenhof, B., Lichtenegger, H., and Collins, J.,
    *   GPS: Theory and Practice, 3rd ed.  New York: Springer-Verlag Wien, 1994.
    *
    * Inputs:
    *   y - The UTM northing coordinate, in meters.
    *
    * Returns:
    *   The footpoint latitude, in radians.
    *
    */
		public static double FootpointLatitude(double y)
		{
			var y_ = y / alpha_;
			return y_ + (beta_ * Math.Sin(2.0 * y_)) + (gamma_ * Math.Sin(4.0 * y_)) +
							 (delta_ * Math.Sin(6.0 * y_)) + (epsilon_ * Math.Sin(8.0 * y_));
		}



		/*
    * MapLatLonToXY
    *
    * Converts a latitude/longitude pair to x and y coordinates in the
    * Transverse Mercator projection.  Note that Transverse Mercator is not
    * the same as UTM; a scale factor is required to convert between them.
    *
    * Reference: Hoffmann-Wellenhof, B., Lichtenegger, H., and Collins, J.,
    * GPS: Theory and Practice, 3rd ed.  New York: Springer-Verlag Wien, 1994.
    *
    * Inputs:
    *    phi - Latitude of the point, in radians.
    *    lambda - Longitude of the point, in radians.
    *    lambda0 - Longitude of the central meridian to be used, in radians.
    *
    * Outputs:
    *    xy - A 2-element array containing the x and y coordinates
    *         of the computed point.
    *
    * Returns:
    *    The function does not return a value.
    *
    */
		public static (double x, double y) MapLatLonToXY(double lat, double lon, double lon0)
		{
			//var N, nu2, ep2, t, t2, l;
			//var l3coef, l4coef, l5coef, l6coef, l7coef, l8coef;
			//var tmp;

			/* Precalculate ep2 */
			//ep2 = (sm_a*sm_a - sm_b*sm_b) / sm_b*sm_b;

			/* Precalculate nu2 */
			var coslat = Math.Cos(lat);
			var nu2 = ep2 * coslat*coslat;

			/* Precalculate N */
			var N = sm_a*sm_a / (sm_b * Math.Sqrt(1 + nu2));

			/* Precalculate t */
			var t = Math.Tan(lat);
			var t2 = t * t;
			//var tmp = (t2 * t2 * t2) - Math.Pow(t, 6.0);

			/* Precalculate l */
			var l = lon - lon0;

			/* Precalculate coefficients for l**n in the equations below
				 so a normal human being can read the expressions for easting
				 and northing
				 -- l**1 and l**2 have coefficients of 1.0 */
			var l3coef = 1.0 - t2 + nu2;

			var l4coef = 5.0 - t2 + 9 * nu2 + 4.0 * (nu2 * nu2);

			var l5coef = 5.0 - 18.0 * t2 + (t2 * t2) + 14.0 * nu2 - 58.0 * t2 * nu2;

			var l6coef = 61.0 - 58.0 * t2 + (t2 * t2) + 270.0 * nu2 - 330.0 * t2 * nu2;

			var l7coef = 61.0 - 479.0 * t2 + 179.0 * (t2 * t2) - (t2 * t2 * t2);


			var l8coef = 1385.0 - 3111.0 * t2 + 543.0 * (t2 * t2) - (t2 * t2 * t2);

			var coslat2 = coslat * coslat;
			var coslat3 = coslat2 * coslat;
			var coslat4 = coslat2 * coslat2;
			var coslat5 = coslat3 * coslat2;
			var coslat6 = coslat3 * coslat3;
			var coslat7 = coslat5 * coslat2;
			var coslat8 = coslat4 * coslat4;

			var l2 = l * l;
			var l3 = l2 * l;
			var l4 = l2 * l2;
			var l5 = l3 * l2;
			var l6 = l3 * l3;
			var l7 = l5 * l2;
			var l8 = l4 * l4;

			/* Calculate easting (x) */
			var x = N * coslat * l +
							(N / 6.0 * coslat3 * l3coef * l3) +
							(N / 120.0 * coslat5 * l5coef * l5) +
							(N / 5040.0 * coslat7 * l7coef * l7);

			/* Calculate northing (y) */
			var y = ArcLengthOfMeridian(lat) +
							(t / 2.0 * N * coslat2 * l2) +
							(t / 24.0 * N * coslat4 * l4coef * l4) +
							(t / 720.0 * N * coslat6 * l6coef * l6) +
							(t / 40320.0 * N * coslat8 * l8coef * l8);

			return (x, y);
		}

		/*
		* MapXYToLatLon
		*
		* Converts x and y coordinates in the Transverse Mercator projection to
		* a latitude/longitude pair.  Note that Transverse Mercator is not
		* the same as UTM; a scale factor is required to convert between them.
		*
		* Reference: Hoffmann-Wellenhof, B., Lichtenegger, H., and Collins, J.,
		*   GPS: Theory and Practice, 3rd ed.  New York: Springer-Verlag Wien, 1994.
		*
		* Inputs:
		*   x - The easting of the point, in meters.
		*   y - The northing of the point, in meters.
		*   lambda0 - Longitude of the central meridian to be used, in radians.
		*
		* Outputs:
		*   philambda - A 2-element containing the latitude and longitude
		*               in radians.
		*
		* Returns:
		*   The function does not return a value.
		*
		* Remarks:
		*   The local variables Nf, nuf2, tf, and tf2 serve the same purpose as
		*   N, nu2, t, and t2 in MapLatLonToXY, but they are computed with respect
		*   to the footpoint latitude phif.
		*
		*   x1frac, x2frac, x2poly, x3poly, etc. are to enhance readability and
		*   to optimize computations.
		*
		*/
		public static (double lat, double lon) MapXYToLatLon(double x, double y, double lon0)
		{
			/* Get the value of phif, the footpoint latitude. */
			var phif = FootpointLatitude(y);

			/* Precalculate ep2 */
			//var ep2 = (Math.Pow(sm_a, 2.0) - Math.Pow(sm_b, 2.0)) / Math.Pow(sm_b, 2.0);
			//var ep2 = (sm_a*sm_a - sm_b*sm_b) / (sm_b*sm_b);

			/* Precalculate cos (phif) */
			var cf = Math.Cos(phif);

			/* Precalculate nuf2 */
			//var nuf2 = ep2 * Math.Pow(cf, 2.0);
			var nuf2 = ep2 * cf*cf;

			/* Precalculate Nf and initialize Nfpow */
			//var Nf = Math.Pow(sm_a, 2.0) / (sm_b * Math.Sqrt(1 + nuf2));
			var Nf = (sm_a*sm_a) / (sm_b * Math.Sqrt(1 + nuf2));
			var Nfpow = Nf;

			/* Precalculate tf */
			var tf = Math.Tan(phif);
			var tf2 = tf * tf;
			var tf4 = tf2 * tf2;

			/* Precalculate fractional coefficients for x**n in the equations
         below to simplify the expressions for latitude and longitude. */
			var x1frac = 1.0 / (Nfpow * cf);

			Nfpow *= Nf; /* now equals Nf**2) */
			var x2frac = tf / (2.0 * Nfpow);

			Nfpow *= Nf; /* now equals Nf**3) */
			var x3frac = 1.0 / (6.0 * Nfpow * cf);

			Nfpow *= Nf; /* now equals Nf**4) */
			var x4frac = tf / (24.0 * Nfpow);

			Nfpow *= Nf; /* now equals Nf**5) */
			var x5frac = 1.0 / (120.0 * Nfpow * cf);

			Nfpow *= Nf; /* now equals Nf**6) */
			var x6frac = tf / (720.0 * Nfpow);

			Nfpow *= Nf; /* now equals Nf**7) */
			var x7frac = 1.0 / (5040.0 * Nfpow * cf);

			Nfpow *= Nf; /* now equals Nf**8) */
			var x8frac = tf / (40320.0 * Nfpow);

			/* Precalculate polynomial coefficients for x**n.
         -- x**1 does not have a polynomial coefficient. */
			var x2poly = -1.0 - nuf2;

			var x3poly = -1.0 - 2 * tf2 - nuf2;

			var x4poly = 5.0 + 3.0 * tf2 + 6.0 * nuf2 - 6.0 * tf2 * nuf2 - 3.0 * (nuf2 * nuf2) -
							 9.0 * tf2 * (nuf2 * nuf2);

			var x5poly = 5.0 + 28.0 * tf2 + 24.0 * tf4 + 6.0 * nuf2 + 8.0 * tf2 * nuf2;

			var x6poly = -61.0 - 90.0 * tf2 - 45.0 * tf4 - 107.0 * nuf2 + 162.0 * tf2 * nuf2;

			var x7poly = -61.0 - 662.0 * tf2 - 1320.0 * tf4 - 720.0 * (tf4 * tf2);

			var x8poly = 1385.0 + 3633.0 * tf2 + 4095.0 * tf4 + 1575 * (tf4 * tf2);

			var x2 = x * x;
			var x3 = x2 * x;
			var x4 = x2 * x2;
			var x5 = x3 * x2;
			var x6 = x3 * x3;
			var x7 = x4 * x3;
			var x8 = x4 * x4;

			/* Calculate latitude */
			var lat = phif + x2frac * x2poly * x2 + x4frac * x4poly * x4 +
										 x6frac * x6poly * x6 + x8frac * x8poly * x8;

			/* Calculate longitude */
			var lon = lon0 + x1frac * x + x3frac * x3poly * x3 +
										 x5frac * x5poly * x5 + x7frac * x7poly * x7;

			return (lat, lon);
		}



		/*
    * LatLonToUTMXY
    *
    * Converts a latitude/longitude pair to x and y coordinates in the
    * Universal Transverse Mercator projection.
    *
    * Inputs:
    *   lat - Latitude of the point, in radians.
    *   lon - Longitude of the point, in radians.
    *   zone - UTM zone to be used for calculating values for x and y.
    *          If zone is less than 1 or greater than 60, the routine
    *          will determine the appropriate zone from the value of lon.
    *
    * Outputs:
    *   xy - A 2-element array where the UTM x and y values will be stored.
    *
    * Returns:
    *   The UTM zone used for calculating the values of x and y.
    *
    */
		public static (double x, double y) LatLonToUTMXY(double lat, double lon, int zone)
		{
			(double x, double y) = MapLatLonToXY(lat, lon, UTMCentralMeridian(zone));

			/* Adjust easting and northing for UTM system. */
			(x, y) = (x * UTMScaleFactor + 500000.0, y * UTMScaleFactor);
			if (y < 0.0) y += 10000000.0;

			return (x, y);
		}

		public static (double x, double y) LatLonToXYCart(double lat, double lon, int zone)
		{
			(double x, double y) = MapLatLonToXY(lat, lon, UTMCentralMeridian(zone));
			/* Adjust easting and northing for Cartesian system. */
			return (x * UTMScaleFactor + 500000.0, y * UTMScaleFactor);
		}


		/*
    * UTMXYToLatLon
    *
    * Converts x and y coordinates in the Universal Transverse Mercator
    * projection to a latitude/longitude pair.
    *
    * Inputs:
    *   x - The easting of the point, in meters.
    *   y - The northing of the point, in meters.
    *   zone - The UTM zone in which the point lies.
    *   southhemi - True if the point is in the southern hemisphere;
    *               false otherwise.
    *
    * Outputs:
    *   latlon - A 2-element array containing the latitude and
    *            longitude of the point, in radians.
    *
    * Returns:
    *   The function does not return a value.
    *
    */
		public static (double lat, double lon) UTMXYToLatLon(double x, double y, int zone, bool southhemi)
		{
			x -= 500000.0;
			x /= UTMScaleFactor;

			/* If in southern hemisphere, adjust y accordingly. */
			if (southhemi) y -= 10000000.0;

			y /= UTMScaleFactor;

			var cmeridian = UTMCentralMeridian(zone);
			return MapXYToLatLon(x, y, cmeridian);
		}

		/*eslint-enable */
		// Original code until here
		////////////////////////////


		static string bands = "CDEFGHJKLMNPQRSTUVWX";
		static int nBandIdx = bands.IndexOf('N');

		public static char calcBand(double lat)
		{
			if (lat < -80.0 || lat > 84.0) return '?';
			var bandIdx = (int)(lat/8 + 10);
			return bandIdx < bands.Length ? bands[bandIdx] : 'X'; // cover extra X band
		}

		public static int calcZone(char band, double lon)
		{
			var zone = (lon == 180.0) ? 60 : (int)(lon/6 + 30) + 1;

			if (band == 'V' && lon > 3.0 && lon < 7.0)
			{
				// Norway exception:
				zone = 32;
			}
			else if (band == 'X')
			{
				// Special zones for Svalbard
				if (lon >= 0.0 && lon < 9.0)
				{
					zone = 31;
				}
				else if (lon >= 9.0 && lon < 21.0)
				{
					zone = 33;
				}
				else if (lon >= 21.0 && lon < 33.0)
				{
					zone = 35;
				}
				else if (lon >= 33.0 && lon < 42.0)
				{
					zone = 37;
				}
			}
			return zone;
		}

		public static (double lat, double lon)? UTM2LatLon(Utm utm)
		{
			if (utm.southHemi == null && utm.band == null)
			{
				throw new Exception("Undefined hemisphere in " + utm.ToString());
			}
			var southHemi = utm.southHemi;
			var band = utm.band;
			if (band.HasValue && bands.IndexOf(band.Value) >= 0)
			{
				southHemi = bands.IndexOf(band.Value) < nBandIdx;
			}

			var latlon = UTMXYToLatLon(utm.x, utm.y, utm.zone, southHemi.Value);
			if (Math.Abs(latlon.lat) > pi / 2) return null;
			return (lat: RadToDeg(latlon.lat), lon: RadToDeg(latlon.lon));
		}

		public static Utm LatLon2UTM(double lat, double lon, int? zone = null, bool? southHemi = null)
		{
			//double max = 180d, min = -180d, d = max - min;
			//return x == max ? x : ((x - min) % d + d) % d + min;
			double wrapLon() => lon == 180d ? lon : ((lon + 180) % 360 + 360) % 360 - 180;

			lon = wrapLon();
			var band = calcBand(lat);
			zone = zone ?? calcZone(band, lon);
			southHemi = (southHemi == null) ? lat < 0 : southHemi.Value;

			var res = LatLonToUTMXY(DegToRad(lat), DegToRad(lon), zone.Value);
			// This is the object returned
			return new Utm(res.x, res.y,
								zone: zone.Value,
								band: band,
								southHemi: southHemi);
		}

		public static IEnumerable<geoPoint> UTM2LatLon(IEnumerable<UtmPoint> points, int zone, bool southHemi)
		{
			var pts = points.Select(p=>(x:(p.x - 500000)/UTMScaleFactor,y:(southHemi? p.y - 10000000 : p.y)/UTMScaleFactor));
			var hpi = pi / 2;
			var cmeridian = UTMCentralMeridian(zone);
			var lls = pts.Select(p=>MapXYToLatLon(p.x, p.y, cmeridian)).ToList();
			return lls.Select(ll=> Math.Abs(ll.lat) > hpi? null : new geoPoint(lat: RadToDeg(ll.lat), lon: RadToDeg(ll.lon)));
		}

		public static geoPoint UTM2LatLon(UtmPoint point, int zone, bool southHemi)
		{
			var pt = (x:(point.x - 500000) / UTMScaleFactor, y:(southHemi ? point.y - 10000000 : point.y) / UTMScaleFactor);
			var cmeridian = UTMCentralMeridian(zone);
			var ll = MapXYToLatLon(pt.x, pt.y, cmeridian);
			return Math.Abs(ll.lat) > (pi / 2) ? null : new geoPoint(RadToDeg(ll.lat), RadToDeg(ll.lon));
		}

		public static (double x,double y)  LatLon2UTMCart(double lat, double lon)
		{
			if (lon!=180d)
				lon = ((lon + 180d) % 360d + 360d) % 360d - 180d;

			var bandIdx = (int)(lat / 8 + 10);
			var band = bandIdx < bands.Length ? bands[bandIdx] : 'X'; // cover extra X band
			var zone = calcZone(band,lon);

			return LatLonToXYCart(DegToRad(lat), DegToRad(lon), zone);
		}

		public static IEnumerable<(double x, double y)> LatLon2UTMCarts(IEnumerable<(double lat, double lon)> coords)
		{
			//wraplon
			var co = coords.Select(c=>(c.lat, lon: c.lon == 180d ? c.lon : ((c.lon + 180d) % 360d + 360d) % 360d - 180d));

			return co.Select(c => {
				var bandIdx = (int)(c.lat / 8 + 10);
				var band = bandIdx < bands.Length ? bands[bandIdx] : 'X'; // cover extra X band
				var zone = calcZone(band, c.lon);
				return LatLonToXYCart(DegToRad(c.lat), DegToRad(c.lon), zone);
			});
		}

		public static IEnumerable<double> Distances_1ToRest(IEnumerable<(double x, double y)> coords)
		{
			if (coords.Count() < 2)
				return new double[0];

			var carts = LatLon2UTMCarts(coords);
			var first = carts.First();
			var rest = carts.Skip(1);
			return rest.Select(c => distance(first, c));

			double distance((double x, double y) p1, (double x, double y) p2)
				=> Math.Sqrt((p2.x - p1.x) * (p2.x - p1.x) + (p2.y - p1.y) * (p2.y - p1.y));
		}
	}

}
