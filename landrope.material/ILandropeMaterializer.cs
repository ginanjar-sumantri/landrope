using landrope.mod;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace landrope.material
{
	public interface ILandropeMaterializer
	{
		void Start(LandropeContext context, TimeSpan interval, Action<LandropeContext> materializing);
		void Stop();
	}

	public class LandropeMaterializer : ILandropeMaterializer
	{
		Timer tmr = null;
		LandropeContext context;
		Action<LandropeContext> materializing;

		// [ [[agg 1A, agg 1B...], [agg 11A, agg 11B...], [agg 12A, agg 12B...]],
		//   [[agg 2A, agg 2B...], [agg 21A, agg 21B...]], [agg 22A, agg 22B...]],
		//   ...
		//

		private void OnTimer(object state)
		{
			if (context != null && materializing != null)
				materializing.Invoke(context);
		}

		public void Start(LandropeContext context, TimeSpan interval, Action<LandropeContext> materializing)
		{
			this.materializing = materializing;
			if (tmr != null)
				tmr.Change(TimeSpan.Zero, interval);
			else
			{
				this.context = context;
				tmr = new Timer(OnTimer, null, TimeSpan.Zero, interval);
			}
		}

		public void Stop()
		{
			tmr.Dispose();
			tmr = null;
		}
	}
}
