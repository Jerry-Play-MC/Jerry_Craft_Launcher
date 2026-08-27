using System;
using System.Collections.Generic;
using System.IO;
using fNbt;

namespace New_Launcher.SavesManager
{
    internal class LevelDatEditorLegacy : ILevelDatEditor
    {
        private readonly string _filePath;
        private NbtFile _nbtFile;
        private NbtCompound _dataTag;
        private int _dataVersion = -1;

        public LevelDatEditorLegacy(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentException("文件路径不能为空", nameof(filePath));
            _filePath = filePath;
            LoadFile();
        }

        private void LoadFile()
        {
            if (!File.Exists(_filePath))
                throw new FileNotFoundException($"找不到文件: {_filePath}");

            _nbtFile = new NbtFile();
            // 修复：显式传入 null 作为 selector
            _nbtFile.LoadFromFile(_filePath, NbtCompression.GZip, null);

            if (_nbtFile.RootTag == null)
                throw new InvalidDataException("NBT 文件根标签为空");

            _dataTag = _nbtFile.RootTag.Get<NbtCompound>("Data");
            if (_dataTag == null)
                throw new InvalidDataException("NBT 文件中缺少 Data 标签");

            var versionTag = _dataTag.Get<NbtInt>("DataVersion");
            _dataVersion = versionTag != null ? versionTag.Value : -1;

            Normalize();
        }

        private void Normalize()
        {
            // ---- 难度迁移 ----
            var diffSettings = _dataTag.Get<NbtCompound>("difficulty_settings");
            if (diffSettings == null)
            {
                diffSettings = new NbtCompound("difficulty_settings");
                _dataTag.Add(diffSettings);

                var oldDifficulty = _dataTag.Get<NbtByte>("Difficulty");
                if (oldDifficulty != null)
                {
                    diffSettings["difficulty"] = new NbtString("difficulty", DifficultyIntToString(oldDifficulty.Value));
                    _dataTag.Remove("Difficulty");
                }

                var oldLocked = _dataTag.Get<NbtByte>("DifficultyLocked");
                if (oldLocked != null)
                {
                    diffSettings["locked"] = new NbtByte("locked", oldLocked.Value);
                    _dataTag.Remove("DifficultyLocked");
                }
                else
                    diffSettings["locked"] = new NbtByte("locked", 0);
            }

            // ---- 出生点迁移 ----
            var spawnTag = _dataTag.Get<NbtCompound>("Spawn");
            if (spawnTag == null)
            {
                var oldX = _dataTag.Get<NbtInt>("SpawnX");
                var oldY = _dataTag.Get<NbtInt>("SpawnY");
                var oldZ = _dataTag.Get<NbtInt>("SpawnZ");
                if (oldX != null && oldY != null && oldZ != null)
                {
                    spawnTag = new NbtCompound("Spawn");
                    spawnTag["pos"] = new NbtIntArray("pos", new[] { oldX.Value, oldY.Value, oldZ.Value });
                    spawnTag["dimension"] = new NbtString("dimension", "minecraft:overworld");
                    spawnTag["yaw"] = new NbtInt("yaw", 0);
                    spawnTag["pitch"] = new NbtInt("pitch", 0);
                    _dataTag.Add(spawnTag);
                    _dataTag.Remove("SpawnX");
                    _dataTag.Remove("SpawnY");
                    _dataTag.Remove("SpawnZ");
                }
                else
                {
                    spawnTag = new NbtCompound("Spawn");
                    spawnTag["pos"] = new NbtIntArray("pos", new[] { 0, 64, 0 });
                    spawnTag["dimension"] = new NbtString("dimension", "minecraft:overworld");
                    spawnTag["yaw"] = new NbtInt("yaw", 0);
                    spawnTag["pitch"] = new NbtInt("pitch", 0);
                    _dataTag.Add(spawnTag);
                }
            }

            if (_dataTag.Get<NbtCompound>("GameRules") == null)
                _dataTag.Add(new NbtCompound("GameRules"));
        }

        private string DifficultyIntToString(int val)
        {
            switch (val)
            {
                case 0: return "peaceful";
                case 1: return "easy";
                case 2: return "normal";
                case 3: return "hard";
                default: return "normal";
            }
        }

        private int DifficultyStringToInt(string val)
        {
            switch (val?.ToLowerInvariant())
            {
                case "peaceful": return 0;
                case "easy": return 1;
                case "normal": return 2;
                case "hard": return 3;
                default: return 2;
            }
        }

