using fNbt;
using System;
using System.Collections.Generic;
using System.IO;

namespace New_Launcher.SavesManager
{
    internal class LevelDatEditor26 : ILevelDatEditor
    {
        private readonly string _worldPath;
        private readonly string _levelDatPath;
        private readonly string _gameRulesPath;
        private NbtFile _nbtFile;
        private NbtCompound _dataTag;
        private int _dataVersion;

        private Dictionary<string, string> _gameRules = new Dictionary<string, string>();
        private bool _gameRulesLoaded = false;
        private bool _gameRulesDirty = false;

        public LevelDatEditor26(string worldPath)
        {
            if (string.IsNullOrEmpty(worldPath))
                throw new ArgumentException("存档路径不能为空", nameof(worldPath));
            _worldPath = worldPath;
            _levelDatPath = Path.Combine(worldPath, "level.dat");
            _gameRulesPath = Path.Combine(Path.Combine(worldPath, "data"), "game_rules.dat");

            if (!File.Exists(_levelDatPath))
                throw new FileNotFoundException($"找不到 level.dat: {_levelDatPath}");

            LoadLevelDat();
            LoadGameRules();
        }

        /// <summary>
        /// 不关闭极限模式，仅重置玩家为生存并清除死亡标记（支持 26.1+）
        /// 玩家数据文件位于存档的 player/data/ 或 playerdata/ 或 data/player/ 下
        /// </summary>
        public void ResurrectPlayerOnly()
        {
            // 1. 获取单人玩家的 UUID
            string uuid = SingleplayerUuid;
            if (string.IsNullOrEmpty(uuid))
                throw new InvalidOperationException("未找到单人玩家 UUID，无法进行急救。");

            // 2. 尝试多个可能的玩家数据文件路径
            string[] possiblePaths = new string[]
            {
                Path.Combine(Path.Combine(Path.Combine(_worldPath, "player"), "data"), uuid + ".dat"),
                Path.Combine(Path.Combine(Path.Combine(_worldPath, "players"), "data"), uuid + ".dat"),
                Path.Combine(Path.Combine(_worldPath, "playerdata"), uuid + ".dat"),
                Path.Combine(Path.Combine(Path.Combine(_worldPath, "data"), "player"), uuid + ".dat"),
                Path.Combine(Path.Combine(Path.Combine(_worldPath, "data"), "players"), uuid + ".dat")
            };

            string playerDataPath = null;
            foreach (var path in possiblePaths)
            {
                if (File.Exists(path))
                {
                    playerDataPath = path;
                    break;
                }
            }

            if (playerDataPath == null)
                throw new FileNotFoundException($"找不到玩家数据文件，尝试过：{string.Join(", ", possiblePaths)}");

            // 3. 加载玩家数据文件（GZip 压缩）
            var playerFile = new NbtFile();
            playerFile.LoadFromFile(playerDataPath, NbtCompression.GZip, null);
            var playerRoot = playerFile.RootTag;
            if (playerRoot == null || !(playerRoot is NbtCompound))
                throw new InvalidDataException("玩家数据文件格式无效");

            var playerTag = (NbtCompound)playerRoot;

            // 4. 修改玩家当前模式为生存 (0)
            playerTag["playerGameType"] = new NbtInt("playerGameType", 0);

            // 5. 清除死亡标记
            if (playerTag.Contains("LastDeathLocation"))
                playerTag.Remove("LastDeathLocation");

            // 6. 重置生命值
            playerTag["Health"] = new NbtFloat("Health", 20.0f);

            // 7. 重置死亡计时器
            if (playerTag.Contains("DeathTime"))
                playerTag["DeathTime"] = new NbtShort("DeathTime", 0);

            // 8. 可选清除其他状态
            if (playerTag.Contains("HurtTime"))
                playerTag["HurtTime"] = new NbtShort("HurtTime", 0);
            if (playerTag.Contains("AttackTime"))
                playerTag["AttackTime"] = new NbtShort("AttackTime", 0);
            if (playerTag.Contains("FallDistance"))
                playerTag["FallDistance"] = new NbtFloat("FallDistance", 0.0f);

            // 9. 保存玩家数据文件
            playerFile.SaveToFile(playerDataPath, NbtCompression.GZip);

            // 10. 同时将世界默认模式设置为生存（可选）
            _dataTag["GameType"] = new NbtInt("GameType", 0);
        }

