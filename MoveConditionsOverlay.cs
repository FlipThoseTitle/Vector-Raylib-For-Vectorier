using Raylib_cs;
using System.Numerics;
using Vector.Core.Animation;
using Vector.Core.Animation.Events;
using Vector.Core.Detector;
using Vector.Core.Location;
using Vector.Core.Models;
using Vector.Core.Runners;

namespace Vector
{
    /// <summary>
    /// I key: shows the conditions of the moves the player can switch to right now, i.e. the reactions of every event
    /// (Keys, OnEnd, DetectorH, DetectorV, OnCollision, OnFrame, OnArea/OnTrigger) of the intervals active at the
    /// current frame, groups included (Groups="RunGroup").
    ///
    /// On the level each H / V condition is drawn relative to the platform under its detector, the platform that
    /// would trigger it:
    ///  - DeltaX / DeltaY: the zone the detector has to be in, measured from the condition's corner of the platform
    ///    (0 top-left, 1 top-right, 2 bottom-right, 3 bottom-left; mirrored when the player faces left), with the
    ///    Min / Max written on its lines;
    ///  - Width / Height: a ruler along the platform's top / left edge with the Min / Max marks;
    ///  - Sides: the allowed sides (0 top, 1 right, 2 bottom, 3 left) of the detector event's platform.
    /// With no platform under the detector, the DeltaX / DeltaY zone is drawn dashed around the detector instead: the
    /// zone a platform's corner has to be in for the condition to hold.
    ///
    /// The list on the left names each move with its conditions and their current value: green when it holds now,
    /// red when it does not, grey when it cannot be measured (C = collision conditions, measured at the hit).
    /// </summary>
    public static class MoveConditionsOverlay
    {
        const float FontSize = 13;
        const float LineHeight = 15;
        const float Open = 70; // world units an unbounded side (no Min or no Max) is drawn to

        static readonly Color Pass = new(110, 230, 120, 255);
        static readonly Color Fail = new(255, 95, 95, 255);
        static readonly Color Unknown = new(170, 170, 170, 255);
        static readonly Color Heading = new(255, 200, 80, 255);
        static readonly Color Text = new(235, 235, 235, 255);
        static readonly Color PanelBack = new(15, 15, 18, 170);

        // One color per move so its zones and labels can be told apart.
        static readonly Color[] Palette =
        {
            new(0, 200, 255, 255), new(255, 120, 200, 255), new(255, 170, 0, 255), new(140, 255, 90, 255),
            new(190, 140, 255, 255), new(255, 255, 90, 255), new(90, 255, 220, 255), new(255, 110, 70, 255),
        };

        record struct Entry(string Event, AnimationReaction Reaction, DetectorLine? EventDetector);

        public static void Draw(Camera2D camera)
        {
            var player = LevelMainController.current?.Location?.GetUserModel();
            var animations = player?.ControllerAnimations;
            if (player == null || animations?.Animation == null || Sets.Current == null)
                return;

            var vs = Sets.Current.Containers[1];
            var obj = player.ModelObject;
            int sign = player.Sign;
            var velocity = player.DeltaVelocity(Vector3.Zero);
            var deltaH = player.DeltaDetector(obj.DetectorHorizontalLine);
            var deltaV = player.DeltaDetector(obj.DetectorVerticalLine);

            Vector2 ToScreen(Vector3 p) => Raylib.GetWorldToScreen2D(vs.TransformPoint(p), camera);

            var intervals = animations.CurrentIntervals;
            drawn.Clear();
            stacks.Clear();
            placed.Clear();

            // The list is made of blocks (a heading, or one move with its conditions) that are never split.
            var blocks = new List<List<(string text, Color color, float indent)>>
            {
                new() { ($"Move conditions (I): {animations.Animation.Name}, frame {animations.CurrentFrame}, facing {(sign == 1 ? "right" : "left")}", Heading, 0) },
            };
            if (intervals.Count == 0)
                blocks[0].Add(("No interval at this frame.", Text, 8));

            int colorIndex = 0;
            foreach (var interval in intervals)
            {
                var entries = Collect(interval, obj);
                var head = new List<(string, Color, float)> { ($"Interval {interval.BeginFrame}-{interval.EndFrame}{(entries.Count == 0 ? ": no moves" : "")}", Heading, 0) };
                blocks.Add(head);

                foreach (var entry in entries)
                {
                    var reaction = entry.Reaction;
                    var color = Palette[colorIndex++ % Palette.Length];
                    var lines = new List<(string text, Color color, float indent)>();
                    blocks.Add(lines);
                    lines.Add(($"{entry.Event} > {reaction.Name}{(reaction.Priority != 0 ? $"  (priority {reaction.Priority})" : "")}", color, 6));

                    string extras = Extras(reaction, sign);
                    if (extras.Length > 0)
                        lines.Add((extras, Text, 16));

                    foreach (var (deltas, type) in new[] { (reaction.DeltasH, "H"), (reaction.DeltasV, "V"), (reaction.DeltasC, "C") })
                    {
                        var data = type == "H" ? deltaH : type == "V" ? deltaV : null;
                        foreach (var delta in deltas.Items)
                            lines.Add(Describe(delta, type, data, sign, velocity));
                    }

                    DrawOnLevel(entry, color, sign, obj, ToScreen);
                }
            }

            DrawPanel(blocks);
        }

