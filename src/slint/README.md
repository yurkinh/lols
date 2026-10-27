# LOLs per Second in Slint for .NET

A port of the benchmark to [Slint for .NET](https://www.nuget.org/packages/yurkinh.slint.net), for internal testing.
It runs on macOS, Linux, and Windows; Slint for .NET has no Android or iOS packages yet.

Run it:

```sh
dotnet run -- canvas
dotnet run -- elements
```

Measure a published, native build:

```sh
dotnet publish -c Release
SLINT_BACKEND=winit-skia bin/Release/net10.0/osx-arm64/publish/lols canvas
```

`SLINT_BACKEND=winit-femtovg` selects the other renderer.
When the run ends, the app prints the final rate to the console and keeps the window open.

## Modes

- `canvas`, the default, follows `LolsView` on the `canvas` branch:
  a background thread queues 100,000 lols and sleeps 1 ms after every 256.
  The window shows the latest 500 in a fixed set of rows, so Slint updates properties and creates no elements.
- `elements` follows the `TextView` version on `main`:
  a background thread queues 5,000 lols and sleeps 1 ms after each.
  Slint adds a `Text` for each lol and removes the oldest one at 500.

In both modes, a Slint timer applies the queued lols about once per frame,
since Slint objects belong to the main thread, and the count goes up as each lol reaches the model.

## Results

Apple Silicon Mac, macOS, Native AOT build of `yurkinh.slint.net` 1.19.0-preview.6:

| Mode | femtovg | Skia |
|---|---|---|
| `canvas` | ~203,000 LOL/s | ~204,000 LOL/s |
| `elements` | ~795 LOL/s | ~793 LOL/s |

Both rates are limited by the `Thread.Sleep` calls of the original, not by Slint:
256 lols per sleep in `canvas`, and one lol per sleep, about 1.1 ms on macOS, in `elements`.
As the main README says about the `canvas` branch, such numbers mostly count objects put in a queue.
Slint also only creates and draws the elements that a frame renders,
so lols that are added and removed between two frames cost little.
A rate without the sleeps measures the same queue and model updates, not drawing, and varied several-fold between runs.
A frames-per-second measurement would say more, but needs a frame counter that Slint for .NET doesn't expose yet.