        public int DataVersion => _dataVersion;

        public string LevelName
        {
            get => _dataTag.Get<NbtString>("LevelName")?.Value ?? string.Empty;
            set => _dataTag["LevelName"] = new NbtString("LevelName", value ?? string.Empty);
        }

        public bool AllowCommands
        {
            get => _dataTag.Get<NbtByte>("allowCommands")?.Value == 1;
            set => _dataTag["allowCommands"] = new NbtByte("allowCommands", (byte)(value ? 1 : 0));
        }

        public int GameType
        {
            get => _dataTag.Get<NbtInt>("GameType")?.Value ?? 0;
            set
            {
                if (value < 0 || value > 3) throw new ArgumentOutOfRangeException();
                _dataTag["GameType"] = new NbtInt("GameType", value);
            }
        }

        public string Difficulty
        {
            get
            {
                var diff = _dataTag.Get<NbtCompound>("difficulty_settings");
                if (diff != null)
                {
                    var tag = diff.Get<NbtString>("difficulty");
                    if (tag != null) return tag.Value;
                }
                var old = _dataTag.Get<NbtByte>("Difficulty");
                return old != null ? DifficultyIntToString(old.Value) : "normal";
            }
            set
            {
                var normalized = value?.ToLowerInvariant();
                if (normalized != "peaceful" && normalized != "easy" && normalized != "normal" && normalized != "hard")
                    throw new ArgumentException("难度必须是 peaceful/easy/normal/hard");
                var diff = _dataTag.Get<NbtCompound>("difficulty_settings");
                if (diff == null) { diff = new NbtCompound("difficulty_settings"); _dataTag.Add(diff); }
                diff["difficulty"] = new NbtString("difficulty", normalized);
            }
        }

        public bool IsDifficultyLocked
        {
            get
            {
                var diff = _dataTag.Get<NbtCompound>("difficulty_settings");
                if (diff != null)
                {
                    var tag = diff.Get<NbtByte>("locked");
                    if (tag != null) return tag.Value == 1;
                }
                var old = _dataTag.Get<NbtByte>("DifficultyLocked");
                return old != null && old.Value == 1;
            }
            set
            {
                var diff = _dataTag.Get<NbtCompound>("difficulty_settings");
                if (diff == null) { diff = new NbtCompound("difficulty_settings"); _dataTag.Add(diff); }
                diff["locked"] = new NbtByte("locked", (byte)(value ? 1 : 0));
            }
        }

        public bool IsHardcore
        {
            get
            {
                var diff = _dataTag.Get<NbtCompound>("difficulty_settings");
                if (diff != null)
                {
                    var tag = diff.Get<NbtByte>("hardcore");
                    if (tag != null) return tag.Value == 1;
                }
                var root = _dataTag.Get<NbtByte>("hardcore");
                return root != null && root.Value == 1;
            }
            set
            {
                var diff = _dataTag.Get<NbtCompound>("difficulty_settings");
                if (diff == null) { diff = new NbtCompound("difficulty_settings"); _dataTag.Add(diff); }
                diff["hardcore"] = new NbtByte("hardcore", (byte)(value ? 1 : 0));
                _dataTag["hardcore"] = new NbtByte("hardcore", (byte)(value ? 1 : 0));
            }
        }

        public bool IsInitialized
        {
            get => _dataTag.Get<NbtByte>("initialized")?.Value != 0;
            set => _dataTag["initialized"] = new NbtByte("initialized", (byte)(value ? 1 : 0));
        }

        public long Time
        {
            get => _dataTag.Get<NbtLong>("Time")?.Value ?? 0;
            set => _dataTag["Time"] = new NbtLong("Time", value);
        }

        public SpawnPoint26 Spawn
        {
            get
            {
                var spawnTag = _dataTag.Get<NbtCompound>("Spawn");
                if (spawnTag != null)
                {
                    var pos = spawnTag.Get<NbtIntArray>("pos");
                    if (pos != null && pos.Value.Length >= 3)
                    {
                        var dim = spawnTag.Get<NbtString>("dimension")?.Value ?? "minecraft:overworld";
                        return new SpawnPoint26(pos.Value[0], pos.Value[1], pos.Value[2], dim);
                    }
                }
                return new SpawnPoint26(0, 64, 0);
            }
            set
            {
                var spawnTag = _dataTag.Get<NbtCompound>("Spawn");
                if (spawnTag == null) { spawnTag = new NbtCompound("Spawn"); _dataTag.Add(spawnTag); }
                spawnTag["pos"] = new NbtIntArray("pos", new[] { value.X, value.Y, value.Z });
                spawnTag["dimension"] = new NbtString("dimension", value.Dimension ?? "minecraft:overworld");
            }
        }