        // The same condition is often shared by several moves (e.g. DeltaY <= 0 at corner 0): each line, ruler and
        // side is drawn once (in the first move's color), and move names at the same place are stacked.
        static readonly HashSet<string> drawn = new();
        static readonly Dictionary<string, int> stacks = new();

        static bool FirstTime(string key) => drawn.Add(key);

        /// <summary>Screen rectangles of the labels already drawn this frame: a new label moves down past them.</summary>
        static readonly List<Raylib_cs.Rectangle> placed = new();

        static string Key(Vector2 p) => $"{MathF.Round(p.X)},{MathF.Round(p.Y)}";

        /// <summary>A label under the previous ones placed at the same anchor.</summary>
        static void StackedLabel(string text, Vector2 anchor, Vector2 offset, Color color)
        {
            if (!FirstTime("label|" + Key(anchor) + "|" + text))
                return;
            string key = Key(anchor);
            stacks.TryGetValue(key, out int index);
            stacks[key] = index + 1;
            Label(text, anchor + offset + new Vector2(0, index * LineHeight), color);
        }

        /// <summary>Every (event, reaction) pair of the interval, in the order moves.xml lists them.</summary>
        static List<Entry> Collect(AnimationInterval interval, ModelObject obj)
        {
            var list = new List<Entry>();
            void Add(string name, AnimationEvent e, DetectorLine? detector = null)
            {
                if (e.Reaction == null)
                    return;
                foreach (var reaction in e.Reaction)
                    list.Add(new Entry(name, reaction, detector));
            }
            foreach (var e in interval.KeyEvents)
                Add("Key " + KeyName(e.Key), e);
            foreach (var e in interval.DetectorHEvents)
                Add("DetectorH" + DetectorTypeName(e.Type), e, obj.DetectorHorizontalLine);
            foreach (var e in interval.DetectorVEvents)
                Add("DetectorV" + DetectorTypeName(e.Type), e, obj.DetectorVerticalLine);
            foreach (var e in interval.CollisionEvents)
                Add("OnCollision", e);
            foreach (var e in interval.FrameEvents)
                Add("OnFrame " + e.Frame, e);
            foreach (var e in interval.AreaEvents)
                Add("OnArea", e);
            foreach (var e in interval.EndEvents)
                Add("OnEnd", e);
            return list;
        }

        static string KeyName(KeyboardKey key) => key switch
        {
            KeyboardKey.Up => "Up",
            KeyboardKey.Down => "Down",
            KeyboardKey.Left => "Left",
            KeyboardKey.Right => "Right",
            _ => "?",
        };

        static string DetectorTypeName(DetectorEvent.DetectorEventType type) => type switch
        {
            DetectorEvent.DetectorEventType.On => " On",
            DetectorEvent.DetectorEventType.Off => " Off",
            _ => "",
        };

        /// <summary>The move's other conditions (sides, slopes, inside, area name) on one line.</summary>
        static string Extras(AnimationReaction reaction, int sign)
        {
            var parts = new List<string>();
            if (reaction.Sides.Count > 0)
                parts.Add("Sides " + string.Join("|", reaction.Sides.Select(s => MirrorSide(s, sign))));
            if (reaction.SlopeH.Count > 0)
                parts.Add("SlopeH " + string.Join("|", reaction.SlopeH));
            if (reaction.SlopeV.Count > 0)
                parts.Add("SlopeV " + string.Join("|", reaction.SlopeV));
            if (!float.IsNaN(reaction.InsideH))
                parts.Add("InsideH " + reaction.InsideH);
            if (!float.IsNaN(reaction.InsideV))
                parts.Add("InsideV " + reaction.InsideV);
            if (reaction.TriggerName != null)
                parts.Add("Area " + reaction.TriggerName);
            return string.Join("   ", parts);
        }

