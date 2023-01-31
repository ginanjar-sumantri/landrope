using geo.shared;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
//using System.Windows.Media.Media3D;
//using Triangulator.Geometry;

namespace GeomHelper
{

	public static class PointHelper
	{

		public static bool between(this double x, double bound1, double bound2) => (bound1 <= x && x <= bound2) || (bound2 <= x && x <= bound1);
		public static bool between(this long x, long bound1, long bound2) => (bound1 <= x && x <= bound2) || (bound2 <= x && x <= bound1);

	}

	public partial class Polygon
	{
		internal interface IChain
		{
			ChainList Siblings { get; } // should contains LSegments
			Guid tag { get; }
			void Remove(Guid tag);
			void Add(IChain item);
			IChain[] otherChains(IChain item);

			void Replace(Guid oldTag, IChain replacer);

			void Dispose();
		}

		internal class ChainList : List<IChain>
		{
			public void Remove(Guid tag)
			{
				IChain item = this.FirstOrDefault(c => c.tag == tag);
				if (item != null)
					Remove(item);
			}
		}

		internal abstract class Chain : IChain
		{
			public ChainList Siblings { get; private set; } // should contains LSegments
			public Guid tag { get; private set; }
			public abstract bool canAdd { get; }

			public Chain()
			{
				tag = Guid.NewGuid();
				Siblings = new ChainList();
			}

			public virtual IChain[] otherChains(IChain item)
			{
				return Siblings.Where(l => l.tag != item.tag).ToArray();
			}

			public virtual void Remove(Guid tag)
			{
				IChain sibling = Siblings.FirstOrDefault(c => c.tag == tag);
				if (sibling != null)
				{
					Siblings.Remove(sibling);
					sibling.Siblings.Remove(this.tag);
				}
			}

			public virtual void Add(IChain item)
			{
				if (canAdd && ((Chain)item).canAdd)
				{
					Siblings.Add(item);
					item.Siblings.Add(this);
				}
			}

			public void Replace(Guid oldTag, IChain replacement)
			{
				Remove(tag);
				Add(replacement);
			}

			public virtual void Dispose() { }
		}

		internal abstract class Node : Chain
		{
			public Point location;

			public Node(Point location)
				: base()
			{
				this.location = location;
			}

		}

		internal class Vertex : Node// 2 siblings only
		{
			public char? state;

			public List<LSegment> candidates = new List<LSegment>();

			public IChain GetReplacement(IChain item)
			{
				IChain[] others = otherChains(item);
				return others.Any() ? others[0] : null;
			}

			public override void Dispose()
			{
				if (Siblings.Count == 0)
					return;

				LSegment seg1 = Siblings[0] as LSegment;
				Node node0 = seg1.otherChains(this)?.Cast<Node>().FirstOrDefault();
				LSegment seg2 = Siblings.Count > 1 ? Siblings[1] as LSegment : null;
				Node node1 = seg2?.otherChains(this)?.Cast<Node>().FirstOrDefault();
				seg1.Dispose();
				if (seg2 != null)
				{
					if (node0 == null)
						return;
					if (node1 == null || (node0 is Crosser && node1 is Crosser))
						seg2.Dispose();
					else
						seg2.Replace(this.tag, node0);
				}
			}

			public Vertex(Point location, char? state)
				: base(location)
			{
				this.state = state;
			}
			public override bool canAdd => Siblings.Count < 2;
		}

		internal class Crosser : Node // can up to 4 siblings
		{
			public Crosser(Point location)
				: base(location)
			{
			}

			public override IChain[] otherChains(IChain item)
			{
				return Siblings.Cast<LSegment>().Where(c => c.tag != item.tag && c.group == ((LSegment)item).group).ToArray();
			}
			public override bool canAdd => Siblings.Count < 4;

			public LSegment Split(LSegment segment, Node anchor)
			{
				if (segment.Siblings.Count < 2) // invalid segment
				{
					segment.Siblings.Add(this);
					return null;
				}
				Node node = segment.Siblings.Cast<Node>().FirstOrDefault(k => k.location == location);
				if (node != null) // crossing in a vertex?
				{
					if (!(node is Vertex))
						return null;
					// replac the vertex  with this Crossing
					List<IChain> chains = node.Siblings;
					foreach (IChain item in chains)
					{
						item.Replace(node.tag, this);
					}
					node.Dispose();
					return null;
				}

				Node n = segment.otherChains(anchor).Cast<Node>().FirstOrDefault();
				segment.Replace(n.tag, this);

				LSegment newsegment = new LSegment(segment.group, this, n);
				return newsegment;
			}