        private NbtCompound GetGameRulesTag()
        {
            var rules = _dataTag.Get<NbtCompound>("GameRules");
            if (rules == null) { rules = new NbtCompound("GameRules"); _dataTag.Add(rules); }
            return rules;
        }

        public string GetGameRule(string ruleName)
        {
            var rules = GetGameRulesTag();
            var tag = rules.Get<NbtString>(ruleName);
            return tag?.Value;
        }

        public void SetGameRule(string ruleName, string value)
        {
            var rules = GetGameRulesTag();
            if (value == null)
            {
                // 修复：ContainsKey -> Contains
                if (rules.Contains(ruleName))
                    rules.Remove(ruleName);
            }
            else
            {
                rules[ruleName] = new NbtString(ruleName, value);
            }
        }

        public Dictionary<string, string> GetAllGameRules()
        {
            var dict = new Dictionary<string, string>();
            var rules = _dataTag.Get<NbtCompound>("GameRules");
            if (rules != null)
            {
                foreach (var tag in rules.Tags)
                {
                    if (tag is NbtString str)
                        dict[tag.Name] = str.Value;
                }
            }
            return dict;
        }

        public List<ModificationItem> GetModifications()
        {
            var list = new List<ModificationItem>();

            list.Add(new ModificationItem { Name = "LevelName", ValueType = "string", CurrentValue = LevelName, IsSupported = true, Description = "存档显示名称" });
            list.Add(new ModificationItem { Name = "AllowCommands", ValueType = "bool", CurrentValue = AllowCommands, IsSupported = true, Description = "是否允许使用命令" });
            list.Add(new ModificationItem { Name = "GameType", ValueType = "int", CurrentValue = GameType, IsSupported = true, Description = "游戏模式 (0=生存,1=创造,2=冒险,3=旁观)" });
            list.Add(new ModificationItem { Name = "Difficulty", ValueType = "string", CurrentValue = Difficulty, IsSupported = true, Description = "难度 (peaceful/easy/normal/hard)" });
            list.Add(new ModificationItem { Name = "DifficultyLocked", ValueType = "bool", CurrentValue = IsDifficultyLocked, IsSupported = true, Description = "难度是否锁定" });
            list.Add(new ModificationItem { Name = "Hardcore", ValueType = "bool", CurrentValue = IsHardcore, IsSupported = true, Description = "是否为极限模式" });
            list.Add(new ModificationItem { Name = "Initialized", ValueType = "bool", CurrentValue = IsInitialized, IsSupported = true, Description = "是否已初始化" });
            list.Add(new ModificationItem { Name = "Time", ValueType = "long", CurrentValue = Time, IsSupported = true, Description = "游戏时间（刻）" });
            var spawn = Spawn;
            list.Add(new ModificationItem { Name = "Spawn", ValueType = "spawn", CurrentValue = $"{spawn.X},{spawn.Y},{spawn.Z},{spawn.Dimension}", IsSupported = true, Description = "出生点坐标和维度 (x,y,z,dimension)" });

            var allRules = GetAllGameRules();
            foreach (var kv in allRules)
                list.Add(new ModificationItem { Name = "GameRule." + kv.Key, ValueType = "string", CurrentValue = kv.Value, IsSupported = true, Description = "游戏规则" });

            string[] commonRules = { "keepInventory", "doDaylightCycle", "doMobSpawning", "doFireTick", "doWeatherCycle" };
            foreach (var rule in commonRules)
                if (!allRules.ContainsKey(rule))
                    list.Add(new ModificationItem { Name = "GameRule." + rule, ValueType = "string", CurrentValue = null, IsSupported = true, Description = "游戏规则（当前未设置，可创建）" });

            return list;
        }

        public void ApplyModification(string name, object value)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("修改项名称不能为空");