        /// <summary>Facing left, sides 1 and 3 swap (AnimationReaction.IsSide).</summary>
        static int MirrorSide(int side, int sign) => sign == -1 ? side switch { 1 => 3, 3 => 1, _ => side } : side;

        static string Bounds(AnimationDelta delta)
        {
            float min = delta.Value.X, max = delta.Value.Y;
            if (!float.IsNaN(min) && !float.IsNaN(max))
                return $"{F(min)} .. {F(max)}";
            if (!float.IsNaN(min))
                return $">= {F(min)}";
            if (!float.IsNaN(max))
                return $"<= {F(max)}";
            return "any";
        }

        static string F(double v) => Math.Round(v, 1).ToString(System.Globalization.CultureInfo.InvariantCulture);

        static (string, Color, float) Describe(AnimationDelta delta, string type, AnimationDeltaData? data, int sign, Vector3 velocity)
        {
            string corner = delta.Name is AnimationDeltaName.DeltaX or AnimationDeltaName.DeltaY && delta.Corner >= 0
                ? $" c{delta.GetCorner(sign)}" : "";
            string text = $"{DeltaName(delta.Name)} {Bounds(delta)}  [{type}{corner}]";
            if (type == "C")
                return (text + "  (at the collision)", Unknown, 16);
            if (data == null)
                return (text + $"  no platform on Detector{type}", Fail, 16);
            double value = data.GetDeltaValue(delta.Name, delta.GetCorner(sign), sign, velocity);
            bool ok = delta.IsInterval(value);
            return (text + $"  = {F(value)}", ok ? Pass : Fail, 16);
        }

        static string DeltaName(AnimationDeltaName name) => name switch
        {
            AnimationDeltaName.VelocityX => "VelosityX",
            AnimationDeltaName.VelocityY => "VelosityY",
            _ => name.ToString(),
        };

        // ================= ON THE LEVEL ================= //

        static void DrawOnLevel(Entry entry, Color color, int sign, ModelObject obj, Func<Vector3, Vector2> toScreen)
        {
            var reaction = entry.Reaction;
            string name = reaction.Name;

            // Sides of the detector event's platform.
            if (reaction.Sides.Count > 0 && entry.EventDetector?.Platform is QuadRunner sidePlatform)
            {
                var edges = sidePlatform.EdgeList;
                foreach (int side in reaction.Sides)
                {
                    int s = MirrorSide(side, sign);
                    if (s < 0 || s >= edges.Count)
                        continue;
                    var a = toScreen(edges[s].Point1);
                    var b = toScreen(edges[s].Point2);
                    if (FirstTime($"side|{Key(a)}|{Key(b)}"))
                        Raylib.DrawLineEx(a, b, 4, Fade(color, 0.8f));
                    // On a long side the label goes where the player is, not at the side's middle off screen.
                    var at = ClosestOnSegment(a, b, toScreen(entry.EventDetector!.Position));
                    StackedLabel($"side {s}: {name}", at, new Vector2(6, 6), color);
                }
            }

            DrawType(reaction.DeltasH, obj.DetectorHorizontalLine, "H", name, color, sign, toScreen);
            DrawType(reaction.DeltasV, obj.DetectorVerticalLine, "V", name, color, sign, toScreen);
        }

