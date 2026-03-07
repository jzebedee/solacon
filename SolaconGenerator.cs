#nullable enable

using System;
using System.Globalization;
using System.Net;
using System.Text;

namespace Solacon;

/// <summary>
/// Generates deterministic static SVG solacons from seed values.
/// </summary>
public static class SolaconGenerator
{
    /// <summary>
    /// Generates a static SVG solacon for the supplied seed.
    /// </summary>
    /// <param name="seed">The deterministic seed used to derive the solacon geometry and default color.</param>
    /// <param name="rgb">
    /// An optional RGB triplet in the original solacon format, for example <c>0, 30, 255</c>.
    /// When omitted, the color is derived from the hash in the same way as <c>setRGBFromHash()</c>.
    /// </param>
    /// <param name="includeTitle">Whether to include a <c>&lt;title&gt;</c> element in the generated SVG.</param>
    /// <param name="title">
    /// An optional title string to use instead of the default visual hash title. Ignored when <paramref name="includeTitle"/> is <see langword="false"/>.
    /// </param>
    /// <returns>An SVG document string equivalent to the inline JavaScript solacon renderer.</returns>
    public static string GenerateSvg(string seed, string? rgb = null, bool includeTitle = true, string? title = null)
    {
        ArgumentNullException.ThrowIfNull(seed);

        const double canvasSize = 1000d;
        const double radius = canvasSize / 2d;
        const double center = canvasSize / 2d;

        static uint Sdbm(string value)
        {
            if (value.Length < 6)
            {
                value += value + value + value + value;
            }

            uint hash = 0;
            foreach (var ch in value)
            {
                hash = ch + (hash << 6) + (hash << 16) - hash;
            }

            return hash;
        }

        static (double X, double Y) Point(double theta, double r) => (r * Math.Cos(theta), r * Math.Sin(theta));

        static string PointString((double X, double Y) point, double offsetX, double offsetY, string? command = null)
        {
            var prefix = command is null ? string.Empty : $"{command} ";
            NiceDecimal nx = point.X + offsetX;
            NiceDecimal ny = point.Y + offsetY;
            return $"{prefix}{nx} {ny}";
        }

        static string Bezier(double angleStart, double angleEnd, double radiusStart, double radiusEnd, double offsetX, double offsetY)
        {
            var start = Point(angleStart, radiusStart);
            var end = Point(angleEnd, radiusEnd);
            var delta = (angleEnd - angleStart) / 3d;

            var control1 = Point(angleStart + delta, (radiusStart + radiusEnd) / 2d);
            var control2 = Point(angleEnd - delta, (radiusStart + radiusEnd) / 2d);
            var path = $"{PointString(start, offsetX, offsetY, "M")} C {PointString(control1, offsetX, offsetY)}, {PointString(control2, offsetX, offsetY)}, {PointString(end, offsetX, offsetY)}";

            control1 = Point(angleStart + delta, (radiusStart + radiusEnd) / 3d);
            control2 = Point(angleEnd - delta, (radiusStart + radiusEnd) / 3d);
            path += $" C {PointString(control1, offsetX, offsetY)}, {PointString(control2, offsetX, offsetY)}, {PointString(start, offsetX, offsetY)}";

            return path;
        }

        var hash = Sdbm(seed);
        var slices = (int)(hash & 0x07) + 3;
        var wedgeAngle = (Math.PI * 2d) / slices;

        var resolvedRgb = rgb;
        if (string.IsNullOrWhiteSpace(resolvedRgb))
        {
            NiceDecimal r = ((hash & 0x0Fu) / 15d) * 255d;
            NiceDecimal g = (((hash >> 4) & 0x0Fu) / 15d) * 255d;
            NiceDecimal b = (((hash >> 8) & 0x0Fu) / 15d) * 255d;
            resolvedRgb = $"{r},{g},{b}";
        }

        var swishes = new (double Radius1, double Radius2, int Alpha)[6];
        for (var i = 0; i < swishes.Length; i++)
        {
            swishes[i] = (
                ((hash >> (i * 3)) & 0x07) / 7d,
                ((hash >> ((i * 3) + 1)) & 0x07) / 7d,
                (int)((hash >> ((i * 3) + 2)) & 0x07));
        }

        var builder = new StringBuilder(2048);
        builder.Append("<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 1000 1000\">");
        if (includeTitle)
        {
            builder.Append("<title>");
            if (title is not null)
            {
                builder.Append(WebUtility.HtmlEncode(title));
            }
            else
            {
                builder.Append($"A visual hash representation of the string: {hash}");
            }
            builder.Append("</title>");
        }

        builder.Append("<defs><g id=\"w\">");

        var angleDegrees = 360d / slices;
        foreach (var (Radius1, Radius2, Alpha) in swishes)
        {
            var opacity = new NiceDecimal(Alpha / 7d);
            var path = Bezier(0d, wedgeAngle, radius * Radius1, radius * Radius2, center, center);
            builder.Append("<path fill-opacity=\"");
            builder.Append(opacity);
            builder.Append("\" d=\"");
            builder.Append(path);
            builder.Append("\" />");
        }

        builder.Append("</g></defs><g fill=\"rgb(");
        builder.Append(resolvedRgb);
        builder.Append(")\">");

        for (var sliceIndex = 0; sliceIndex < slices; sliceIndex++)
        {
            builder.Append("<use href=\"#w\"");
            if (sliceIndex > 0)
            {
                builder.Append(" transform=\"rotate(");
                builder.Append(new NiceDecimal(angleDegrees * sliceIndex));
                builder.Append(" 500 500)\"");
            }

            builder.Append(" />");
        }

        builder.Append("</g></svg>");
        return builder.ToString();
    }
}
