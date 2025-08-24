using System.Diagnostics.Metrics;

namespace HypixelAPI.Metrics;

public interface IHypixelRequestsCounter {
	void Increment(int value = 1);
}

public class HypixelRequestsCounter : IHypixelRequestsCounter {
	private readonly Counter<int> _requestsCounter;

	public HypixelRequestsCounter(IMeterFactory meterFactory) {
		var meter = meterFactory.Create("hypixel.api");
		_requestsCounter = meter.CreateCounter<int>("hypixel.api.requests", description: "The number of requests in total.");
	}
	
	public void Increment(int value = 1) {
		_requestsCounter.Add(value);
	}
}