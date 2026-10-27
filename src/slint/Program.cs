using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using lols;
using Slint;

internal static class Program
{
	const int Max = 500;

	// Windows needs a single-threaded apartment for the window.
	[STAThread]
	static int Main(string[] args)
	{
		var mode = args.Length > 0 ? args[0] : "canvas";
		if (mode is not ("canvas" or "elements"))
		{
			Console.Error.WriteLine("Usage: lols [canvas|elements]");
			return 2;
		}
		// canvas: like LolsView on the canvas branch, which queues 100000 lols and sleeps every 256.
		// elements: like the TextView version on main, which adds 5000 lols and sleeps after each.
		var total = mode == "canvas" ? 100_000 : 5_000;

		using var window = MainWindow.Create();
		var model = new ListModel<Lol>();
		window.Lols = model;

		// Slint objects belong to this thread, so the producer only fills the queue.
		var queue = new ConcurrentQueue<Lol>();
		_ = Task.Factory.StartNew(() =>
		{
			var random = Random.Shared;
			for (var produced = 1; produced <= total; produced++)
			{
				queue.Enqueue(new Lol
				{
					X = random.NextSingle(),
					Y = random.NextSingle(),
					Color = Color.FromArgb(0xFF000000 | (uint)random.Next(0xFFFFFF)),
					Rotation = random.NextSingle() * 360,
				});
				if (mode == "elements" || produced % 256 == 0)
				{
					Thread.Sleep(1);
				}
			}
		}, TaskCreationOptions.LongRunning);

		var count = 0;
		var next = 0;
		var stopwatch = Stopwatch.StartNew();

		// Applies the queued lols about once per frame, like Invalidate() on Android.
		using var drain = new Slint.Timer();
		drain.Start(TimerMode.Repeated, TimeSpan.FromMilliseconds(16), () =>
		{
			while (count < total && queue.TryDequeue(out var lol))
			{
				if (mode == "elements")
				{
					if (model.Count >= Max)
					{
						model.RemoveAt(0);
					}
					model.Add(lol);
				}
				else if (model.Count < Max)
				{
					model.Add(lol);
				}
				else
				{
					// A fixed set of rows: Slint only updates properties, and creates no elements.
					model[next] = lol;
					next = (next + 1) % Max;
				}
				count++;
			}
			if (count == total && stopwatch.IsRunning)
			{
				stopwatch.Stop();
				Console.WriteLine(string.Create(CultureInfo.InvariantCulture,
					$"{mode}: {Rate()} after {count} lols in {stopwatch.Elapsed.TotalSeconds:0.00} s"));
			}
		});

		using var update = new Slint.Timer();
		update.Start(TimerMode.Repeated, TimeSpan.FromMilliseconds(500), () => window.Rate = Rate());

		window.Run();
		return 0;

		string Rate() => "LOL/s: " + (count / stopwatch.Elapsed.TotalSeconds).ToString("0.00", CultureInfo.InvariantCulture);
	}
}
