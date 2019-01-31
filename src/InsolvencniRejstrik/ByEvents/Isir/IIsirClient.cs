namespace InsolvencniRejstrik.ByEvents
{
	interface IIsirClient
	{
		string GetUrl(string spisovaZnacka);
		string GetSoud(string spisovaZnacka);
	}
}
