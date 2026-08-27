using System;
using System.Collections.Generic;

namespace New_Launcher.SavesManager
{
    public class ModificationItem
    {
        public string Name { get; set; }
        public string ValueType { get; set; }
        public object CurrentValue { get; set; }
        public bool IsSupported { get; set; }
        public string Description { get; set; }

        public override string ToString() =>
            $"{Name} ({ValueType}) = {CurrentValue} {(IsSupported ? "" : "[不支持]")}";
    }

    public struct SpawnPoint26
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Z { get; set; }
        public string Dimension { get; set; }

        public SpawnPoint26(int x, int y, int z, string dimension = "minecraft:overworld")
        {
            X = x; Y = y; Z = z;
            Dimension = dimension ?? "minecraft:overworld";
        }

        public override string ToString() => $"({X}, {Y}, {Z}) in {Dimension}";
    }

    internal interface ILevelDatEditor
    {
        int DataVersion { get; }
        List<ModificationItem> GetModifications();
        void ApplyModification(string name, object value);
        void Save();
    }
}