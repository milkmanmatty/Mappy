namespace Mappy.Util
{
    using System.Collections.Generic;

    public static class InitialMissionCommands
    {
        public static List<string> Split(string initialMission)
        {
            var commands = new List<string>();
            if (string.IsNullOrWhiteSpace(initialMission))
            {
                return commands;
            }

            var parts = initialMission.Split(',');
            foreach (var part in parts)
            {
                var trimmed = part.Trim();
                if (trimmed.Length == 0)
                {
                    continue;
                }

                commands.Add(trimmed);
            }

            return commands;
        }

        public static string Join(IEnumerable<string> commands)
        {
            if (commands == null)
            {
                return string.Empty;
            }

            var parts = new List<string>();
            foreach (var command in commands)
            {
                if (string.IsNullOrWhiteSpace(command))
                {
                    continue;
                }

                parts.Add(command.Trim());
            }

            return string.Join(",", parts);
        }
    }
}
