# AGENTS.md

## Project overview

Edi.ImageWatermark is a small .NET class library for adding text watermarks to images with SkiaSharp. The public surface is intentionally compact:

- `IImageWatermarker` and `ImageWatermarker` in `src/Edi.ImageWatermark/`
- `WatermarkPosition` enum with `TopLeft`, `TopRight`, `BottomLeft`, `BottomRight`, and `Center`
- xUnit tests in `src/Edi.ImageWatermark.Tests/`
- A Windows Forms manual test app in `src/Edi.ImageWatermark.ManualTest/`

Treat this as a NuGet package first. Avoid changes that casually alter public API behavior, constructor parameters, method signatures, default values, package metadata, or encoded output behavior.

## Build and test

Run commands from the `src` directory unless noted otherwise:

```powershell
dotnet build --configuration Release
dotnet test --configuration Release
dotnet pack Edi.ImageWatermark/Edi.ImageWatermark.csproj --configuration Release -o nupkg
```

The GitHub Actions workflow uses Windows and .NET `10.0.x`. Keep changes compatible with the current `net10.0` target unless the task explicitly asks for framework changes.

## Coding conventions

- Follow the existing file-scoped namespace style.
- Prefer simple, explicit C# over new abstractions; this library has a deliberately small implementation.
- Use `using var`/`Dispose` patterns consistently for SkiaSharp objects, streams, images, codecs, typefaces, fonts, paints, and canvases.
- Preserve stream usability: returned `MemoryStream` instances should be positioned at `0` before returning.
- Keep exception behavior intentional and covered by tests. Existing validation throws `ArgumentException`, `ArgumentOutOfRangeException`, `InvalidOperationException`, `FileNotFoundException`, or `PlatformNotSupportedException` depending on the failure.
- Do not add broad catch blocks unless they preserve disposal semantics and rethrow without hiding the original exception.
- Do not introduce external image files for tests when SkiaSharp can generate test images in memory.

## Image processing rules

- Decode input with SkiaSharp and preserve the detected encoded format when saving whenever supported by SkiaSharp. When the detected format is not supported for encoding by SkiaSharp, fall back to PNG and do not throw; document this behavior with a test.
- Be careful with coordinate math. Watermark placement should keep text within image bounds for all `WatermarkPosition` values, including small images and large text.
- Continue supporting the pixel-threshold behavior: when enabled and the image area is below the threshold, `AddWatermark` returns `null`.
- Font selection must stay cross-platform. Windows/macOS use Arial when available; Linux should try font families ["DejaVu Sans", "Liberation Sans", "FreeSans", "Noto Sans"] and directories ["/usr/share/fonts", "/usr/local/share/fonts", "~/.fonts"] before failing.
- If a custom font path is supplied, validate that the file exists and load the typeface from that file.

## Testing guidance

- Add or update xUnit tests for every behavior change in `ImageWatermarker`.
- Prefer `[Fact]` for single behavior checks and `[Theory]` with `[InlineData]` for positions or formats.
- Generate test images with `SKSurface` and encode them to a `MemoryStream`; reset stream position before using it.
- Test public behavior rather than private helpers directly. The existing tests exercise private positioning, format, and font logic through `AddWatermark`.
- For format-related changes, cover `.png`, `.jpg`/`.jpeg`, `.bmp`, `.gif`, and `.webp` where practical.

## Packaging and repository notes

- The NuGet package includes the root `README.md` and `img/edi-logo-blue.png`; do not remove these packing items accidentally.
- Avoid changing `<Version>`, package metadata, or publish workflow behavior unless the task is specifically release-related.
- Keep the Windows Forms manual test app as a consumer/demo of the library. Do not move production watermarking behavior into the manual test project.
- The solution file is `src/Edi.ImageWatermark.slnx`.
