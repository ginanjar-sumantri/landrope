using landrope.mod2;
using System;
using System.Threading;

namespace landrope.housekeeper
{
	public interface IHousekeeping
	{
		void Start(TimeSpan interval, ExtLandropeContext context);
	}

	public class Housekeeping : IHousekeeping
	{
		ExtLandropeContext context;
		Timer timer;
		public void Start(TimeSpan interval, ExtLandropeContext context)
		{
			this.context = context;
			timer = new Timer(ontimer, null, TimeSpan.Zero, interval);
		}

		void ontimer(object state)
		{
			Persil.FillBlankIDs(context);
		}

	}
}
