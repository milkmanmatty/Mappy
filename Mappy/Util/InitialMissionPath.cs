namespace Mappy.Util
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;

    using Mappy.Data;

    public static class InitialMissionPath
    {
        public enum SegmentKind
        {
            Move,
            Patrol,
            Attack,
        }

        private enum CommandKind
        {
            Patrol,
            MovePoint,
            AttackPoint,
            AttackUnit,
            Guard,
            Wait,
            Break,
        }

        public static Route Build(
            string initialMission,
            int startX,
            int startZ,
            Guid selfId,
            IEnumerable<SchemaUnit> schemaUnits)
        {
            if (string.IsNullOrWhiteSpace(initialMission))
            {
                return Route.Empty;
            }

            var commands = ParseCommands(initialMission);
            if (commands.Count == 0)
            {
                return Route.Empty;
            }

            var units = new List<SchemaUnit>();
            if (schemaUnits != null)
            {
                foreach (var unit in schemaUnits)
                {
                    if (unit != null)
                    {
                        units.Add(unit);
                    }
                }
            }

            var trailingPatrolStart = commands.Count;
            for (var i = commands.Count - 1; i >= 0; i--)
            {
                if (commands[i].Kind != CommandKind.Patrol)
                {
                    break;
                }

                trailingPatrolStart = i;
            }

            var segments = new List<Segment>();
            var waits = new List<WaitMarker>();
            var waitStacks = new Dictionary<long, int>();
            var trailingPatrolPoints = new List<PointXZ>();
            var currentX = startX;
            var currentZ = startZ;

            for (var i = 0; i < commands.Count; i++)
            {
                var command = commands[i];
                switch (command.Kind)
                {
                    case CommandKind.Wait:
                        var stackKey = PackPoint(currentX, currentZ);
                        var stackIndex = 0;
                        if (waitStacks.TryGetValue(stackKey, out var existingStack))
                        {
                            stackIndex = existingStack;
                        }

                        waitStacks[stackKey] = stackIndex + 1;
                        waits.Add(new WaitMarker(currentX, currentZ, command.Seconds, stackIndex));
                        break;

                    case CommandKind.Patrol:
                        AddSegment(segments, SegmentKind.Patrol, currentX, currentZ, command.X, command.Z);
                        currentX = command.X;
                        currentZ = command.Z;
                        if (i >= trailingPatrolStart)
                        {
                            trailingPatrolPoints.Add(new PointXZ(command.X, command.Z));
                        }

                        break;

                    case CommandKind.MovePoint:
                        AddSegment(segments, SegmentKind.Move, currentX, currentZ, command.X, command.Z);
                        currentX = command.X;
                        currentZ = command.Z;
                        break;

                    case CommandKind.AttackPoint:
                        AddSegment(segments, SegmentKind.Attack, currentX, currentZ, command.X, command.Z);
                        currentX = command.X;
                        currentZ = command.Z;
                        break;

                    case CommandKind.AttackUnit:
                        if (TryFindUnit(units, selfId, command.Target, matchIdentOnly: false, currentX, currentZ, out var attackTarget))
                        {
                            AddSegment(segments, SegmentKind.Attack, currentX, currentZ, attackTarget.XPos, attackTarget.ZPos);
                            currentX = attackTarget.XPos;
                            currentZ = attackTarget.ZPos;
                        }

                        break;

                    case CommandKind.Guard:
                        if (TryFindUnit(units, selfId, command.Target, matchIdentOnly: true, currentX, currentZ, out var guardTarget))
                        {
                            AddSegment(segments, SegmentKind.Move, currentX, currentZ, guardTarget.XPos, guardTarget.ZPos);
                            currentX = guardTarget.XPos;
                            currentZ = guardTarget.ZPos;
                        }

                        break;
                }
            }

            if (trailingPatrolPoints.Count >= 2)
            {
                var first = trailingPatrolPoints[0];
                var last = trailingPatrolPoints[trailingPatrolPoints.Count - 1];
                AddSegment(segments, SegmentKind.Patrol, last.X, last.Z, first.X, first.Z);
            }

            if (segments.Count == 0 && waits.Count == 0)
            {
                return Route.Empty;
            }

            return new Route(segments, waits);
        }

        private static void AddSegment(List<Segment> segments, SegmentKind kind, int x1, int z1, int x2, int z2)
        {
            if (x1 == x2 && z1 == z2)
            {
                return;
            }

            segments.Add(new Segment(kind, x1, z1, x2, z2));
        }

        private static bool TryFindUnit(
            List<SchemaUnit> units,
            Guid selfId,
            string target,
            bool matchIdentOnly,
            int fromX,
            int fromZ,
            out SchemaUnit found)
        {
            found = null;
            if (string.IsNullOrEmpty(target))
            {
                return false;
            }

            var bestDistance = long.MaxValue;
            foreach (var unit in units)
            {
                if (unit.Id == selfId)
                {
                    continue;
                }

                var identMatch = !string.IsNullOrEmpty(unit.Ident)
                    && string.Equals(unit.Ident, target, StringComparison.OrdinalIgnoreCase);
                var nameMatch = !matchIdentOnly
                    && string.Equals(unit.Unitname, target, StringComparison.OrdinalIgnoreCase);
                if (!identMatch && !nameMatch)
                {
                    continue;
                }

                var dx = (long)unit.XPos - fromX;
                var dz = (long)unit.ZPos - fromZ;
                var distance = (dx * dx) + (dz * dz);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    found = unit;
                }
            }

            return found != null;
        }

        private static List<Command> ParseCommands(string initialMission)
        {
            var commands = new List<Command>();
            var parts = initialMission.Split(',');
            foreach (var part in parts)
            {
                if (TryParseCommand(part, out var command))
                {
                    commands.Add(command);
                }
            }

            return commands;
        }

        private static bool TryParseCommand(string text, out Command command)
        {
            command = default(Command);
            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            var tokens = text.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length == 0)
            {
                return false;
            }

            var op = tokens[0];
            if (op.Equals("m", StringComparison.OrdinalIgnoreCase))
            {
                return TryPoint(tokens, CommandKind.MovePoint, ref command);
            }

            if (op.Equals("p", StringComparison.OrdinalIgnoreCase))
            {
                return TryPoint(tokens, CommandKind.Patrol, ref command);
            }

            if (op.Equals("u", StringComparison.OrdinalIgnoreCase))
            {
                return TryPoint(tokens, CommandKind.MovePoint, ref command);
            }

            if (op.Equals("a", StringComparison.OrdinalIgnoreCase))
            {
                if (tokens.Length >= 3 && TryInt(tokens[1], out var x) && TryInt(tokens[2], out var z))
                {
                    command.Kind = CommandKind.AttackPoint;
                    command.X = x;
                    command.Z = z;
                    return true;
                }

                if (tokens.Length >= 2)
                {
                    command.Kind = CommandKind.AttackUnit;
                    command.Target = tokens[1];
                    return true;
                }

                return false;
            }

            if (op.Equals("g", StringComparison.OrdinalIgnoreCase))
            {
                if (tokens.Length < 2)
                {
                    return false;
                }

                command.Kind = CommandKind.Guard;
                command.Target = tokens[1];
                return true;
            }

            if (op.Equals("b", StringComparison.OrdinalIgnoreCase))
            {
                if (tokens.Length >= 5
                    && TryInt(tokens[2], out _)
                    && TryInt(tokens[3], out var x)
                    && TryInt(tokens[4], out var z))
                {
                    command.Kind = CommandKind.MovePoint;
                    command.X = x;
                    command.Z = z;
                    return true;
                }

                if (tokens.Length >= 3 && TryInt(tokens[2], out _))
                {
                    command.Kind = CommandKind.Break;
                    return true;
                }

                return false;
            }

            if (op.Equals("w", StringComparison.OrdinalIgnoreCase))
            {
                if (tokens.Length >= 2 && TryInt(tokens[1], out var seconds) && seconds >= 0)
                {
                    command.Kind = CommandKind.Wait;
                    command.Seconds = seconds;
                    return true;
                }

                return false;
            }

            if (op.Equals("wa", StringComparison.OrdinalIgnoreCase)
                || op.Equals("s", StringComparison.OrdinalIgnoreCase)
                || op.Equals("d", StringComparison.OrdinalIgnoreCase)
                || op.Equals("o", StringComparison.OrdinalIgnoreCase)
                || op.Equals("bw", StringComparison.OrdinalIgnoreCase)
                || op.Equals("i", StringComparison.OrdinalIgnoreCase))
            {
                command.Kind = CommandKind.Break;
                return true;
            }

            return false;
        }

        private static bool TryPoint(string[] tokens, CommandKind kind, ref Command command)
        {
            if (tokens.Length < 3 || !TryInt(tokens[1], out var x) || !TryInt(tokens[2], out var z))
            {
                return false;
            }

            command.Kind = kind;
            command.X = x;
            command.Z = z;
            return true;
        }

        private static bool TryInt(string text, out int value)
        {
            return int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
        }

        private static long PackPoint(int x, int z)
        {
            return ((long)x << 32) | (uint)z;
        }

        public struct Segment
        {
            public Segment(SegmentKind kind, int x1, int z1, int x2, int z2)
            {
                this.Kind = kind;
                this.X1 = x1;
                this.Z1 = z1;
                this.X2 = x2;
                this.Z2 = z2;
            }

            public SegmentKind Kind { get; }

            public int X1 { get; }

            public int Z1 { get; }

            public int X2 { get; }

            public int Z2 { get; }
        }

        public struct WaitMarker
        {
            public WaitMarker(int x, int z, int seconds, int stackIndex)
            {
                this.X = x;
                this.Z = z;
                this.Seconds = seconds;
                this.StackIndex = stackIndex;
            }

            public int X { get; }

            public int Z { get; }

            public int Seconds { get; }

            public int StackIndex { get; }
        }

        private struct PointXZ
        {
            public PointXZ(int x, int z)
            {
                this.X = x;
                this.Z = z;
            }

            public int X { get; }

            public int Z { get; }
        }

        private struct Command
        {
            public CommandKind Kind;

            public int X;

            public int Z;

            public int Seconds;

            public string Target;
        }

        public sealed class Route
        {
            public Route(IReadOnlyList<Segment> segments, IReadOnlyList<WaitMarker> waits)
            {
                this.Segments = segments ?? new Segment[0];
                this.Waits = waits ?? new WaitMarker[0];
            }

            public static Route Empty { get; } = new Route(new Segment[0], new WaitMarker[0]);

            public IReadOnlyList<Segment> Segments { get; }

            public IReadOnlyList<WaitMarker> Waits { get; }
        }
    }
}