        static void DrawType(AnimationDeltas deltas, DetectorLine? detector, string type, string name, Color color, int sign, Func<Vector3, Vector2> toScreen)
        {
            if (detector == null || deltas.Items.Count == 0)
                return;
            var platform = detector.Platform;
            Vector3 det = detector.Position;

            // Width / Height: rulers along the platform.
            if (platform != null)
            {
                foreach (var delta in deltas.Items)
                {
                    if (delta.Name == AnimationDeltaName.Width)
                        Ruler(platform, true, delta, $"{name} Width [{type}]", color, sign, toScreen);
                    else if (delta.Name == AnimationDeltaName.Height)
                        Ruler(platform, false, delta, $"{name} Height [{type}]", color, sign, toScreen);
                }
            }

            // DeltaX / DeltaY: one zone per corner.
            foreach (var group in deltas.Items
                         .Where(d => d.Name is AnimationDeltaName.DeltaX or AnimationDeltaName.DeltaY)
                         .GroupBy(d => d.GetCorner(sign)))
            {
                int corner = group.Key;
                var dx = group.FirstOrDefault(d => d.Name == AnimationDeltaName.DeltaX);
                var dy = group.FirstOrDefault(d => d.Name == AnimationDeltaName.DeltaY);

                // Offsets of the detector from the corner: (detector - corner).X * sign in [x0, x1], .Y in [y0, y1].
                float x0 = dx?.Value.X ?? float.NaN, x1 = dx?.Value.Y ?? float.NaN;
                float y0 = dy?.Value.X ?? float.NaN, y1 = dy?.Value.Y ?? float.NaN;

                if (platform != null)
                {
                    Vector3 c = platform.GetCornerByIndex(corner);
                    // Detector X in [c + x0, c + x1] facing right, [c - x1, c - x0] facing left.
                    var (left, right, leftLabel, rightLabel) = sign == 1
                        ? (c.X + x0, c.X + x1, dx != null ? $"DeltaX {F(x0)}" : null, dx != null ? $"DeltaX {F(x1)}" : null)
                        : (c.X - x1, c.X - x0, dx != null ? $"DeltaX {F(x1)}" : null, dx != null ? $"DeltaX {F(x0)}" : null);
                    float top = c.Y + y0, bottom = c.Y + y1;
                    Zone(c, left, right, top, bottom, leftLabel, rightLabel,
                        dy != null ? $"DeltaY {F(y0)}" : null, dy != null ? $"DeltaY {F(y1)}" : null,
                        dashed: false, color, toScreen);

                    var cs = toScreen(c);
                    if (FirstTime("corner|" + Key(cs)))
                    {
                        Raylib.DrawRectangleV(cs - new Vector2(3, 3), new Vector2(6, 6), color);
                        Raylib.DrawLineEx(cs, toScreen(det), 1, Fade(color, 0.6f));
                    }
                    StackedLabel($"{name} [{type} c{corner}]", cs, new Vector2(6, 4), color);
                }
                else
                {
                    // No platform: where the corner of a platform has to be. Corner X in [d - x1, d - x0] facing
                    // right, [d + x0, d + x1] facing left; corner Y in [d - y1, d - y0].
                    var (left, right, leftLabel, rightLabel) = sign == 1
                        ? (det.X - x1, det.X - x0, dx != null ? $"DeltaX {F(x1)}" : null, dx != null ? $"DeltaX {F(x0)}" : null)
                        : (det.X + x0, det.X + x1, dx != null ? $"DeltaX {F(x0)}" : null, dx != null ? $"DeltaX {F(x1)}" : null);
                    float top = det.Y - y1, bottom = det.Y - y0;
                    Zone(det, left, right, top, bottom, leftLabel, rightLabel,
                        dy != null ? $"DeltaY {F(y1)}" : null, dy != null ? $"DeltaY {F(y0)}" : null,
                        dashed: true, color, toScreen);
                    var ds = toScreen(det);
                    StackedLabel($"No platform on Detector{type}: dashed = where a platform's corner must be", ds, new Vector2(8, 8), Text);
                }
            }
        }