        private void LoadLevelDat()
        {
            _nbtFile = new NbtFile();
            // 修复：显式传入 null 作为 selector
            _nbtFile.LoadFromFile(_levelDatPath, NbtCompression.GZip, null);
            if (_nbtFile.RootTag == null)
                throw new InvalidDataException("NBT 根标签为空");
            _dataTag = _nbtFile.RootTag.Get<NbtCompound>("Data");
            if (_dataTag == null)
                throw new InvalidDataException("缺少 Data 标签");

            var verTag = _dataTag.Get<NbtInt>("DataVersion");
            _dataVersion = verTag != null ? verTag.Value : -1;

            if (_dataVersion < LevelDatManager.MIN_DATA_VERSION_26_1)
                throw new NotSupportedException($"数据版本 {_dataVersion} 低于 26.1+");
        }

        private void LoadGameRules()
        {
            _gameRules.Clear();
            _gameRulesLoaded = true;
            _gameRulesDirty = false;

            if (!File.Exists(_gameRulesPath))
                return;

            try
            {
                var file = new NbtFile();
                file.LoadFromFile(_gameRulesPath, NbtCompression.GZip, null);
                var root = file.RootTag;
                if (root != null && root is NbtCompound compound)
                {
                    foreach (var tag in compound.Tags)
                        if (tag is NbtString str)
                            _gameRules[tag.Name] = str.Value;
                }
            }
            catch { /* 忽略损坏文件 */ }
        }

        private void SaveGameRules()
        {
            if (!_gameRulesDirty) return;
            var compound = new NbtCompound("GameRules");
            foreach (var kv in _gameRules)
                compound.Add(new NbtString(kv.Key, kv.Value));

            var file = new NbtFile();
            file.RootTag = compound;
            string dir = Path.GetDirectoryName(_gameRulesPath);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            file.SaveToFile(_gameRulesPath, NbtCompression.GZip); // 只有两个参数
            _gameRulesDirty = false;
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
                return "normal";
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
                return false;
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
                return false;
            }
            set
            {
                var diff = _dataTag.Get<NbtCompound>("difficulty_settings");
                if (diff == null) { diff = new NbtCompound("difficulty_settings"); _dataTag.Add(diff); }
                diff["hardcore"] = new NbtByte("hardcore", (byte)(value ? 1 : 0));
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

        public string SingleplayerUuid
        {
            get
            {
                var tag = _dataTag.Get<NbtTag>("singleplayer_uuid");
                if (tag == null) return null;

                if (tag is NbtString strTag)
                    return strTag.Value; // 假设字符串已带连字符
                else if (tag is NbtIntArray intArrayTag)
                {
                    // 将 int[] 转换为带连字符的 UUID 字符串（标准格式）
                    int[] ints = intArrayTag.Value;
                    if (ints.Length != 4)
                        throw new InvalidDataException("singleplayer_uuid 数组长度不为4");
                    // 每个 int 按大端序转为 8 位十六进制，拼接后转为标准 UUID 格式
                    string hex = "";
                    foreach (int val in ints)
                        hex += val.ToString("X8");
                    // 插入连字符：8-4-4-4-12
                    string uuid = hex.ToLowerInvariant();
                    return uuid.Substring(0, 8) + "-" +
                           uuid.Substring(8, 4) + "-" +
                           uuid.Substring(12, 4) + "-" +
                           uuid.Substring(16, 4) + "-" +
                           uuid.Substring(20, 12);
                }
                else
                    throw new InvalidCastException($"singleplayer_uuid 标签类型为 {tag.GetType().Name}，预期 NbtString 或 NbtIntArray");
            }
            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    if (_dataTag.Contains("singleplayer_uuid"))
                        _dataTag.Remove("singleplayer_uuid");
                }
                else
                {
                    _dataTag["singleplayer_uuid"] = new NbtString("singleplayer_uuid", value);
                }
            }
        }

        private void EnsureGameRulesLoaded()
        {
            if (!_gameRulesLoaded) LoadGameRules();
        }

        public string GetGameRule(string ruleName)
        {
            EnsureGameRulesLoaded();
            _gameRules.TryGetValue(ruleName, out string val);
            return val;
        }

        public void SetGameRule(string ruleName, string value)
        {
            EnsureGameRulesLoaded();
            if (value == null)
                _gameRules.Remove(ruleName);
            else
                _gameRules[ruleName] = value;
            _gameRulesDirty = true;
        }

        public Dictionary<string, string> GetAllGameRules()
        {
            EnsureGameRulesLoaded();
            return new Dictionary<string, string>(_gameRules);
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
            _nbtFile.SaveToFile(_levelDatPath, NbtCompression.GZip); // 只有两个参数
            SaveGameRules();
        }
    }
}