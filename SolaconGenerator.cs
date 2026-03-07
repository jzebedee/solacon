#nullable enable

using System;
using System.Globalization;
using System.Text;

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
    /// <returns>An SVG document string equivalent to the inline JavaScript solacon renderer.</returns>
    public static string GenerateSvg(string seed, string? rgb = null)
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

        static double NiceDecimal(double value) => Math.Floor((value * 1000d) + 0.5d) / 1000d;

        static string Format(double value) => NiceDecimal(value).ToString("0.###", CultureInfo.InvariantCulture);

        static (double X, double Y) Point(double theta, double r) => (r * Math.Cos(theta), r * Math.Sin(theta));

        static string PointString((double X, double Y) point, double offsetX, double offsetY, string? command = null)
        {
            var prefix = command is null ? string.Empty : $"{command} ";
            return $"{prefix}{Format(point.X + offsetX)} {Format(point.Y + offsetY)}";
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

        static string EscapeXml(string value) =>
            value
                .Replace("&", "&amp;", StringComparison.Ordinal)
                .Replace("<", "&lt;", StringComparison.Ordinal)
                .Replace(">", "&gt;", StringComparison.Ordinal)
                .Replace("\"", "&quot;", StringComparison.Ordinal)
                .Replace("'", "&apos;", StringComparison.Ordinal);

        var hash = Sdbm(seed);
        var slices = (int)(hash & 0x07) + 3;
        var wedgeAngle = (Math.PI * 2d) / slices;

        var resolvedRgb = rgb;
        if (string.IsNullOrWhiteSpace(resolvedRgb))
        {
            var red = NiceDecimal((((hash & 0x0Fu) / 15d) * 255d));
            var green = NiceDecimal(((((hash >> 4) & 0x0Fu) / 15d) * 255d));
            var blue = NiceDecimal(((((hash >> 8) & 0x0Fu) / 15d) * 255d));
            resolvedRgb = $"{Format(red)},{Format(green)},{Format(blue)}";
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
        builder.Append("<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 1000 1000\" preserveAspectRatio=\"xMidYMid meet\" role=\"img\" aria-labelledby=\"solacon-title\">");
        builder.Append("<title id=\"solacon-title\">");
        builder.Append(EscapeXml($"A visual hash representation of the string: {hash}"));
        builder.Append("</title>");

        for (var sliceIndex = 0; sliceIndex < slices; sliceIndex++)
        {
            var angleStart = wedgeAngle * sliceIndex;
            var angleEnd = wedgeAngle * (sliceIndex + 1);

            foreach (var swish in swishes)
            {
                var opacity = Format(swish.Alpha / 7d);
                var path = Bezier(angleStart, angleEnd, radius * swish.Radius1, radius * swish.Radius2, center, center);
                builder.Append("<path fill=\"rgba(");
                builder.Append(resolvedRgb);
                builder.Append(", ");
                builder.Append(opacity);
                builder.Append(")\" d=\"");
                builder.Append(path);
                builder.Append("\" class=\"solacon-shade-");
                builder.Append(swish.Alpha.ToString(CultureInfo.InvariantCulture));
                builder.Append("\" />");
            }
        }

        builder.Append("</svg>");
        return builder.ToString();
    }
}