			public Vertex[] Reconnect()
			{
				if (Siblings.Count == 2)
				{
					Vertex nver = new Vertex(this.location, 'o');
					IChain[] chains = Siblings.ToArray();
					Siblings.ForEach(s => Remove(s.tag));
					chains.ToList().ForEach(c => nver.Add(c));
					return new Vertex[] { nver };
				}
				string[] groups = Siblings.Cast<LSegment>().Select(s => s.group).Distinct().ToArray();
				List<Vertex> vertices = new List<Vertex>();
				foreach (string group in groups)
				{
					LSegment first = Siblings.Cast<LSegment>().FirstOrDefault(s => s.group == group);
					if (first != null)
						continue;
					LSegment next = Siblings.Cast<LSegment>().Where(s => s.group != group).OrderByDescending(s => Cosine(first, s, this)).FirstOrDefault();
					if (next!=null)
					{
						Vertex nvx = new Vertex(this.location, 'o');
						first.Replace(this.tag, nvx);
						next.Replace(this.tag, nvx);
						vertices.Add(nvx);
					}
				}
				Siblings.ForEach(s => Remove(s.tag));
				return vertices.ToArray();
			}

			private double Cosine(LSegment s1, LSegment s2, Node joint)
			{
				Segment sg1 = s1.getSegment(joint.location);
				Segment sg2 = s2.getSegment(joint.location);
				if (sg1 == null || sg2 == null)
					return -1;
				return sg1.CosineTo(sg2.Pt2);
			}
		}

		internal class LSegment : Chain
		{
			private Segment stuff = null;
			public string group;

			public LSegment(string group, Node node1, Node node2)
				: base()
			{
				this.group = group;
				Add(node1);
				Add(node2);
			}

			public Segment segment
			{
				get
				{
					if (Siblings.Count < 2 || !(Siblings[0] is Node && Siblings[1] is Node))
						return null;
					Point Pt1 = ((Node)Siblings[0]).location;
					Point Pt2 = ((Node)Siblings[1]).location;
					if (stuff == null)
						stuff = new Segment(Pt1, Pt2);
					else
					{
						stuff.Pt1 = Pt1;
						stuff.Pt2 = Pt2;
					}
					return stuff;
				}
			}

			public Segment getSegment(Point start)
			{
				if (Siblings.Count < 2 || !(Siblings[0] is Node && Siblings[1] is Node))
					return null;

				Point Pt1 = ((Node)Siblings[0]).location;
				Point Pt2 = ((Node)Siblings[1]).location;
				if (Pt1 != start && Pt2 != start)
					return null;
				if (Pt2==start)
				{
					Point Ptx = Pt2;
					Pt2 = Pt1;
					Pt1 = Ptx;
				}
				if (stuff == null)
					stuff = new Segment(Pt1, Pt2);
				else
				{
					stuff.Pt1 = Pt1;
					stuff.Pt2 = Pt2;
				}
				return stuff;
			}

			public override bool canAdd => Siblings.Count < 2;

			public override void Dispose()
			{
				Siblings.ToArray().ToList().ForEach(s => Remove(s.tag));
			}
		}