        /// <summary>
        /// A zone bounded by up to four lines (NaN = no bound on that side, drawn open). Only bounded sides get a
        /// line and a label; an axis without any condition spans <see cref="Open"/> around the anchor.
        /// </summary>
        static void Zone(Vector3 anchor, float left, float right, float top, float bottom,
            string? leftLabel, string? rightLabel, string? topLabel, string? bottomLabel,
            bool dashed, Color color, Func<Vector3, Vector2> toScreen)
        {
            bool hasLeft = !float.IsNaN(left), hasRight = !float.IsNaN(right);
            bool hasTop = !float.IsNaN(top), hasBottom = !float.IsNaN(bottom);
            bool xAxis = leftLabel != null, yAxis = topLabel != null;

            // Extent of the lines along the other axis.
            float l = hasLeft ? left : (hasRight ? right - Open : anchor.X - Open);
            float r = hasRight ? right : (hasLeft ? left + Open : anchor.X + Open);
            float t = hasTop ? top : (hasBottom ? bottom - Open : anchor.Y - Open);
            float b = hasBottom ? bottom : (hasTop ? top + Open : anchor.Y + Open);
            if (!xAxis) { l = anchor.X - Open; r = anchor.X + Open; }
            if (!yAxis) { t = anchor.Y - Open; b = anchor.Y + Open; }

            var tl = toScreen(new Vector3(l, t, 0));
            var br = toScreen(new Vector3(r, b, 0));
            var fill = new Raylib_cs.Rectangle(MathF.Min(tl.X, br.X), MathF.Min(tl.Y, br.Y), MathF.Abs(br.X - tl.X), MathF.Abs(br.Y - tl.Y));
            if (FirstTime($"fill|{fill.X:0},{fill.Y:0},{fill.Width:0},{fill.Height:0}"))
                Raylib.DrawRectangleRec(fill, Fade(color, dashed ? 0.06f : 0.12f));

            if (xAxis && hasLeft)
                Line(toScreen(new Vector3(left, t, 0)), toScreen(new Vector3(left, b, 0)), dashed, color, leftLabel!, true);
            if (xAxis && hasRight && right != left)
                Line(toScreen(new Vector3(right, t, 0)), toScreen(new Vector3(right, b, 0)), dashed, color, rightLabel!, true);
            if (yAxis && hasTop)
                Line(toScreen(new Vector3(l, top, 0)), toScreen(new Vector3(r, top, 0)), dashed, color, topLabel!, false);
            if (yAxis && hasBottom && bottom != top)
                Line(toScreen(new Vector3(l, bottom, 0)), toScreen(new Vector3(r, bottom, 0)), dashed, color, bottomLabel!, false);
        }

        static void Line(Vector2 a, Vector2 b, bool dashed, Color color, string label, bool vertical)
        {
            if (!FirstTime($"line|{Key(a)}|{Key(b)}|{label}"))
                return;
            if (dashed)
                DashedLine(a, b, 2, color);
            else
                Raylib.DrawLineEx(a, b, 2, color);
            // Vertical lines: label at the top end; horizontal lines: label at the right end.
            var at = vertical ? (a.Y < b.Y ? a : b) + new Vector2(3, -LineHeight) : (a.X > b.X ? a : b) + new Vector2(4, -LineHeight / 2);
            Label(label, at, color);
        }

        /// <summary>Width (along the top edge) or Height (along the left edge) with its Min / Max marks.</summary>
        static void Ruler(QuadRunner platform, bool width, AnimationDelta delta, string title, Color color, int sign, Func<Vector3, Vector2> toScreen)
        {
            if (!FirstTime($"ruler|{platform.GetHashCode()}|{width}|{delta.Value.X}|{delta.Value.Y}|{title}"))
                return;
            Vector3 origin = platform.GetCornerByIndex(0);
            const float offset = 8; // drawn a little outside the platform
            Vector3 Along(float d) => width ? new Vector3(origin.X + d, origin.Y - offset, 0) : new Vector3(origin.X - offset, origin.Y + d, 0);

            float min = delta.Value.X, max = delta.Value.Y;
            float actual = width ? platform.GetSize(sign).X : platform.GetSize(sign).Y;
            float end = MathF.Max(actual, float.IsNaN(max) ? (float.IsNaN(min) ? 0 : min) : max);

            Raylib.DrawLineEx(toScreen(Along(0)), toScreen(Along(end)), 1, Fade(color, 0.5f));
            Raylib.DrawLineEx(toScreen(Along(0)), toScreen(Along(actual)), 3, Fade(color, delta.IsInterval(actual) ? 0.9f : 0.35f));
            void Mark(float d, string text)
            {
                var p = toScreen(Along(d));
                var n = width ? new Vector2(0, 6) : new Vector2(6, 0);
                Raylib.DrawLineEx(p - n, p + n, 2, color);
                Label(text, width ? p + new Vector2(3, -LineHeight - 6) : p + new Vector2(-TextWidth(text) - 10, -LineHeight / 2), color);
            }
            if (!float.IsNaN(min))
                Mark(min, $"{(width ? "W" : "H")} >= {F(min)}");
            if (!float.IsNaN(max))
                Mark(max, $"{(width ? "W" : "H")} <= {F(max)}");
            var start = toScreen(Along(0));
            Label(title, width ? start + new Vector2(0, -LineHeight * 2 - 6) : start + new Vector2(-TextWidth(title) - 10, -LineHeight * 1.5f), color);
        }

