namespace landrope.common
{
	public interface IMultiChild
	{
		bool CanAdd(string collname);
		object Add(string collname);
	}
}