		public static Polygon operator |(Polygon left, Polygon right) // OR-ing (union)
		{
			List<Vertex> leftNodes = left.pts.Skip(1).Select(p => new Vertex(p, 'o')).ToList();
			List<Vertex> rightNodes = right.pts.Skip(1).Select(p => new Vertex(p, 'o')).ToList();
			List<LSegment> leftSegments = new List<LSegment>();
			List<LSegment> rightSegments = new List<LSegment>();

			for (int i = 0, j = leftNodes.Count - 1; i < leftNodes.Count; j = i++)
				leftSegments.Add(new LSegment("L", leftNodes[j], leftNodes[i]));
			for (int i = 1, j = rightNodes.Count - 1; i < rightNodes.Count; j = i++)
				rightSegments.Add(new LSegment("R", rightNodes[j], rightNodes[i]));

			// marking insider left segments
			leftNodes.ForEach(s => { if (right.IsInside(s.location)) s.state = 'i'; });
			rightNodes.ForEach(s => { if (left.IsInside(s.location)) s.state = 'i'; });

			List<Crosser> crossers = new List<Crosser>();
			//find for crossing segments
			Node leftAnchor = leftSegments[0].Siblings[0] as Node;
			Node rightAnchor = rightSegments[0].Siblings[0] as Node;
			Node leftRun = leftAnchor;
			Node rightRun;
			do
			{
				LSegment lfocus = leftRun.Siblings[0] as LSegment;
				rightRun = rightAnchor;
				do
				{
					LSegment rfocus = rightRun.Siblings[0] as LSegment;
					Segment.Intersection inter = lfocus.segment.Intersect(rfocus.segment);
					if (inter.point != null && inter.in_this && inter.in_other)
					{
						Crosser crs = new Crosser(inter.point);
						LSegment ls = crs.Split(lfocus, leftRun);
						if (ls != null)
							leftSegments.Add(ls);
						ls = crs.Split(rfocus, rightRun);
						if (ls != null)
							rightSegments.Add(ls);
						crossers.Add(crs);
					}
					rightRun = rfocus.otherChains(rightRun).Cast<Node>().FirstOrDefault();
				} while (rightRun != null && rightRun != rightAnchor);
				leftRun = lfocus.otherChains(leftRun).Cast<Node>().FirstOrDefault();
			} while (leftRun != null && leftRun != leftAnchor);

			// removing left vertices which are inside polygon, walking start from outside vertex
			leftRun = leftAnchor;
			while (!(leftRun is Vertex) || ((Vertex)leftRun).state == 'o')
			{
				leftRun = leftRun.Siblings[1].Siblings[1] as Node;
				if (leftRun == leftAnchor)
					break;
			}
			leftAnchor = leftRun;
			do
			{
				Node tmp = leftRun.Siblings[0].otherChains(leftRun).Cast<Node>().FirstOrDefault();
				if (leftRun is Vertex && ((Vertex)leftRun).state == 'i')
				{
					((Vertex)leftRun).Dispose();
					leftRun = tmp;
				}
				else
					leftRun = tmp;
			} while (leftRun != null && leftRun != leftAnchor);

			// removing right vertices which are inside polygon, walking start from outside vertex
			rightRun = rightAnchor;
			while (!(rightRun is Vertex) || ((Vertex)rightRun).state == 'o')
			{
				rightRun = rightRun.Siblings[1].Siblings[1] as Node;
				if (rightRun == rightAnchor)
					break;
			}
			rightAnchor = rightRun;
			do
			{
				Node tmp = rightRun.Siblings[0].otherChains(rightRun).Cast<Node>().FirstOrDefault();
				if (rightRun is Vertex && ((Vertex)rightRun).state == 'i')
				{
					((Vertex)rightRun).Dispose();
					rightRun = tmp;
				}
				else
					rightRun = tmp;
			} while (rightRun != null && rightRun != rightAnchor);

			crossers.ForEach(c => c.Reconnect());
			crossers.Clear();

			List<List<Segment>> segments = new List<List<Segment>>();

			List<Vertex> allnodes = leftNodes.Union(rightNodes).ToList();

			Vertex anchor = allnodes[0];
			if (anchor != null)
			{
				while (allnodes.Any())
				{
					List<Segment> seglist = new List<Segment>();
					segments.Add(seglist);

					Vertex runner = anchor;
					do
					{
						LSegment sib = leftRun.Siblings[0] as LSegment;
						Segment seg = sib.getSegment(leftRun.location);
						if (seg != null)
							seglist.Add(seg);
						leftNodes.Remove(runner);
						runner = sib.otherChains(runner).Cast<Vertex>().FirstOrDefault();
					}
					while (runner != null && runner != anchor);
				}
			}
			if (!segments.Any())
				return null;
			List<Point> pts = segments[0].Select(s => s.Pt1).ToList();
			if (pts.Last() != pts.First())
				pts.Add(pts.First());

			return new Polygon(false, pts.ToArray());
		}
	}
}