        // ================= DRAWING HELPERS ================= //

        static Color Fade(Color c, float alpha) => new(c.R, c.G, c.B, (byte)Math.Clamp(alpha * 255, 0, 255));

        static float TextWidth(string text) => Raylib.MeasureTextEx(GameFont.Font, text, FontSize, FontSize / 10f).X;

        /// <summary>Text with a dark shadow so it reads over any background.</summary>
        static void Label(string text, Vector2 at, Color color)
        {
            at = new Vector2(MathF.Round(at.X), MathF.Round(at.Y));
            float w = TextWidth(text);
            for (int tries = 0; tries < 12; tries++)
            {
                var rect = new Raylib_cs.Rectangle(at.X, at.Y, w, LineHeight - 1);
                if (!placed.Any(r => Raylib.CheckCollisionRecs(r, rect)))
                    break;
                at.Y += LineHeight;
            }
            placed.Add(new Raylib_cs.Rectangle(at.X, at.Y, w, LineHeight - 1));
            if (at.X > Raylib.GetScreenWidth() || at.Y > Raylib.GetScreenHeight() || at.X + TextWidth(text) < 0 || at.Y + LineHeight < 0)
                return;
            GameFont.Draw(text, at.X + 1, at.Y + 1, FontSize, new Color((byte)0, (byte)0, (byte)0, (byte)200));
            GameFont.Draw(text, at.X, at.Y, FontSize, color);
        }

        static Vector2 ClosestOnSegment(Vector2 a, Vector2 b, Vector2 p)
        {
            var ab = b - a;
            float lengthSquared = ab.LengthSquared();
            if (lengthSquared < 1e-6f)
                return a;
            float t = Math.Clamp(Vector2.Dot(p - a, ab) / lengthSquared, 0, 1);
            return a + ab * t;
        }

        static void DashedLine(Vector2 a, Vector2 b, float thick, Color color)
        {
            float length = Vector2.Distance(a, b);
            if (length < 0.5f)
                return;
            var dir = (b - a) / length;
            const float dash = 6, gap = 4;
            for (float d = 0; d < length; d += dash + gap)
                Raylib.DrawLineEx(a + dir * d, a + dir * MathF.Min(d + dash, length), thick, color);
        }

        /// <summary>
        /// The list, along the top of the screen in as many columns as needed (left of the debug buttons) and at
        /// most ~40% of the screen high, so the player and the level below stay visible.
        /// </summary>
        static void DrawPanel(List<List<(string text, Color color, float indent)>> blocks)
        {
            const float left = 10, top = 26, pad = 6, columnGap = 14;
            float right = Raylib.GetScreenWidth() - 200; // the debug buttons' column
            float maxHeight = MathF.Max(LineHeight * 6, Raylib.GetScreenHeight() * 0.42f);
            int maxLines = (int)(maxHeight / LineHeight);

            // Lay the blocks out in columns.
            var columns = new List<List<(string text, Color color, float indent)>> { new() };
            foreach (var block in blocks)
            {
                var column = columns[^1];
                if (column.Count > 0 && column.Count + block.Count > maxLines)
                    columns.Add(column = new());
                column.AddRange(block.Take(maxLines));
            }

            float x = left;
            int hidden = 0;
            for (int c = 0; c < columns.Count; c++)
            {
                var column = columns[c];
                float width = 0;
                foreach (var line in column)
                    width = MathF.Max(width, line.indent + TextWidth(line.text));
                if (c > 0 && x + width > right)
                {
                    hidden += columns.Skip(c).Sum(col => col.Count);
                    break;
                }
                Raylib.DrawRectangleRec(new Raylib_cs.Rectangle(x - pad, top - pad, width + pad * 2, column.Count * LineHeight + pad * 2), PanelBack);
                for (int i = 0; i < column.Count; i++)
                    GameFont.Draw(column[i].text, x + column[i].indent, top + i * LineHeight, FontSize, column[i].color);
                x += width + pad * 2 + columnGap;
            }
            if (hidden > 0)
                Label($"... {hidden} more lines", new Vector2(left, top + maxLines * LineHeight + pad * 2), Unknown);
        }
    }
}
