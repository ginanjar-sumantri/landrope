using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;
using System.Linq;

namespace mycomponents
{
	public partial class ItemScroll : UserControl
	{
		int limit = 0;
		public int Limit {
			get => limit;
			set { 
				limit = value;
				if (Value > limit)
					Value = limit;
				else if (limit > 0 && Value == 0)
					Value = 1;
			} 
		}

		int _value=0;
		public int Value 
		{ get => _value;
			set
			{
				var changed = value != _value;
				_value = value;
				if (limit == 0)
					textBox1.Text = "";
				else
					textBox1.Text = $"{_value}";
				if (changed && ValueChange != null && ValueChange.GetInvocationList().Any())
					ValueChange.Invoke(this, new ScrollValueChangeArgs(value));
			} 
		}
		public event ScrollValueChangeHandler ValueChange;

		public ItemScroll()
		{
			InitializeComponent();
		}

		public delegate void ScrollValueChangeHandler(object sender, ScrollValueChangeArgs e);

		private void button1_Click(object sender, EventArgs e)
		{
			if (_value > 1)
				Value = _value - 1;
		}

		private void button2_Click(object sender, EventArgs e)
		{
			if (_value < limit)
				Value = _value + 1;
		}

		public void MoveLast()
		{
			Value = Limit;
		}

	}

	public class ScrollValueChangeArgs : EventArgs
	{
		public readonly int Value;
		internal ScrollValueChangeArgs(int value)
		{
			Value = value;
		}
	}
}