            if (name.StartsWith("GameRule.", StringComparison.OrdinalIgnoreCase))
            {
                string ruleName = name.Substring("GameRule.".Length);
                SetGameRule(ruleName, value?.ToString());
                Save();
                return;
            }

            switch (name)
            {
                case "LevelName": LevelName = value?.ToString(); break;
                case "AllowCommands": AllowCommands = Convert.ToBoolean(value); break;
                case "GameType": GameType = Convert.ToInt32(value); break;
                case "Difficulty": Difficulty = value?.ToString(); break;
                case "DifficultyLocked": IsDifficultyLocked = Convert.ToBoolean(value); break;
                case "Hardcore": IsHardcore = Convert.ToBoolean(value); break;
                case "Initialized": IsInitialized = Convert.ToBoolean(value); break;
                case "Time": Time = Convert.ToInt64(value); break;
                case "Spawn":
                    {
                        string str = value?.ToString();
                        if (string.IsNullOrEmpty(str)) throw new ArgumentException("出生点值不能为空");
                        var parts = str.Split(',');
                        if (parts.Length < 3) throw new ArgumentException("出生点格式必须为 x,y,z,dimension");
                        int x = int.Parse(parts[0].Trim());
                        int y = int.Parse(parts[1].Trim());
                        int z = int.Parse(parts[2].Trim());
                        string dim = parts.Length >= 4 ? parts[3].Trim() : "minecraft:overworld";
                        Spawn = new SpawnPoint26(x, y, z, dim);
                    }
                    break;
                default: throw new NotSupportedException($"修改项 '{name}' 不被支持");
            }
            Save();
        }

        public void Save()
        {
            // 反规范化
            var diff = _dataTag.Get<NbtCompound>("difficulty_settings");
            if (diff != null)
            {
                var diffStr = diff.Get<NbtString>("difficulty");
                if (diffStr != null)
                    _dataTag["Difficulty"] = new NbtByte("Difficulty", (byte)DifficultyStringToInt(diffStr.Value));
                var locked = diff.Get<NbtByte>("locked");
                if (locked != null)
                    _dataTag["DifficultyLocked"] = new NbtByte("DifficultyLocked", locked.Value);
            }
            var spawnTag = _dataTag.Get<NbtCompound>("Spawn");
            if (spawnTag != null)
            {
                var pos = spawnTag.Get<NbtIntArray>("pos");
                if (pos != null && pos.Value.Length >= 3)
                {
                    _dataTag["SpawnX"] = new NbtInt("SpawnX", pos.Value[0]);
                    _dataTag["SpawnY"] = new NbtInt("SpawnY", pos.Value[1]);
                    _dataTag["SpawnZ"] = new NbtInt("SpawnZ", pos.Value[2]);
                }
            }
            // SaveToFile 只有两个参数
            _nbtFile.SaveToFile(_filePath, NbtCompression.GZip);
        }

        /// <summary>
        /// 不关闭极限模式，仅重置玩家为生存并清除死亡标记
        /// </summary>
        public void ResurrectPlayerOnly()
        {
            // 获取 Player 标签
            var playerTag = _dataTag.Get<NbtCompound>("Player");
            if (playerTag == null)
            {
                throw new InvalidOperationException("存档中找不到 Player 标签，无法进行急救操作。");
            }

            // 设置玩家当前模式为生存 (0)
            playerTag["playerGameType"] = new NbtInt("playerGameType", 0);

            // 清除死亡标记
            if (playerTag.Contains("LastDeathLocation"))
                playerTag.Remove("LastDeathLocation");

            // 重置生命值
            playerTag["Health"] = new NbtFloat("Health", 20.0f);

            // 重置死亡计时器
            if (playerTag.Contains("DeathTime"))
                playerTag["DeathTime"] = new NbtShort("DeathTime", 0);

            // 可选清除其他状态
            if (playerTag.Contains("HurtTime"))
                playerTag["HurtTime"] = new NbtShort("HurtTime", 0);
            if (playerTag.Contains("AttackTime"))
                playerTag["AttackTime"] = new NbtShort("AttackTime", 0);
            if (playerTag.Contains("FallDistance"))
                playerTag["FallDistance"] = new NbtFloat("FallDistance", 0.0f);

            // 设置世界默认模式也为生存（可选）
            _dataTag["GameType"] = new NbtInt("GameType", 0);

            // 注意：不要修改 Data.hardcore，保持极限模式开启
        }
    }
}