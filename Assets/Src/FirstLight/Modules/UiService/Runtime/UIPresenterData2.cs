namespace FirstLight.UIService
{
	public abstract class UIPresenterData2<T> : UIPresenter2 where T : class
	{
		protected new T Data => (T) base.Data;
	}
}